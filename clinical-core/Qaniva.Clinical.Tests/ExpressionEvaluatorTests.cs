using Qaniva.Clinical.Core.Engine;
using Qaniva.Clinical.Core.Model;
using Xunit;

namespace Qaniva.Clinical.Tests;

public class ExpressionEvaluatorTests
{
    private static PatientState State()
    {
        var s = new PatientState
        {
            Rhythm = "demo_bradycardia",
            Circulation = "poor_perfusion",
            PainScore = 2,
            SimTimeSec = 240,
        };
        s.Vitals.Hr = 38;
        s.Vitals.SbpMmHg = 84;
        s.Flags.Add("monitor_on");
        s.DisclosedFacts.Add("onset_1h");
        s.ActionCounts["ecg_12lead"] = 1;
        return s;
    }

    [Theory]
    [InlineData("simTimeSec >= 300", false)]
    [InlineData("simTimeSec >= 240", true)]
    [InlineData("vitals.hr <= 20", false)]
    [InlineData("vitals.hr < 40 && vitals.sbpMmHg < 90", true)]
    [InlineData("flag('monitor_on')", true)]
    [InlineData("flag('atropine_given')", false)]
    [InlineData("!flag('atropine_given')", true)]
    [InlineData("flag('atropine_given') || simTimeSec >= 180", true)]
    [InlineData("disclosed('onset_1h') && !disclosed('nope')", true)]
    [InlineData("actionCount('ecg_12lead') == 1", true)]
    [InlineData("rhythm == 'demo_bradycardia'", true)]
    [InlineData("circulation != 'normal'", true)]
    [InlineData("(simTimeSec >= 300) || (vitals.hr < 40 && flag('monitor_on'))", true)]
    public void EvaluatesBooleanExpressions(string expr, bool expected)
    {
        Assert.Equal(expected, ExpressionEvaluator.EvaluateBool(expr, State()));
    }

    [Theory]
    [InlineData("simTimeSec >")]
    [InlineData("flag(monitor_on)")]
    [InlineData("vitals.unknownVital > 1")]
    [InlineData("bogusAccessor")]
    [InlineData("1 = 1")]
    public void RejectsMalformedExpressions(string expr)
    {
        Assert.ThrowsAny<ExpressionException>(() => ExpressionEvaluator.EvaluateBool(expr, State()));
    }

    [Fact]
    public void NonBooleanResultInBooleanContextThrows()
    {
        Assert.Throws<ExpressionException>(() => ExpressionEvaluator.EvaluateBool("simTimeSec", State()));
    }
}
