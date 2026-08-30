using System.Linq;
using Qaniva.Clinical.Core.Replay;
using Xunit;

namespace Qaniva.Clinical.Tests;

public class DeterminismTests
{
    [Theory]
    [InlineData("ideal_path.script.json")]
    [InlineData("harmful_path.script.json")]
    public void SameScriptProducesIdenticalResult(string scriptFile)
    {
        var caseDefinition = TestData.DemoCase();
        var script = TestData.LoadScript(scriptFile);

        var runA = Replayer.Run(caseDefinition, script);
        var runB = Replayer.Run(caseDefinition, script);

        Assert.Equal(runA.ReplayHash, runB.ReplayHash);
        Assert.Equal(runA.FinalSnapshot.StateHash, runB.FinalSnapshot.StateHash);
        Assert.Equal(runA.Score.Total, runB.Score.Total);
        Assert.Equal(
            GoldenSerializer.ToGoldenJson(runA),
            GoldenSerializer.ToGoldenJson(runB));
    }

    [Fact]
    public void TimelineHashesChainCorrectly()
    {
        var caseDefinition = TestData.DemoCase();
        var result = Replayer.Run(caseDefinition, TestData.LoadScript("ideal_path.script.json"));

        var events = result.Timeline.Events;
        Assert.NotEmpty(events);

        // Each event's beforeHash must equal the previous event's afterHash.
        for (int i = 1; i < events.Count; i++)
        {
            Assert.Equal(events[i - 1].AfterHash, events[i].BeforeHash);
        }

        // Sequence numbers are contiguous from 0.
        Assert.Equal(Enumerable.Range(0, events.Count), events.Select(e => e.Seq));
    }

    [Fact]
    public void DifferentSeedKeepsDeterminismForNonStochasticCase()
    {
        // The demo case has no stochastic content, so seed must not change the outcome.
        var caseDefinition = TestData.DemoCase();
        var script = TestData.LoadScript("ideal_path.script.json");

        var withSeedA = Replayer.Run(caseDefinition, Reseed(script, 1));
        var withSeedB = Replayer.Run(caseDefinition, Reseed(script, 999));

        Assert.Equal(withSeedA.FinalSnapshot.StateHash, withSeedB.FinalSnapshot.StateHash);
        Assert.Equal(withSeedA.Score.Total, withSeedB.Score.Total);
    }

    private static AttemptScript Reseed(AttemptScript script, ulong seed) => new()
    {
        CaseId = script.CaseId,
        CaseVersion = script.CaseVersion,
        Seed = seed,
        Steps = script.Steps,
    };
}
