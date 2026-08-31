using System.Collections.Generic;
using System.Linq;
using Qaniva.Clinical.Core.Engine;
using Qaniva.Clinical.Core.Model;
using Qaniva.Clinical.Core.Replay;
using Xunit;

namespace Qaniva.Clinical.Tests;

/// <summary>
/// Behavioural + golden coverage for the second production-like case
/// (anaphylaxis_food_001, MVP DEMO APPROVED / clinical validation pending).
/// Also the case-factory proof: this suite required NO new engine capability.
/// </summary>
public class AnaphylaxisCaseTests
{
    private const ulong Seed = 20260901;

    [Theory]
    [InlineData("ana_optimal_path.script.json", "ana_optimal_path.golden.json")]
    [InlineData("ana_delayed_epi_path.script.json", "ana_delayed_epi_path.golden.json")]
    [InlineData("ana_alternative_path.script.json", "ana_alternative_path.golden.json")]
    [InlineData("ana_thorough_path.script.json", "ana_thorough_path.golden.json")]
    [InlineData("ana_harmful_route_path.script.json", "ana_harmful_route_path.golden.json")]
    [InlineData("ana_deterioration_path.script.json", "ana_deterioration_path.golden.json")]
    public void ReplayMatchesGolden(string scriptFile, string goldenFile)
    {
        var result = Replayer.Run(TestData.AnaphylaxisCase(), TestData.LoadScript(scriptFile));
        var expected = TestData.ReadOrUpdateGolden(goldenFile, result).Trim();
        var actual = GoldenSerializer.ToGoldenJson(result).Trim();
        Assert.Equal(GoldenSerializer.Normalize(expected), GoldenSerializer.Normalize(actual));
    }

    [Fact]
    public void OptimalPathEarnsFullScoreAndTheEpiResponseIsVisible()
    {
        var result = Replayer.Run(TestData.AnaphylaxisCase(), TestData.LoadScript("ana_optimal_path.script.json"));
        Assert.Equal("complete", result.FinalSnapshot.TerminalOutcome);
        Assert.Equal("observed_in_ed", result.FinalSnapshot.TerminalStateId);
        Assert.Empty(result.Rejections);
        Assert.Equal(80, result.Score.Total);
        // epi_response fired: BP recovered, breathing back to spontaneous.
        Assert.True(result.FinalSnapshot.Vitals.SbpMmHg > 95, $"SBP {result.FinalSnapshot.Vitals.SbpMmHg}");
        Assert.Equal("spontaneous", result.FinalSnapshot.Breathing);
        Assert.Equal("patent", result.FinalSnapshot.Airway);
    }

    [Fact]
    public void AlternativePathwayScoresIdenticallyToOptimal()
    {
        // Different order + the OTHER reassessment exam + the ADMIT disposition
        // must earn the exact same score (two alternatives-equivalence criteria).
        var optimal = Replayer.Run(TestData.AnaphylaxisCase(), TestData.LoadScript("ana_optimal_path.script.json"));
        var alt = Replayer.Run(TestData.AnaphylaxisCase(), TestData.LoadScript("ana_alternative_path.script.json"));
        Assert.Equal(optimal.Score.Total, alt.Score.Total);
        Assert.Equal("admit", alt.FinalSnapshot.TerminalOutcome);
        Assert.Empty(alt.Rejections);
    }

    [Fact]
    public void AdjunctsFirstDelaysEpinephrineAndEndsPartialAfterDeterioration()
    {
        var result = Replayer.Run(TestData.AnaphylaxisCase(), TestData.LoadScript("ana_delayed_epi_path.script.json"));
        Assert.Equal("partial", result.FinalSnapshot.TerminalOutcome);
        Assert.Equal("observed_after_deterioration", result.FinalSnapshot.TerminalStateId);
        Assert.Contains("deteriorated", result.FinalSnapshot.Flags);
        Assert.Contains("give_epinephrine_im", result.DebriefFacts.DelayedActionIds);
        Assert.True(result.Score.Total < 80, $"expected timing loss, got {result.Score.Total}");
    }

    [Fact]
    public void ThoroughPathWithSecondEpiDoseStillEarnsFullScore()
    {
        // Extra adjuncts/labs and a repeat epinephrine dose cost TIME, not points
        // (this case has no efficiency trap by design — blueprint).
        var result = Replayer.Run(TestData.AnaphylaxisCase(), TestData.LoadScript("ana_thorough_path.script.json"));
        Assert.Equal(80, result.Score.Total);
        Assert.Equal("complete", result.FinalSnapshot.TerminalOutcome);
        Assert.Empty(result.Rejections); // second IM dose is accepted (repeatable)
    }

    [Fact]
    public void IvBolusEpinephrineIsPenalizedWithACanonicalSurge()
    {
        var result = Replayer.Run(TestData.AnaphylaxisCase(), TestData.LoadScript("ana_harmful_route_path.script.json"));
        Assert.Contains("give_epinephrine_iv_push", result.DebriefFacts.HarmfulActionIds);
        Assert.Equal(68, result.Score.Total); // 80 - 12 route penalty
        Assert.Equal("complete", result.FinalSnapshot.TerminalOutcome);
    }

    [Fact]
    public void IvPushSurgeIsVisibleOnVitalsBeforeTheImDoseCorrects()
    {
        var sim = new Simulation(TestData.AnaphylaxisCase(), Seed).Initialize();
        sim.ApplyAction("attach_monitor");
        var push = sim.ApplyAction("give_epinephrine_iv_push");
        Assert.Contains("iv_epi_surge", push.Event!.TriggeredRuleIds);
        Assert.Equal(118 + 38, sim.Snapshot().Vitals.Hr);
        Assert.Equal(88 + 30, sim.Snapshot().Vitals.SbpMmHg);
    }

    [Fact]
    public void ReassessmentOnlyCountsAfterEpinephrine()
    {
        var sim = new Simulation(TestData.AnaphylaxisCase(), Seed).Initialize();
        sim.ApplyAction("exam_lungs"); // before treatment: not reassessment
        Assert.Equal(
            "missed",
            sim.CriterionResults().First(c => c.Id == "c_reassess_after_epi").Classification);

        sim.ApplyAction("give_epinephrine_im");
        sim.ApplyAction("exam_lungs"); // repeatable exam, now post-epi
        Assert.Equal(
            "correct",
            sim.CriterionResults().First(c => c.Id == "c_reassess_after_epi").Classification);
    }

    [Fact]
    public void UntreatedReactionProgressesToPeriArrestTakeover()
    {
        var result = Replayer.Run(TestData.AnaphylaxisCase(), TestData.LoadScript("ana_deterioration_path.script.json"));
        Assert.Equal("deteriorated", result.FinalSnapshot.TerminalOutcome);
        Assert.Equal("peri_arrest_takeover", result.FinalSnapshot.TerminalStateId);
        Assert.Equal("arrest", result.FinalSnapshot.Circulation);
        Assert.Equal("obstructed", result.FinalSnapshot.Airway);
        Assert.Equal(10, result.Score.Total); // history + monitor only
    }

    [Fact]
    public void DispositionsRequireEpinephrineFirst()
    {
        var sim = new Simulation(TestData.AnaphylaxisCase(), Seed).Initialize();
        Assert.False(sim.ApplyAction("disposition_observation").Accepted);
        var availability = sim.GetActionAvailability();
        Assert.False(availability.First(a => a.ActionId == "disposition_observation").Enabled);
        // Premature discharge stays possible — that is the modeled error path.
        Assert.True(availability.First(a => a.ActionId == "disposition_discharge_early").Enabled);
    }

    [Fact]
    public void RuleDebriefTextsSurfaceInTheEngineData()
    {
        var caseDef = TestData.AnaphylaxisCase();
        var t1 = caseDef.TransitionRules.First(r => r.Id == "t1_reaction_worsens");
        Assert.False(string.IsNullOrEmpty(t1.DebriefText));
        var sim = new Simulation(caseDef, Seed).Initialize();
        var wait = sim.AdvanceTime(400);
        Assert.Contains("t1_reaction_worsens", wait.Event!.TriggeredRuleIds);
    }

    [Fact]
    public void GoldenScriptsAreDeterministicAcrossRuns()
    {
        foreach (var script in new[] { "ana_optimal_path.script.json", "ana_deterioration_path.script.json" })
        {
            var a = Replayer.Run(TestData.AnaphylaxisCase(), TestData.LoadScript(script));
            var b = Replayer.Run(TestData.AnaphylaxisCase(), TestData.LoadScript(script));
            Assert.Equal(a.ReplayHash, b.ReplayHash);
        }
    }
}
