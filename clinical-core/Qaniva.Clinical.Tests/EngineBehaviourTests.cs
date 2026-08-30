using System.Linq;
using Qaniva.Clinical.Core.Engine;
using Qaniva.Clinical.Core.Model;
using Xunit;

namespace Qaniva.Clinical.Tests;

public class EngineBehaviourTests
{
    private static Simulation FreshSim() => new Simulation(TestData.DemoCase(), 20260830).Initialize();

    [Fact]
    public void InvalidActionLeavesStateUnchanged()
    {
        var sim = FreshSim();
        var hashBefore = sim.Snapshot().StateHash;

        // give_atropine requires IV access.
        var result = sim.ApplyAction("give_atropine");

        Assert.False(result.Accepted);
        Assert.Contains("Precondition not met", result.RejectionReason);
        Assert.Equal(hashBefore, sim.Snapshot().StateHash);
        Assert.Equal(0, sim.Timeline.Count);
    }

    [Fact]
    public void UnknownActionIsRejected()
    {
        var sim = FreshSim();
        var result = sim.ApplyAction("teleport_patient");
        Assert.False(result.Accepted);
        Assert.Equal(0, sim.Timeline.Count);
    }

    [Fact]
    public void NonRepeatableActionCannotRunTwice()
    {
        var sim = FreshSim();
        Assert.True(sim.ApplyAction("attach_monitor").Accepted);
        var second = sim.ApplyAction("attach_monitor");
        Assert.False(second.Accepted);
        Assert.Contains("already been performed", second.RejectionReason);
    }

    [Fact]
    public void TimeTransitionTriggersDeteriorationRuleViaClock()
    {
        var sim = FreshSim();
        double hrStart = sim.Snapshot().Vitals.Hr;

        // No treatment given; advance past the 300s deterioration threshold.
        var result = sim.AdvanceTime(305);

        Assert.True(result.Accepted);
        Assert.Contains("deterioration_untreated", result.Event!.TriggeredRuleIds);
        Assert.True(sim.Snapshot().Vitals.Hr < hrStart);
        Assert.Contains("deteriorating", sim.Snapshot().Flags);
        Assert.Contains("distress_severe", result.PresentationCues);
    }

    [Fact]
    public void DeteriorationRuleDoesNotFireOnceTreated()
    {
        var sim = FreshSim();
        Assert.True(sim.ApplyAction("attach_monitor").Accepted);
        Assert.True(sim.ApplyAction("iv_access").Accepted);
        Assert.True(sim.ApplyAction("give_atropine").Accepted);

        var afterTreatment = sim.Snapshot().Vitals.Hr;
        var result = sim.AdvanceTime(600);

        Assert.DoesNotContain("deterioration_untreated", result.Event!.TriggeredRuleIds);
        Assert.Equal(afterTreatment, sim.Snapshot().Vitals.Hr);
        Assert.Contains("stabilized", sim.Snapshot().Flags);
    }

    [Fact]
    public void GivingContraindicatedDrugIsScoredHarmful()
    {
        var sim = FreshSim();
        var result = sim.ApplyAction("give_demo_contraindicated_drug");

        Assert.True(result.Accepted);
        Assert.Equal(EntryClassification.Harmful, result.Event!.Classification);
        Assert.True(result.Event.ScoreDelta < 0);
    }

    [Fact]
    public void VisibilityGuardHidesPacingUntilConditionMet()
    {
        var sim = FreshSim();
        sim.ApplyAction("attach_monitor"); // pacing needs monitor_on as a precondition
        Assert.DoesNotContain("transcutaneous_pacing", sim.GetAvailableActions().Select(a => a.Id));

        // Becomes visible after 180s even without atropine.
        sim.AdvanceTime(200);
        Assert.Contains("transcutaneous_pacing", sim.GetAvailableActions().Select(a => a.Id));
    }

    [Fact]
    public void DispositionDrivesTerminalComplete()
    {
        var sim = FreshSim();
        sim.ApplyAction("attach_monitor");
        sim.ApplyAction("iv_access");
        sim.ApplyAction("give_atropine");
        var result = sim.ApplyAction("disposition_ccu");

        Assert.True(result.Terminated);
        Assert.True(sim.IsTerminated);
        Assert.Equal("complete", sim.TerminalOutcome);
        Assert.Empty(sim.GetAvailableActions());

        // Actions after terminal are rejected.
        Assert.False(sim.ApplyAction("consult_cardiology").Accepted);
    }

    [Fact]
    public void IgnoringAnUnstablePatientReachesTerminalDeath()
    {
        var sim = FreshSim();
        sim.ApplyAction("give_demo_contraindicated_drug"); // hr 38 -> 26
        var result = sim.AdvanceTime(305); // deterioration -> hr 11 -> arrest rule -> death

        Assert.True(result.Terminated);
        Assert.Equal("death", sim.TerminalOutcome);
        Assert.Contains("deterioration_untreated", result.Event!.TriggeredRuleIds);
        Assert.Contains("arrest_if_ignored", result.Event!.TriggeredRuleIds);
    }
}
