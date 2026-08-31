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
                // A harmful criterion with state constraints only counts when the
                // constraints hold at (post-)action time — e.g. "nitrate is harmful
                // only while hypotensive". Constraint-free harmful criteria are
                // unconditional, exactly as before.
                if (!StateConstraintsMet(criterion, postState))
                {
                    continue;
                }
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
            outcome.TimingMultiplier = multiplier;
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

    /// <summary>
    /// Per-criterion debrief facts, in case-definition order. Deterministic — the
    /// debrief/results layers render these, never recompute them. Classifications:
    /// non-harmful: correct | delayed (timing window partially missed) | missed;
    /// harmful: harmful (performed) | avoided (not performed).
    /// </summary>
    public IReadOnlyList<CriterionResult> BuildCriterionResults()
    {
        var results = new List<CriterionResult>(_case.ScoringCriteria.Count);
        foreach (var criterion in _case.ScoringCriteria)
        {
            var outcome = _outcomes[criterion.Id];
            string classification;
            if (criterion.Harmful)
            {
                classification = outcome.Credited ? "harmful" : "avoided";
            }
            else if (!outcome.Credited)
            {
                classification = "missed";
            }
            else
            {
                classification = outcome.TimingMultiplier >= 0.999 ? "correct" : "delayed";
            }

            var acceptedLabels = new List<string>();
            foreach (var actionId in criterion.AcceptedActions)
            {
                var action = _case.AvailableActions.Find(a => a.Id == actionId);
                acceptedLabels.Add(action?.Label ?? actionId);
            }

            results.Add(new CriterionResult(
                criterion.Id,
                criterion.Label,
                criterion.Category,
                criterion.Criticality,
                criterion.Harmful,
                classification,
                outcome.Credited,
                outcome.Credited ? outcome.CreditedAtSec : -1,
                outcome.Credited ? outcome.AwardedPoints : 0,
                criterion.Harmful ? -criterion.Points : criterion.Points,
                new List<string>(criterion.EvidenceRefs),
                acceptedLabels));
        }
        return results;
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
        public double TimingMultiplier { get; set; } = 1.0;
    }
}

/// <summary>One rubric criterion's final, deterministic outcome for the debrief.</summary>
public sealed class CriterionResult
{
    public CriterionResult(
        string id,
        string label,
        string category,
        string criticality,
        bool harmful,
        string classification,
        bool credited,
        int creditedAtSec,
        double awardedPoints,
        double maxPoints,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> acceptedActionLabels)
    {
        Id = id;
        Label = label;
        Category = category;
        Criticality = criticality;
        Harmful = harmful;
        Classification = classification;
        Credited = credited;
        CreditedAtSec = creditedAtSec;
        AwardedPoints = awardedPoints;
        MaxPoints = maxPoints;
        EvidenceRefs = evidenceRefs;
        AcceptedActionLabels = acceptedActionLabels;
    }

    public string Id { get; }
    public string Label { get; }
    public string Category { get; }
    public string Criticality { get; }
    public bool Harmful { get; }
    /// <summary>correct | delayed | missed | harmful | avoided</summary>
    public string Classification { get; }
    public bool Credited { get; }
    /// <summary>-1 when never credited.</summary>
    public int CreditedAtSec { get; }
    public double AwardedPoints { get; }
    /// <summary>Positive max for scored criteria; negative magnitude for harmful penalties.</summary>
    public double MaxPoints { get; }

    /// <summary>Evidence-ledger ids from the case rubric (learner-visible traceability).</summary>
    public IReadOnlyList<string> EvidenceRefs { get; }

    /// <summary>Labels of every accepted action (alternatives surface in the debrief).</summary>
    public IReadOnlyList<string> AcceptedActionLabels { get; }
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
