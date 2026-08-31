using System;
using System.Collections.Generic;
using System.Linq;
using Qaniva.Clinical.Core.Model;

namespace Qaniva.Clinical.Core.Engine;

/// <summary>
/// The deterministic clinical simulation. Given the same <see cref="CaseDefinition"/>,
/// the same seed, and the same ordered actions, it always produces the same timeline,
/// final state, and score. No Unity, no wall-clock, no unseeded randomness.
/// </summary>
public sealed class Simulation
{
    private const int MaxRuleCascadeIterations = 16;
    private const string WaitActionId = "__wait__";

    private readonly CaseDefinition _case;
    private readonly ScoringEngine _scoring;
    private readonly AttemptTimeline _timeline = new();
    private readonly SortedDictionary<string, int> _pendingRules = new(StringComparer.Ordinal);

    private PatientState _state;
    private int _seq;

    public Simulation(CaseDefinition caseDefinition, ulong seed)
    {
        _case = caseDefinition ?? throw new ArgumentNullException(nameof(caseDefinition));
        Seed = seed;
        Rng = new DeterministicRng(seed);
        _scoring = new ScoringEngine(_case);
        _state = PatientState.FromInitial(_case.InitialState);
    }

    public ulong Seed { get; }

    public DeterministicRng Rng { get; }

    public bool IsTerminated { get; private set; }

    public string? TerminalStateId { get; private set; }

    public string? TerminalOutcome { get; private set; }

    public AttemptTimeline Timeline => _timeline;

    public int SimTimeSec => _state.SimTimeSec;

    /// <summary>
    /// Prepare the simulation: run one rule pass at t=0 so any immediately-true
    /// rule (or terminal condition) is reflected before the first action.
    /// </summary>
    public Simulation Initialize()
    {
        if (_seq != 0 || _timeline.Count != 0)
        {
            throw new InvalidOperationException("Initialize() must be called exactly once, before any action.");
        }

        var before = Hashing.StateHash(_state);
        var fired = RunRulePass();
        var after = Hashing.StateHash(_state);
        bool terminated = CheckTerminalStates();

        if (fired.Count > 0 || terminated)
        {
            AppendEvent("__engine_init__", "Case initialised", new Dictionary<string, string>(), before, after, fired, 0, EntryClassification.Neutral);
        }

        return this;
    }

    public IReadOnlyList<ActionDefinition> GetAvailableActions()
    {
        if (IsTerminated)
        {
            return Array.Empty<ActionDefinition>();
        }

        var available = new List<ActionDefinition>();
        foreach (var action in _case.AvailableActions)
        {
            if (!IsActionOfferable(action))
            {
                continue;
            }
            available.Add(action);
        }
        return available;
    }

    /// <summary>Apply a player action. State is unchanged if the result is a rejection.</summary>
    public ActionResult ApplyAction(string actionId, IReadOnlyDictionary<string, string>? actionParams = null)
    {
        actionParams ??= new Dictionary<string, string>();

        if (IsTerminated)
        {
            return ActionResult.Rejected("The simulation has already ended.");
        }

        var action = _case.AvailableActions.FirstOrDefault(a => a.Id == actionId);
        if (action is null)
        {
            return ActionResult.Rejected($"Unknown action \"{actionId}\".");
        }

        if (action.Visibility == "when" &&
            (string.IsNullOrEmpty(action.VisibleWhen) || !ExpressionEvaluator.EvaluateBool(action.VisibleWhen!, _state)))
        {
            return ActionResult.Rejected($"Action \"{actionId}\" is not available yet.");
        }

        if (!action.Repeatable && _state.ActionCount(actionId) > 0)
        {
            return ActionResult.Rejected($"Action \"{actionId}\" has already been performed.");
        }

        foreach (var precondition in action.Preconditions)
        {
            if (!ExpressionEvaluator.EvaluateBool(precondition, _state))
            {
                return ActionResult.Rejected($"Precondition not met for \"{actionId}\": {precondition}");
            }
        }

        var beforeHash = Hashing.StateHash(_state);
        var disclosedBefore = new SortedSet<string>(_state.DisclosedFacts, StringComparer.Ordinal);

        _state.SimTimeSec += action.TimeCostSec;
        _state.IncrementActionCount(actionId);
        ApplyEffects(action.Effects);

        var fired = RunRulePass();
        var scoring = _scoring.ScoreAction(actionId, _state.SimTimeSec, _state);

        var afterHash = Hashing.StateHash(_state);
        bool terminated = CheckTerminalStates();

        var evt = AppendEvent(
            actionId,
            action.Label,
            actionParams,
            beforeHash,
            afterHash,
            fired,
            scoring.Delta,
            scoring.Classification);

        var template = ResolveResultTemplate(action);
        ResultAsset? asset = template?.AssetId is { } assetId
            ? _case.ResultAssets.FirstOrDefault(a => a.Id == assetId)
            : null;
        return ActionResult.Ok(
            evt,
            terminated,
            CollectCues(fired),
            template?.Text,
            template?.AssetId,
            asset?.Label,
            asset?.Provenance?.ClinicalStatus,
            CollectNewDisclosures(disclosedBefore));
    }

    /// <summary>Resolves the action's result template from case data (presentation text only —
    /// never state). Null when the action has no template or the case has no template list.</summary>
    private ResultTemplate? ResolveResultTemplate(ActionDefinition action)
    {
        if (string.IsNullOrEmpty(action.ResultTemplateId) || _case.ResultTemplates.Count == 0)
        {
            return null;
        }
        return _case.ResultTemplates.FirstOrDefault(t => t.Id == action.ResultTemplateId);
    }

    private IReadOnlyList<DisclosedFact> CollectNewDisclosures(SortedSet<string> disclosedBefore)
    {
        var fresh = new List<DisclosedFact>();
        foreach (var factId in _state.DisclosedFacts) // SortedSet — deterministic order
        {
            if (disclosedBefore.Contains(factId))
            {
                continue;
            }
            var fact = _case.HiddenFacts.FirstOrDefault(f => f.Id == factId);
            fresh.Add(new DisclosedFact(factId, fact?.Text ?? ""));
        }
        return fresh;
    }

    /// <summary>Advance the simulated clock with no player action (pure time passage).</summary>
    public ActionResult AdvanceTime(int seconds)
    {
        if (seconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), "Time cannot move backwards.");
        }
        if (IsTerminated)
        {
            return ActionResult.Rejected("The simulation has already ended.");
        }

        var beforeHash = Hashing.StateHash(_state);
        var disclosedBefore = new SortedSet<string>(_state.DisclosedFacts, StringComparer.Ordinal);
        _state.SimTimeSec += seconds;
        var fired = RunRulePass();
        var afterHash = Hashing.StateHash(_state);
        bool terminated = CheckTerminalStates();

        var evt = AppendEvent(
            WaitActionId,
            $"Waited {seconds}s",
            new Dictionary<string, string>(),
            beforeHash,
            afterHash,
            fired,
            0,
            EntryClassification.Neutral);

        return ActionResult.Ok(
            evt, terminated, CollectCues(fired),
            newlyDisclosedFacts: CollectNewDisclosures(disclosedBefore));
    }

    public SimulationSnapshot Snapshot()
    {
        return new SimulationSnapshot
        {
            CaseId = _case.Id,
            CaseVersion = _case.Version,
            SimTimeSec = _state.SimTimeSec,
            Vitals = new VitalsSnapshot
            {
                Hr = _state.Vitals.Hr,
                SbpMmHg = _state.Vitals.SbpMmHg,
                DbpMmHg = _state.Vitals.DbpMmHg,
                Spo2 = _state.Vitals.Spo2,
                RrPerMin = _state.Vitals.RrPerMin,
                TempC = _state.Vitals.TempC,
            },
            Rhythm = _state.Rhythm,
            Airway = _state.Airway,
            Breathing = _state.Breathing,
            Circulation = _state.Circulation,
            Neuro = _state.Neuro,
            PainScore = _state.PainScore,
            Flags = _state.Flags.ToArray(),
            DisclosedFacts = _state.DisclosedFacts.ToArray(),
            IsTerminal = IsTerminated,
            TerminalStateId = TerminalStateId,
            TerminalOutcome = TerminalOutcome,
            StateHash = Hashing.StateHash(_state),
        };
    }

    public AttemptScore Score() => _scoring.BuildFinalScore();

    /// <summary>Per-criterion outcomes for the debrief (case-definition order, deterministic).</summary>
    public IReadOnlyList<CriterionResult> CriterionResults() => _scoring.BuildCriterionResults();

    /// <summary>
    /// Structured facts for the debrief. Deterministic — the LLM only narrates these,
    /// it never adds to or recomputes them.
    /// </summary>
    public DebriefFacts BuildDebriefFacts()
    {
        var score = _scoring.BuildFinalScore();
        var correct = new List<string>();
        var delayed = new List<string>();
        var harmful = new List<string>();

        foreach (var evt in _timeline.Events)
        {
            switch (evt.Classification)
            {
                case EntryClassification.Correct:
                    correct.Add(evt.ActionId);
                    break;
                case EntryClassification.Delayed:
                    delayed.Add(evt.ActionId);
                    break;
                case EntryClassification.Harmful:
                    harmful.Add(evt.ActionId);
                    break;
            }
        }

        return new DebriefFacts(
            correct,
            delayed,
            harmful,
            score.MissedCriterionIds,
            TerminalOutcome ?? "incomplete",
            score.Total);
    }

    /// <summary>
    /// Canonical hidden / visible+disabled / enabled projection for every action,
    /// in case-definition order. Empty once the simulation is terminated. UI layers
    /// render this; they never re-derive visibility or precondition logic.
    /// </summary>
    public IReadOnlyList<ActionAvailability> GetActionAvailability()
    {
        if (IsTerminated)
        {
            return Array.Empty<ActionAvailability>();
        }

        var result = new List<ActionAvailability>(_case.AvailableActions.Count);
        foreach (var action in _case.AvailableActions)
        {
            result.Add(ComputeAvailability(action));
        }
        return result;
    }

    // --- internals ----------------------------------------------------

    private ActionAvailability ComputeAvailability(ActionDefinition action)
    {
        bool visible = true;
        if (action.Visibility == "when")
        {
            visible = !string.IsNullOrEmpty(action.VisibleWhen)
                && ExpressionEvaluator.EvaluateBool(action.VisibleWhen!, _state);
        }

        if (!visible)
        {
            return new ActionAvailability(action.Id, action.Label, action.Type,
                visible: false, enabled: false, disabledReason: null);
        }

        if (!action.Repeatable && _state.ActionCount(action.Id) > 0)
        {
            return new ActionAvailability(action.Id, action.Label, action.Type,
                visible: true, enabled: false, disabledReason: "already performed");
        }

        foreach (var precondition in action.Preconditions)
        {
            if (!ExpressionEvaluator.EvaluateBool(precondition, _state))
            {
                return new ActionAvailability(action.Id, action.Label, action.Type,
                    visible: true, enabled: false,
                    disabledReason: $"requires: {precondition}");
            }
        }

        return new ActionAvailability(action.Id, action.Label, action.Type,
            visible: true, enabled: true, disabledReason: null);
    }

    private bool IsActionOfferable(ActionDefinition action)
    {
        // Single source: offerable == Visible && Enabled in the same projection
        // the UI renders, so the two views can never disagree.
        var availability = ComputeAvailability(action);
        return availability.Visible && availability.Enabled;
    }

    private IReadOnlyList<string> RunRulePass()
    {
        var firedThisPass = new List<string>();

        // Higher priority first; ties broken by rule id for determinism.
        var ordered = _case.TransitionRules
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .ToList();

        for (int iteration = 0; iteration < MaxRuleCascadeIterations; iteration++)
        {
            bool appliedSomething = false;

            foreach (var rule in ordered)
            {
                if (rule.Once && _state.FiredRuleIds.Contains(rule.Id))
                {
                    continue;
                }

                if (_pendingRules.TryGetValue(rule.Id, out int applyAtSec))
                {
                    if (_state.SimTimeSec < applyAtSec)
                    {
                        continue;
                    }
                    _pendingRules.Remove(rule.Id);
                    ApplyRule(rule, firedThisPass);
                    appliedSomething = true;
                    continue;
                }

                if (!ExpressionEvaluator.EvaluateBool(rule.When, _state))
                {
                    continue;
                }

                if (rule.DelaySec > 0)
                {
                    _pendingRules[rule.Id] = _state.SimTimeSec + rule.DelaySec;
                    continue;
                }

                ApplyRule(rule, firedThisPass);
                appliedSomething = true;
            }

            if (!appliedSomething)
            {
                break;
            }
        }

        return firedThisPass;
    }

    private void ApplyRule(TransitionRule rule, List<string> firedThisPass)
    {
        ApplyEffects(rule.Effects);
        _state.FiredRuleIds.Add(rule.Id);
        firedThisPass.Add(rule.Id);

        if (!string.IsNullOrEmpty(rule.TerminalState))
        {
            SetTerminal(rule.TerminalState!);
        }
    }

    private IReadOnlyList<string> CollectCues(IReadOnlyList<string> firedRuleIds)
    {
        var cues = new List<string>();
        foreach (var id in firedRuleIds)
        {
            var rule = _case.TransitionRules.FirstOrDefault(r => r.Id == id);
            if (rule is not null && !string.IsNullOrEmpty(rule.PresentationCue))
            {
                cues.Add(rule.PresentationCue!);
            }
        }
        return cues;
    }

    private bool CheckTerminalStates()
    {
        if (IsTerminated)
        {
            return true;
        }
        foreach (var terminal in _case.TerminalStates)
        {
            if (ExpressionEvaluator.EvaluateBool(terminal.When, _state))
            {
                SetTerminal(terminal.Id);
                return true;
            }
        }
        return false;
    }

    private void SetTerminal(string terminalStateId)
    {
        if (IsTerminated)
        {
            return;
        }
        var terminal = _case.TerminalStates.FirstOrDefault(t => t.Id == terminalStateId)
            ?? throw new InvalidOperationException($"Rule referenced unknown terminal state \"{terminalStateId}\".");
        IsTerminated = true;
        TerminalStateId = terminal.Id;
        TerminalOutcome = terminal.Outcome;
    }

    private void ApplyEffects(IEnumerable<Effect> effects)
    {
        foreach (var effect in effects)
        {
            ApplyEffect(effect);
        }
    }

    private void ApplyEffect(Effect effect)
    {
        switch (effect.Op)
        {
            case "setFlag":
                _state.Flags.Add(RequireString(effect.Flag, "setFlag.flag"));
                break;
            case "clearFlag":
                _state.Flags.Remove(RequireString(effect.Flag, "clearFlag.flag"));
                break;
            case "disclose":
                _state.DisclosedFacts.Add(RequireString(effect.FactId, "disclose.factId"));
                break;
            case "setRhythm":
                _state.Rhythm = EffectString(effect);
                break;
            case "setEnum":
                SetEnumTarget(RequireString(effect.Target, "setEnum.target"), EffectString(effect));
                break;
            case "set":
                SetNumericTarget(RequireString(effect.Target, "set.target"), EffectNumber(effect), additive: false);
                break;
            case "adjust":
                SetNumericTarget(RequireString(effect.Target, "adjust.target"), EffectNumber(effect), additive: true);
                break;
            default:
                throw new InvalidOperationException($"Unknown effect op \"{effect.Op}\".");
        }
    }

    private void SetNumericTarget(string target, double value, bool additive)
    {
        if (target.StartsWith("vitals.", StringComparison.Ordinal))
        {
            string vital = target.Substring("vitals.".Length);
            double current = additive ? _state.Vitals.Get(vital) : 0;
            _state.Vitals.Set(vital, current + value);
            return;
        }

        switch (target)
        {
            case "painScore":
                _state.PainScore = (int)Math.Round((additive ? _state.PainScore : 0) + value);
                break;
            case "simTimeSec":
                throw new InvalidOperationException("Effects may not write simTimeSec directly.");
            default:
                throw new InvalidOperationException($"Unknown numeric effect target \"{target}\".");
        }
    }

    private void SetEnumTarget(string target, string value)
    {
        switch (target)
        {
            case "airway":
                _state.Airway = value;
                break;
            case "breathing":
                _state.Breathing = value;
                break;
            case "circulation":
                _state.Circulation = value;
                break;
            case "neuro":
                _state.Neuro = value;
                break;
            case "rhythm":
                _state.Rhythm = value;
                break;
            default:
                throw new InvalidOperationException($"Unknown enum effect target \"{target}\".");
        }
    }

    private AttemptEvent AppendEvent(
        string actionId,
        string label,
        IReadOnlyDictionary<string, string> parameters,
        string beforeHash,
        string afterHash,
        IReadOnlyList<string> triggeredRuleIds,
        double scoreDelta,
        EntryClassification classification)
    {
        var evt = new AttemptEvent
        {
            Seq = _seq++,
            SimTimeSec = _state.SimTimeSec,
            ActionId = actionId,
            Params = new Dictionary<string, string>(
                (IDictionary<string, string>)parameters, StringComparer.Ordinal),
            BeforeHash = beforeHash,
            AfterHash = afterHash,
            TriggeredRuleIds = triggeredRuleIds.ToArray(),
            ScoreDelta = scoreDelta,
            Classification = classification,
            Label = label,
        };
        _timeline.Append(evt);
        return evt;
    }

    private static string RequireString(string? value, string field) =>
        !string.IsNullOrEmpty(value)
            ? value!
            : throw new InvalidOperationException($"Effect field \"{field}\" is required.");

    private static string EffectString(Effect effect)
    {
        if (effect.Value is null || effect.Value.Value.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            throw new InvalidOperationException($"Effect \"{effect.Op}\" requires a string value.");
        }
        return effect.Value.Value.GetString()!;
    }

    private static double EffectNumber(Effect effect)
    {
        if (effect.Value is null || effect.Value.Value.ValueKind != System.Text.Json.JsonValueKind.Number)
        {
            throw new InvalidOperationException($"Effect \"{effect.Op}\" requires a numeric value.");
        }
        return effect.Value.Value.GetDouble();
    }
}

public sealed class DebriefFacts
{
    public DebriefFacts(
        IReadOnlyList<string> correctActionIds,
        IReadOnlyList<string> delayedActionIds,
        IReadOnlyList<string> harmfulActionIds,
        IReadOnlyList<string> missedCriterionIds,
        string outcome,
        double totalScore)
    {
        CorrectActionIds = correctActionIds;
        DelayedActionIds = delayedActionIds;
        HarmfulActionIds = harmfulActionIds;
        MissedCriterionIds = missedCriterionIds;
        Outcome = outcome;
        TotalScore = totalScore;
    }

    public IReadOnlyList<string> CorrectActionIds { get; }
    public IReadOnlyList<string> DelayedActionIds { get; }
    public IReadOnlyList<string> HarmfulActionIds { get; }
    public IReadOnlyList<string> MissedCriterionIds { get; }
    public string Outcome { get; }
    public double TotalScore { get; }
}
