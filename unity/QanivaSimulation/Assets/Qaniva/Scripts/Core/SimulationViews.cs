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
    }

    [Serializable]
    public sealed class TimelineEntryView
    {
        public int Seq;
        public int SimTimeSec;
        public string ActionId = "";
        public string Label = "";
        public string Classification = "Neutral";
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
        public string ReplayHash = "";
    }
}
