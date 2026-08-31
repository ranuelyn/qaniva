using System.Linq;
using Qaniva.Clinical.Core.Engine;
using Xunit;

namespace Qaniva.Clinical.Tests;

/// <summary>
/// Locks the canonical hidden / visible+disabled / enabled projection that UI
/// layers render. The demo case exercises all three states without any client
/// re-deriving clinical logic.
/// </summary>
public class ActionAvailabilityTests
{
    private static Simulation FreshSim() => new Simulation(TestData.DemoCase(), 20260830).Initialize();

    private static Qaniva.Clinical.Core.Model.ActionAvailability Of(Simulation sim, string id) =>
        sim.GetActionAvailability().Single(a => a.ActionId == id);

    [Fact]
    public void InitialStateExposesAllThreeStates()
    {
        var sim = FreshSim();

        var monitor = Of(sim, "attach_monitor");
        Assert.True(monitor.Visible);
        Assert.True(monitor.Enabled);
        Assert.Null(monitor.DisabledReason);

        // give_atropine is VISIBLE but DISABLED: precondition flag('iv_access').
        var atropine = Of(sim, "give_atropine");
        Assert.True(atropine.Visible);
        Assert.False(atropine.Enabled);
        Assert.Contains("iv_access", atropine.DisabledReason);

        // transcutaneous_pacing is HIDDEN until its visibleWhen holds.
        var pacing = Of(sim, "transcutaneous_pacing");
        Assert.False(pacing.Visible);
        Assert.False(pacing.Enabled);
        Assert.Null(pacing.DisabledReason);
    }

    [Fact]
    public void SatisfyingAPreconditionEnablesTheAction()
    {
        var sim = FreshSim();
        Assert.True(sim.ApplyAction("iv_access").Accepted);

        var atropine = Of(sim, "give_atropine");
        Assert.True(atropine.Visible);
        Assert.True(atropine.Enabled);
    }

    [Fact]
    public void UsedNonRepeatableActionBecomesVisibleButDisabled()
    {
        var sim = FreshSim();
        Assert.True(sim.ApplyAction("attach_monitor").Accepted);

        var monitor = Of(sim, "attach_monitor");
        Assert.True(monitor.Visible);
        Assert.False(monitor.Enabled);
        Assert.Equal("already performed", monitor.DisabledReason);
    }

    [Fact]
    public void HiddenActionAppearsWhenItsConditionHolds()
    {
        var sim = FreshSim();
        sim.ApplyAction("attach_monitor");
        sim.AdvanceTime(200); // pacing becomes visible at simTimeSec >= 180

        var pacing = Of(sim, "transcutaneous_pacing");
        Assert.True(pacing.Visible);
        Assert.True(pacing.Enabled);
    }

    [Fact]
    public void OfferabilityEqualsVisibleAndEnabledForEveryAction()
    {
        var sim = FreshSim();
        sim.ApplyAction("attach_monitor");
        sim.ApplyAction("iv_access");

        var offerable = sim.GetAvailableActions().Select(a => a.Id).OrderBy(x => x).ToArray();
        var projected = sim.GetActionAvailability()
            .Where(a => a.Visible && a.Enabled)
            .Select(a => a.ActionId)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(offerable, projected);
    }

    [Fact]
    public void TerminalStateExposesNoActions()
    {
        var sim = FreshSim();
        sim.ApplyAction("attach_monitor");
        sim.ApplyAction("iv_access");
        sim.ApplyAction("give_atropine");
        var result = sim.ApplyAction("disposition_ccu");

        Assert.True(result.Terminated);
        Assert.Empty(sim.GetActionAvailability());
    }
}
