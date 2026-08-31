using System.Collections.Generic;

namespace Qaniva.Clinical.Core.Model;

/// <summary>
/// Immutable, data-driven definition of a clinical simulation case.
/// Deserialized from a schema-validated <c>case.json</c> (see packages/case-schema).
/// The engine never mutates a <see cref="CaseDefinition"/>.
/// </summary>
public sealed class CaseDefinition
{
    public int SchemaVersion { get; set; }
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public CaseMetadata Metadata { get; set; } = new();
    public List<string> LearningObjectives { get; set; } = new();
    public PresentationProfile PresentationProfile { get; set; } = new();
    public PatientProfile Patient { get; set; } = new();
    public InitialStateDto InitialState { get; set; } = new();
    public List<HiddenFact> HiddenFacts { get; set; } = new();
    public List<ActionDefinition> AvailableActions { get; set; } = new();
    public List<TransitionRule> TransitionRules { get; set; } = new();
    public List<ScoringCriterion> ScoringCriteria { get; set; } = new();
    public List<TerminalState> TerminalStates { get; set; } = new();
    public DebriefMetadata DebriefMetadata { get; set; } = new();
    public List<ResultTemplate> ResultTemplates { get; set; } = new();
    public List<ResultAsset> ResultAssets { get; set; } = new();
    public List<CaseReference> References { get; set; } = new();
}

/// <summary>Learner-facing result text for an action (resolved via ResultTemplateId).</summary>
public sealed class ResultTemplate
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    /// <summary>Optional ResultAsset id the learner can open (e.g. an ECG tracing).</summary>
    public string? AssetId { get; set; }
}

/// <summary>
/// A visual artifact an investigation result can reference (ECG now; X-ray/CT/
/// ultrasound later). Generic by design — the engine only passes the id through;
/// presentation layers resolve it to bundled media.
/// </summary>
public sealed class ResultAsset
{
    public string Id { get; set; } = "";
    /// <summary>image (only kind for now)</summary>
    public string Kind { get; set; } = "image";
    public string Label { get; set; } = "";
}

public sealed class CaseMetadata
{
    public string Title { get; set; } = "";
    public string ChiefComplaint { get; set; } = "";
    public string Specialty { get; set; } = "";
    public int EstimatedMinutes { get; set; }
    public List<string> Authors { get; set; } = new();
    public ClinicalReview ClinicalReview { get; set; } = new();
    public bool Fictional { get; set; }
}

public sealed class ClinicalReview
{
    public string Status { get; set; } = "not_reviewed";
    public string? Reviewer { get; set; }
    public string? Date { get; set; }
}

public sealed class PresentationProfile
{
    public string RoomKey { get; set; } = "";
    public string PatientVariant { get; set; } = "";
    public string AnimationStateAtStart { get; set; } = "";
    public string MonitorLayout { get; set; } = "";
    public List<string> RequiredProps { get; set; } = new();
    public string CameraPreset { get; set; } = "";
    public string AudioProfile { get; set; } = "";
}

public sealed class PatientProfile
{
    public string DisplayName { get; set; } = "";
    public int AgeYears { get; set; }
    public string Sex { get; set; } = "other";
    public double WeightKg { get; set; }
    public string Persona { get; set; } = "";
    public List<string> BackgroundFacts { get; set; } = new();
}

public sealed class InitialStateDto
{
    public VitalsDto Vitals { get; set; } = new();
    public string Rhythm { get; set; } = "";
    public string Airway { get; set; } = "patent";
    public string Breathing { get; set; } = "spontaneous";
    public string Circulation { get; set; } = "normal";
    public string Neuro { get; set; } = "alert";
    public int PainScore { get; set; }
    public List<string> Flags { get; set; } = new();
}

public sealed class VitalsDto
{
    public double Hr { get; set; }
    public double SbpMmHg { get; set; }
    public double DbpMmHg { get; set; }
    public double Spo2 { get; set; }
    public double RrPerMin { get; set; }
    public double TempC { get; set; }
}

public sealed class HiddenFact
{
    public string Id { get; set; } = "";
    /// <summary>on_ask | on_exam | on_order_result</summary>
    public string Disclosure { get; set; } = "on_ask";
    public string Text { get; set; } = "";
}

public sealed class ActionDefinition
{
    public string Id { get; set; } = "";
    /// <summary>examine | order | medication | procedure | consult | disposition | communication</summary>
    public string Type { get; set; } = "examine";
    public string Label { get; set; } = "";
    public int TimeCostSec { get; set; }
    /// <summary>always | when</summary>
    public string Visibility { get; set; } = "always";
    public string? VisibleWhen { get; set; }
    public List<string> Preconditions { get; set; } = new();
    public List<ActionParam> Params { get; set; } = new();
    public List<Effect> Effects { get; set; } = new();
    public string? ResultTemplateId { get; set; }
    public List<string> CriterionIds { get; set; } = new();
    public bool Repeatable { get; set; }
}

public sealed class ActionParam
{
    public string Name { get; set; } = "";
    /// <summary>enum | number</summary>
    public string Kind { get; set; } = "number";
    public List<string>? Options { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public string? Unit { get; set; }
}

public sealed class Effect
{
    /// <summary>setFlag | clearFlag | set | adjust | disclose | setRhythm | setEnum</summary>
    public string Op { get; set; } = "";
    public string? Flag { get; set; }
    public string? FactId { get; set; }
    /// <summary>Dotted path into PatientState, e.g. "vitals.sbpMmHg", "painScore", "circulation".</summary>
    public string? Target { get; set; }
    public System.Text.Json.JsonElement? Value { get; set; }
}

public sealed class TransitionRule
{
    public string Id { get; set; } = "";
    public string When { get; set; } = "";
    public int Priority { get; set; }
    public bool Once { get; set; } = true;
    public int DelaySec { get; set; }
    public List<Effect> Effects { get; set; } = new();
    public string? PresentationCue { get; set; }
    public string? TerminalState { get; set; }
}

public sealed class ScoringCriterion
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    /// <summary>critical | major | minor</summary>
    public string Criticality { get; set; } = "minor";
    public List<string> AcceptedActions { get; set; } = new();
    public TimingWindow? TimingWindow { get; set; }
    public List<string> StateConstraints { get; set; } = new();
    public double Points { get; set; }
    /// <summary>critical | timing | efficiency | treatment | disposition</summary>
    public string Category { get; set; } = "treatment";
    public bool Harmful { get; set; }
    public string Rationale { get; set; } = "";
    public List<string> EvidenceRefs { get; set; } = new();
}

public sealed class TimingWindow
{
    public int FullCreditBeforeSec { get; set; }
    public int ZeroCreditAfterSec { get; set; }
}

public sealed class TerminalState
{
    public string Id { get; set; } = "";
    public string When { get; set; } = "";
    /// <summary>complete | discharge | admit | death | aborted</summary>
    public string Outcome { get; set; } = "complete";
    public string Label { get; set; } = "";
}

public sealed class DebriefMetadata
{
    public string Summary { get; set; } = "";
    public List<string> KeyTeachingPoints { get; set; } = new();
    public List<string> CommonErrors { get; set; } = new();
}

public sealed class CaseReference
{
    public string Label { get; set; } = "";
    public string Citation { get; set; } = "";
    public string? Url { get; set; }
}
