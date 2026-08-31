using System.Collections.Generic;
using System.Linq;
using Qaniva.Clinical.Core.Engine;
using Qaniva.Clinical.Core.Model;
using Qaniva.Clinical.Core.Replay;
using Qaniva.Clinical.Core.Serialization;
using Xunit;

namespace Qaniva.Clinical.Tests;

/// <summary>
/// Behavioural + golden coverage for the first production-like case
/// (stemi_anterior_001, MVP DEMO APPROVED / clinical validation pending).
/// Asserts only behaviours the case data defines — no medical facts beyond the
/// review-gated blueprint are asserted here.
/// </summary>
public class StemiCaseTests
{
    private const ulong Seed = 20260831;

    // --- goldens ------------------------------------------------------

    [Theory]
    [InlineData("stemi_ideal_path.script.json", "stemi_ideal_path.golden.json")]
    [InlineData("stemi_delayed_path.script.json", "stemi_delayed_path.golden.json")]
    [InlineData("stemi_alternative_path.script.json", "stemi_alternative_path.golden.json")]
    [InlineData("stemi_inefficient_path.script.json", "stemi_inefficient_path.golden.json")]
    [InlineData("stemi_harmful_path.script.json", "stemi_harmful_path.golden.json")]
    [InlineData("stemi_deterioration_path.script.json", "stemi_deterioration_path.golden.json")]
    public void ReplayMatchesGolden(string scriptFile, string goldenFile)
    {
        var result = Replayer.Run(TestData.StemiCase(), TestData.LoadScript(scriptFile));
        var expected = TestData.ReadOrUpdateGolden(goldenFile, result).Trim();
        var actual = GoldenSerializer.ToGoldenJson(result).Trim();
        Assert.Equal(GoldenSerializer.Normalize(expected), GoldenSerializer.Normalize(actual));
    }

    [Fact]
    public void GoldenScriptsAreDeterministicAcrossRuns()
    {
        foreach (var script in new[]
        {
            "stemi_ideal_path.script.json",
            "stemi_delayed_path.script.json",
            "stemi_deterioration_path.script.json",
        })
        {
            var a = Replayer.Run(TestData.StemiCase(), TestData.LoadScript(script));
            var b = Replayer.Run(TestData.StemiCase(), TestData.LoadScript(script));
            Assert.Equal(a.ReplayHash, b.ReplayHash);
            Assert.Equal(GoldenSerializer.ToGoldenJson(a), GoldenSerializer.ToGoldenJson(b));
        }
    }

    // --- pathway outcomes --------------------------------------------

    [Fact]
    public void IdealPathCompletesWithFullScore()
    {
        var result = Replayer.Run(TestData.StemiCase(), TestData.LoadScript("stemi_ideal_path.script.json"));
        Assert.Equal("complete", result.FinalSnapshot.TerminalOutcome);
        Assert.Equal("handoff_cath_lab", result.FinalSnapshot.TerminalStateId);
        Assert.Empty(result.Rejections);
        Assert.Empty(result.DebriefFacts.HarmfulActionIds);
        Assert.Equal(88, result.Score.Total); // every positive criterion at full credit
    }

    [Fact]
    public void AlternativePathwayScoresIdenticallyToIdeal()
    {
        // Prasugrel instead of ticagrelor + aspirin before the ECG must earn the
        // exact same score — accepted alternatives, not "guess the author".
        var ideal = Replayer.Run(TestData.StemiCase(), TestData.LoadScript("stemi_ideal_path.script.json"));
        var alt = Replayer.Run(TestData.StemiCase(), TestData.LoadScript("stemi_alternative_path.script.json"));
        Assert.Equal(ideal.Score.Total, alt.Score.Total);
        Assert.Equal("complete", alt.FinalSnapshot.TerminalOutcome);
        Assert.Empty(alt.Rejections);
    }

    [Fact]
    public void DelayedPathCompletesWithTimingLoss()
    {
        var result = Replayer.Run(TestData.StemiCase(), TestData.LoadScript("stemi_delayed_path.script.json"));
        Assert.Equal("complete", result.FinalSnapshot.TerminalOutcome);
        Assert.True(result.Score.Total < 88, $"expected timing loss, got {result.Score.Total}");
        Assert.NotEmpty(result.DebriefFacts.DelayedActionIds);
    }

    [Fact]
    public void HarmfulPathIsPenalizedButStillReachesTheLab()
    {
        var result = Replayer.Run(TestData.StemiCase(), TestData.LoadScript("stemi_harmful_path.script.json"));
        Assert.Equal("complete", result.FinalSnapshot.TerminalOutcome);
        Assert.Contains("give_nsaid_analgesia", result.DebriefFacts.HarmfulActionIds);
        Assert.Contains("give_fibrinolytic", result.DebriefFacts.HarmfulActionIds);
    }

    [Fact]
    public void UnansweredCaseDeterioratesToVfTakeoverNotDeath()
    {
        var result = Replayer.Run(TestData.StemiCase(), TestData.LoadScript("stemi_deterioration_path.script.json"));
        Assert.Equal("deteriorated", result.FinalSnapshot.TerminalOutcome);
        Assert.Equal("vf_arrest_takeover", result.FinalSnapshot.TerminalStateId);
        Assert.Equal("arrest", result.FinalSnapshot.Circulation);
    }

    [Fact]
    public void DischargingTheStemiIsATerminalFailurePath()
    {
        var sim = NewSim();
        sim.ApplyAction("ecg_12lead");
        var result = sim.ApplyAction("disposition_discharge");
        Assert.True(result.Terminated);
        Assert.Equal("discharge", sim.Snapshot().TerminalOutcome);
        Assert.Equal("discharged_stemi", sim.Snapshot().TerminalStateId);
    }

    [Fact]
    public void HandoffAfterDeteriorationIsPartialNotComplete()
    {
        var sim = NewSim();
        sim.ApplyAction("ecg_12lead");
        sim.AdvanceTime(700); // -> 760s: T1 fires (no cath activation)
        Assert.Contains("deteriorated", sim.Snapshot().Flags);
        sim.ApplyAction("activate_cath_lab");
        var result = sim.ApplyAction("disposition_cath_lab");
        Assert.True(result.Terminated);
        Assert.Equal("partial", sim.Snapshot().TerminalOutcome);
        Assert.Equal("handoff_after_deterioration", sim.Snapshot().TerminalStateId);
    }

    // --- timing / causality / state-dependence -----------------------

    [Fact]
    public void NitrateIsNeutralWhileNormotensiveAndHarmfulOnceHypotensive()
    {
        // Normotensive: no BP effect, no penalty.
        var calm = NewSim();
        var atRest = calm.ApplyAction("give_nitroglycerin_sl");
        Assert.Equal(EntryClassification.Neutral, atRest.Event!.Classification);
        Assert.Equal(118, calm.Snapshot().Vitals.SbpMmHg);
        Assert.Equal(
            "avoided",
            calm.CriterionResults().First(c => c.Id == "c_nitrate_in_hypotension").Classification);

        // Hypotensive after T1 (SBP 96): the data-driven conditional-effect rule
        // drops BP a further 15 and the state-constrained harmful criterion fires.
        var late = NewSim();
        late.AdvanceTime(720); // T1: SBP 118-22 = 96
        Assert.Equal(96, late.Snapshot().Vitals.SbpMmHg);
        var inShock = late.ApplyAction("give_nitroglycerin_sl");
        Assert.Equal(EntryClassification.Harmful, inShock.Event!.Classification);
        Assert.Equal(81, late.Snapshot().Vitals.SbpMmHg);
        Assert.Equal(
            "harmful",
            late.CriterionResults().First(c => c.Id == "c_nitrate_in_hypotension").Classification);
    }

    [Fact]
    public void TreatmentsAreGatedOnTheDiagnosticEcg()
    {
        var sim = NewSim();
        var early = sim.ApplyAction("give_ticagrelor");
        Assert.False(early.Accepted); // precondition flag('ecg_done')

        var availability = sim.GetActionAvailability();
        var ticagrelor = availability.First(a => a.ActionId == "give_ticagrelor");
        Assert.True(ticagrelor.Visible);
        Assert.False(ticagrelor.Enabled);

        sim.ApplyAction("ecg_12lead");
        Assert.True(sim.ApplyAction("give_ticagrelor").Accepted);
    }

    [Fact]
    public void TroponinResultReturnsAfterTheLabDelayAndNotBefore()
    {
        var sim = NewSim();
        sim.ApplyAction("order_troponin"); // t=20, result due at t=1220
        sim.ApplyAction("ecg_12lead"); // t=80
        Assert.DoesNotContain("troponin_result", sim.Snapshot().DisclosedFacts);

        // give_aspirin then activate so no deterioration terminal interferes.
        sim.ApplyAction("give_aspirin");
        sim.ApplyAction("activate_cath_lab");
        var wait = sim.AdvanceTime(1200);
        Assert.Contains("troponin_result", sim.Snapshot().DisclosedFacts);
        Assert.Contains(wait.NewlyDisclosedFacts, f => f.Id == "troponin_result" && f.Text.Length > 0);
    }

    [Fact]
    public void EcgResultCarriesTemplateTextAndTheTracingAsset()
    {
        var sim = NewSim();
        var result = sim.ApplyAction("ecg_12lead");
        Assert.True(result.Accepted);
        Assert.Contains("12-lead ECG acquired", result.ResultText);
        Assert.Equal("ecg_stemi_anterior_v1", result.ResultAssetId);
        Assert.Contains(result.NewlyDisclosedFacts, f => f.Id == "ecg_tracing");
    }

    [Fact]
    public void FocusedHistorySurfacesTheSafetyNegatives()
    {
        var sim = NewSim();
        var result = sim.ApplyAction("focused_history");
        var ids = result.NewlyDisclosedFacts.Select(f => f.Id).ToList();
        Assert.Contains("hx_pain_character", ids);
        Assert.Contains("hx_risk_factors", ids);
        Assert.Contains("hx_safety_negatives", ids);
    }

    [Fact]
    public void CriterionResultsClassifyTheIdealRunCorrectly()
    {
        var stemiCase = TestData.StemiCase();
        var sim = new Simulation(stemiCase, Seed).Initialize();
        foreach (var step in TestData.LoadScript("stemi_ideal_path.script.json").Steps)
        {
            sim.ApplyAction(step.Action!);
        }
        var results = sim.CriterionResults();
        Assert.All(
            results.Where(c => !c.Harmful),
            c => Assert.Equal("correct", c.Classification));
        Assert.All(
            results.Where(c => c.Harmful),
            c => Assert.Equal("avoided", c.Classification));
        Assert.Equal(stemiCase.ScoringCriteria.Count, results.Count);
    }

    [Fact]
    public void BrokenResultTemplateReferenceFailsAtLoad()
    {
        var json = System.IO.File.ReadAllText(TestData.StemiCasePath)
            .Replace("\"resultTemplateId\": \"ecg_result\"", "\"resultTemplateId\": \"no_such_template\"");
        Assert.Throws<CaseLoadException>(() => CaseLoader.FromJson(json));
    }

    private static Simulation NewSim() => new Simulation(TestData.StemiCase(), Seed).Initialize();
}
