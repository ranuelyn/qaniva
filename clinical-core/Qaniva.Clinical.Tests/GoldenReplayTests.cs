using Qaniva.Clinical.Core.Replay;
using Xunit;

namespace Qaniva.Clinical.Tests;

/// <summary>
/// Locks the engine's observable output for known scripts. If a change to the
/// engine or the demo case shifts a hash, score, or classification, this fails and
/// forces a human to review the diff (regenerate with UPDATE_GOLDEN=1 once reviewed).
/// </summary>
public class GoldenReplayTests
{
    [Theory]
    [InlineData("ideal_path.script.json", "ideal_path.golden.json")]
    [InlineData("harmful_path.script.json", "harmful_path.golden.json")]
    public void ReplayMatchesGolden(string scriptFile, string goldenFile)
    {
        var caseDefinition = TestData.DemoCase();
        var result = Replayer.Run(caseDefinition, TestData.LoadScript(scriptFile));

        var expected = TestData.ReadOrUpdateGolden(goldenFile, result).Trim();
        var actual = GoldenSerializer.ToGoldenJson(result).Trim();

        Assert.Equal(GoldenSerializer.Normalize(expected), GoldenSerializer.Normalize(actual));
    }

    [Fact]
    public void IdealPathScoresWellAndReachesComplete()
    {
        var result = Replayer.Run(TestData.DemoCase(), TestData.LoadScript("ideal_path.script.json"));
        Assert.Equal("complete", result.FinalSnapshot.TerminalOutcome);
        Assert.Empty(result.Rejections);
        Assert.True(result.Score.Total >= 80, $"expected a strong score, got {result.Score.Total}");
        Assert.Empty(result.DebriefFacts.HarmfulActionIds);
    }

    [Fact]
    public void HarmfulPathEndsInDeathWithPenalty()
    {
        var result = Replayer.Run(TestData.DemoCase(), TestData.LoadScript("harmful_path.script.json"));
        Assert.Equal("death", result.FinalSnapshot.TerminalOutcome);
        Assert.Contains("give_demo_contraindicated_drug", result.DebriefFacts.HarmfulActionIds);
        Assert.True(result.Score.Total < 0, $"expected a negative score, got {result.Score.Total}");
    }
}
