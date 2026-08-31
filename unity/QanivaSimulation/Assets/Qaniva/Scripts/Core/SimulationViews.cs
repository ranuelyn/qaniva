using System;
using System.Collections.Generic;

namespace Qaniva.Simulation.Core
{
    /// <summary>Config handed to the runtime when a simulation starts (from START_SIMULATION).</summary>
    [Serializable]
    public struct SimulationConfig
    {
        public string CaseId;
        public int CaseVersion;
        public string AttemptId;
        public string Locale;
        public string Difficulty;
        public long Seed;
    }

    /// <summary>Immutable, presentation-facing view of engine state. No engine internals.</summary>
    [Serializable]
    public sealed class SimulationSnapshotView
    {
        public int SimTimeSec;
        public double Hr;
        public double SbpMmHg;
        public double DbpMmHg;
        public double Spo2;
        public double RrPerMin;
        public double TempC;
        public string Rhythm = "";
        public string Airway = "";
        public string Breathing = "";
        public string Circulation = "";
        public string Neuro = "";
        public int PainScore;
        public List<string> Flags = new List<string>();
        public List<string> DisclosedFacts = new List<string>();
        public bool IsTerminal;
        public string TerminalOutcome;
        public string StateHash = "";
    }

    /// <summary>Result of applying one action, for the presentation layer to react to.</summary>
    [Serializable]
    public sealed class ActionOutcomeView
    {
        public bool Accepted;
        public string RejectionReason;
        public bool Terminated;
        public string Classification = "Neutral";
        public double ScoreDelta;
        public List<string> TriggeredRuleIds = new List<string>();
        public List<string> PresentationCues = new List<string>();
        public SimulationSnapshotView Snapshot;

        /// <summary>Case-authored result narrative for this action (null when none).</summary>
        public string ResultText;

        /// <summary>Result-asset id the learner may open (e.g. an ECG tracing), or null.</summary>
        public string ResultAssetId;

        /// <summary>Learner-facing label of that asset ("12-lead ECG"), or null.</summary>
        public string ResultAssetLabel;

        /// <summary>Asset provenance.clinicalStatus (e.g. placeholder_replacement_required), or null.</summary>
        public string ResultAssetClinicalStatus;

        /// <summary>Facts disclosed by this step (engine-diffed), with learner-facing text.</summary>
        public List<DisclosedFactView> NewlyDisclosedFacts = new List<DisclosedFactView>();
    }

    [Serializable]
    public sealed class DisclosedFactView
    {
        public string Id = "";
        public string Text = "";
    }

    /// <summary>One rubric criterion's final deterministic outcome (engine-owned debrief fact).</summary>
    [Serializable]
    public sealed class CriterionResultView
    {
        public string Id = "";
        public string Label = "";
        public string Category = "";
        public string Criticality = "minor";
        public bool Harmful;
        /// <summary>correct | delayed | missed | harmful | avoided</summary>
        public string Classification = "missed";
        public int CreditedAtSec = -1;
        public double AwardedPoints;
        public double MaxPoints;

        /// <summary>Evidence-ledger ids from the rubric (learner-visible traceability).</summary>
        public List<string> EvidenceRefs = new List<string>();

        /// <summary>Labels of every accepted action (alternatives surface in the debrief).</summary>
        public List<string> AcceptedActionLabels = new List<string>();
    }

    /// <summary>A case-authored literature reference (concise; shown in the debrief).</summary>
    [Serializable]
    public sealed class CaseReferenceView
    {
        public string Label = "";
        public string Citation = "";
    }

    [Serializable]
    public sealed class TimelineEntryView
    {
        public int Seq;
        public int SimTimeSec;
        public string ActionId = "";
        public string Label = "";
        public string Classification = "Neutral";

        /// <summary>Authored causality texts of rules that fired on this step
        /// (TransitionRule.debriefText, resolved from case data). Empty when none.</summary>
        public List<string> StateChanges = new List<string>();
    }

    /// <summary>
    /// The case's presentation metadata (authored in case.json `presentationProfile`,
    /// parsed by the engine, passed through untouched). This is the ONLY channel by
    /// which a case selects its 3D presentation — environment, patient visual,
    /// camera — so a new case never needs a new Unity scene.
    /// </summary>
    [Serializable]
    public sealed class PresentationProfileView
    {
        public string RoomKey = "";
        public string PatientVariant = "";
        public string AnimationStateAtStart = "";
        public string MonitorLayout = "";
        public string CameraPreset = "";
    }

    /// <summary>
    /// Canonical hidden / visible+disabled / enabled projection of one action,
    /// computed by the engine (never by UI). Visible==false means do not render;
    /// Visible &amp;&amp; !Enabled means render greyed with DisabledReason.
    /// </summary>
    [Serializable]
    public sealed class ActionAvailabilityView
    {
        public string ActionId = "";
        public string Label = "";
        /// <summary>Case action type: examine|order|medication|procedure|consult|disposition|communication.</summary>
        public string Type = "";
        public bool Visible;
        public bool Enabled;
        public string DisabledReason;
    }

    [Serializable]
    public sealed class AttemptSummaryView
    {
        public string AttemptId = "";
        public string CaseId = "";
        public int CaseVersion;
        public long Seed;
        public string TerminalOutcome = "aborted";
        public double TotalScore;
        public double ScoreCritical;
        public double ScoreTiming;
        public double ScoreEfficiency;
        public double ScoreTreatment;
        public double ScoreDisposition;
        public List<TimelineEntryView> Timeline = new List<TimelineEntryView>();
        public List<CriterionResultView> Criteria = new List<CriterionResultView>();
        public string DebriefSummary = "";
        public List<string> DebriefKeyTeachingPoints = new List<string>();
        public List<string> DebriefCommonErrors = new List<string>();
        public List<CaseReferenceView> References = new List<CaseReferenceView>();
        public string ReplayHash = "";
    }
}
