using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Qaniva.Simulation.Core;

namespace Qaniva.Bridge
{
    /// <summary>
    /// A DETERMINISTIC STAND-IN for the real engine, used until
    /// <c>Qaniva.Clinical.Core.dll</c> is synced into Assets/Qaniva/Plugins and the
    /// <c>QANIVA_HAS_CLINICAL_CORE</c> scripting define is set (see
    /// unity/QanivaSimulation/README.md).
    ///
    /// It does NOT model medicine. It only advances a clock and reports a fixed
    /// terminal result so the bridge round-trip (START -&gt; READY -&gt; COMPLETED)
    /// can be exercised in the Editor and in CI-style EditMode tests.
    /// </summary>
    public sealed class StubClinicalRuntime : IClinicalRuntime
    {
        private string _caseId = "";
        private int _caseVersion;
        private long _seed;
        private int _simTimeSec;
        private int _acceptedActions;
        private readonly List<TimelineEntryView> _timeline = new List<TimelineEntryView>();

        public bool IsTerminated { get; private set; }

        public void LoadCase(string caseJson, ulong seed)
        {
            var root = JObject.Parse(caseJson);
            _caseId = root.Value<string>("id") ?? "unknown";
            _caseVersion = root.Value<int?>("version") ?? 0;
            _seed = (long)seed;
            _simTimeSec = 0;
            _acceptedActions = 0;
            IsTerminated = false;
            _timeline.Clear();
        }

        public SimulationSnapshotView Initialize() => Snapshot();

        public IReadOnlyList<string> GetAvailableActionIds() =>
            IsTerminated ? new List<string>() : new List<string> { "attach_monitor", "assess_patient", "treat", "disposition" };

        public ActionOutcomeView ApplyAction(string actionId, IReadOnlyDictionary<string, string> parameters)
        {
            if (IsTerminated)
            {
                return new ActionOutcomeView { Accepted = false, RejectionReason = "ended", Snapshot = Snapshot() };
            }

            _simTimeSec += 30;
            _acceptedActions += 1;
            _timeline.Add(new TimelineEntryView
            {
                Seq = _timeline.Count,
                SimTimeSec = _simTimeSec,
                ActionId = actionId,
                Label = actionId,
                Classification = "Neutral",
            });

            if (_acceptedActions >= 4 || actionId == "disposition")
            {
                IsTerminated = true;
            }

            return new ActionOutcomeView
            {
                Accepted = true,
                Terminated = IsTerminated,
                Classification = "Neutral",
                Snapshot = Snapshot(),
            };
        }

        public SimulationSnapshotView AdvanceTime(int seconds)
        {
            if (!IsTerminated)
            {
                _simTimeSec += seconds;
            }
            return Snapshot();
        }

        public AttemptSummaryView BuildAttemptSummary() => new AttemptSummaryView
        {
            CaseId = _caseId,
            CaseVersion = _caseVersion,
            Seed = _seed,
            TerminalOutcome = IsTerminated ? "complete" : "aborted",
            TotalScore = _acceptedActions * 10,
            ScoreTreatment = _acceptedActions * 10,
            Timeline = new List<TimelineEntryView>(_timeline),
            ReplayHash = $"stub-{_caseId}-{_seed}-{_acceptedActions}",
        };

        private SimulationSnapshotView Snapshot() => new SimulationSnapshotView
        {
            SimTimeSec = _simTimeSec,
            Hr = 60,
            SbpMmHg = 110,
            DbpMmHg = 70,
            Spo2 = 97,
            RrPerMin = 16,
            TempC = 36.8,
            Rhythm = "stub_rhythm",
            Airway = "patent",
            Breathing = "spontaneous",
            Circulation = "normal",
            Neuro = "alert",
            PainScore = 0,
            IsTerminal = IsTerminated,
            TerminalOutcome = IsTerminated ? "complete" : null,
            StateHash = $"stub-{_simTimeSec}-{_acceptedActions}",
        };
    }
}
