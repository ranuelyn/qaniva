using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qaniva.Clinical.Core.Replay;

/// <summary>
/// Canonical, stable JSON view of a <see cref="ReplayResult"/>. Used by both the
/// CLI and the golden tests so they can never disagree about the expected shape.
/// </summary>
public static class GoldenSerializer
{
    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static object ToGoldenObject(ReplayResult result)
    {
        var snap = result.FinalSnapshot;
        return new
        {
            replayHash = result.ReplayHash,
            finalStateHash = snap.StateHash,
            outcome = snap.TerminalOutcome ?? "incomplete",
            terminalStateId = snap.TerminalStateId,
            simTimeSec = snap.SimTimeSec,
            vitals = new
            {
                hr = snap.Vitals.Hr,
                sbpMmHg = snap.Vitals.SbpMmHg,
                dbpMmHg = snap.Vitals.DbpMmHg,
                spo2 = snap.Vitals.Spo2,
                rrPerMin = snap.Vitals.RrPerMin,
                tempC = snap.Vitals.TempC,
            },
            circulation = snap.Circulation,
            neuro = snap.Neuro,
            flags = snap.Flags.OrderBy(f => f, System.StringComparer.Ordinal).ToArray(),
            disclosedFacts = snap.DisclosedFacts.OrderBy(f => f, System.StringComparer.Ordinal).ToArray(),
            score = new
            {
                total = result.Score.Total,
                critical = result.Score.Breakdown.Critical,
                timing = result.Score.Breakdown.Timing,
                efficiency = result.Score.Breakdown.Efficiency,
                treatment = result.Score.Breakdown.Treatment,
                disposition = result.Score.Breakdown.Disposition,
                missedCriterionIds = result.Score.MissedCriterionIds.ToArray(),
            },
            timeline = result.Timeline.Events.Select(e => new
            {
                seq = e.Seq,
                simTimeSec = e.SimTimeSec,
                actionId = e.ActionId,
                classification = e.Classification.ToString(),
                scoreDelta = e.ScoreDelta,
                triggeredRuleIds = e.TriggeredRuleIds.ToArray(),
                beforeHash = e.BeforeHash,
                afterHash = e.AfterHash,
            }).ToArray(),
            rejections = result.Rejections.ToArray(),
        };
    }

    public static string ToGoldenJson(ReplayResult result) =>
        JsonSerializer.Serialize(ToGoldenObject(result), PrettyOptions);

    public static string Normalize(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement, PrettyOptions);
    }
}
