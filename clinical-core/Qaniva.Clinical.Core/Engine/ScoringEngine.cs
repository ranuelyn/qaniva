using System;
using System.Collections.Generic;
using System.Linq;
using Qaniva.Clinical.Core.Model;

namespace Qaniva.Clinical.Core.Engine;

/// <summary>
/// Deterministic scoring over a case's rubric. All numbers come from the case
/// definition and the recorded timeline — never from an LLM.
/// </summary>
public sealed class ScoringEngine
{
    private readonly CaseDefinition _case;
    private readonly Dictionary<string, CriterionOutcome> _outcomes;

    public ScoringEngine(CaseDefinition caseDefinition)
    {
        _case = caseDefinition;
        _outcomes = _case.ScoringCriteria.ToDictionary(
            c => c.Id,
            c => new CriterionOutcome(c.Id),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Evaluate the criteria affected by <paramref name="actionId"/> at the given
    /// simulated time and post-action state. Returns the score delta produced by
    /// this action and the strongest classification it earned.
    /// </summary>
    public ActionScoring ScoreAction(string actionId, int simTimeSec, PatientState postState)
    {
        double delta = 0;
        var classification = EntryClassification.Neutral;

        foreach (var criterion in _case.ScoringCriteria)
        {
            if (!criterion.AcceptedActions.Contains(actionId))
            {
                continue;
            }

            var outcome = _outcomes[criterion.Id];

            if (criterion.Harmful)
            {
                if (!outcome.Credited)
                {
                    outcome.Credited = true;
                    outcome.CreditedAtSec = simTimeSec;
                    outcome.AwardedPoints = -criterion.Points;
                    delta -= criterion.Points;
                }
                classification = EntryClassification.Harmful;
                continue;
            }

            if (outcome.Credited)
            {
                continue;
            }

            if (!StateConstraintsMet(criterion, postState))
            {
                continue;
            }

            double multiplier = TimingMultiplier(criterion.TimingWindow, simTimeSec);
            double awarded = Math.Round(criterion.Points * multiplier, 4);

            outcome.Credited = true;
            outcome.CreditedAtSec = simTimeSec;
            outcome.AwardedPoints = awarded;
            delta += awarded;

            if (classification != EntryClassification.Harmful)
            {
                classification = multiplier >= 0.999
                    ? EntryClassification.Correct
                    : EntryClassification.Delayed;
            }
        }

        return new ActionScoring(delta, classification);
    }

    public AttemptScore BuildFinalScore()
    {
        var breakdown = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["critical"] = 0,
            ["timing"] = 0,
            ["efficiency"] = 0,
            ["treatment"] = 0,
            ["disposition"] = 0,
        };

        var missed = new List<string>();

        foreach (var criterion in _case.ScoringCriteria)
        {
            var outcome = _outcomes[criterion.Id];
            string bucket = breakdown.ContainsKey(criterion.Category) ? criterion.Category : "treatment";

            if (criterion.Harmful)
            {
                if (outcome.Credited)
                {
                    breakdown[bucket] += outcome.AwardedPoints; // negative
                }
                continue;
            }

            if (outcome.Credited)
            {
                breakdown[bucket] += outcome.AwardedPoints;
            }
            else
            {
                missed.Add(criterion.Id);
            }
        }

        double total = breakdown.Values.Sum();

        return new AttemptScore(
            Math.Round(total, 4),
            new ScoreBreakdown(
                Math.Round(breakdown["critical"], 4),
                Math.Round(breakdown["timing"], 4),
                Math.Round(breakdown["efficiency"], 4),
                Math.Round(breakdown["treatment"], 4),
                Math.Round(breakdown["disposition"], 4)),
            missed);
    }

    private bool StateConstraintsMet(ScoringCriterion criterion, PatientState state)
    {
        foreach (var expr in criterion.StateConstraints)
        {
            if (!ExpressionEvaluator.EvaluateBool(expr, state))
            {
                return false;
            }
        }
        return true;
    }

    private static double TimingMultiplier(TimingWindow? window, int simTimeSec)
    {
        if (window is null)
        {
            return 1.0;
        }
        if (simTimeSec <= window.FullCreditBeforeSec)
        {
            return 1.0;
        }
        if (simTimeSec >= window.ZeroCreditAfterSec)
        {
            return 0.0;
        }
        double span = window.ZeroCreditAfterSec - window.FullCreditBeforeSec;
        if (span <= 0)
        {
            return 0.0;
        }
        return 1.0 - ((simTimeSec - window.FullCreditBeforeSec) / span);
    }

    private sealed class CriterionOutcome
    {
        public CriterionOutcome(string id) => Id = id;

        public string Id { get; }
        public bool Credited { get; set; }
        public int CreditedAtSec { get; set; }
        public double AwardedPoints { get; set; }
    }
}

public readonly struct ActionScoring
{
    public ActionScoring(double delta, EntryClassification classification)
    {
        Delta = delta;
        Classification = classification;
    }

    public double Delta { get; }
    public EntryClassification Classification { get; }
}

public sealed class AttemptScore
{
    public AttemptScore(double total, ScoreBreakdown breakdown, IReadOnlyList<string> missedCriterionIds)
    {
        Total = total;
        Breakdown = breakdown;
        MissedCriterionIds = missedCriterionIds;
    }

    public double Total { get; }
    public ScoreBreakdown Breakdown { get; }
    public IReadOnlyList<string> MissedCriterionIds { get; }
}

public sealed class ScoreBreakdown
{
    public ScoreBreakdown(double critical, double timing, double efficiency, double treatment, double disposition)
    {
        Critical = critical;
        Timing = timing;
        Efficiency = efficiency;
        Treatment = treatment;
        Disposition = disposition;
    }

    public double Critical { get; }
    public double Timing { get; }
    public double Efficiency { get; }
    public double Treatment { get; }
    public double Disposition { get; }
}
