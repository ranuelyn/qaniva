using System;
using System.Collections.Generic;
using UnityEngine;
using Qaniva.Simulation.Core;

namespace Qaniva.Bridge
{
    /// <summary>
    /// Owns the RN &lt;-&gt; Unity conversation for one simulation. It never computes
    /// clinical results — it forwards player input to <see cref="IClinicalRuntime"/>
    /// and forwards the engine's snapshots/summary back over the bridge.
    ///
    /// Flow: START_SIMULATION -&gt; (load + init) -&gt; SIMULATION_READY
    ///       ... player actions ... -&gt; engine terminal -&gt; SIMULATION_COMPLETED
    ///       EXIT_SIMULATION -&gt; EXIT_REQUESTED (+ ExitRequested event for the app)
    /// </summary>
    public sealed class SimulationBridgeController : MonoBehaviour
    {
        private IUnityBridge _bridge;
        private IClinicalRuntime _runtime;
        private ICaseProvider _caseProvider;

        private string _caseId;
        private int _caseVersion;
        private string _attemptId;
        private long _seed;
        private string _startedAtIso;
        private bool _paused;
        private bool _completed;
        private float _startRealtime;

        /// <summary>Raised after EXIT_REQUESTED is sent so the host app can unload Unity.</summary>
        public event Action ExitRequested;

        /// <summary>Latest snapshot from the engine, for presentation adapters to bind to.</summary>
        public SimulationSnapshotView CurrentSnapshot { get; private set; }
        public event Action<SimulationSnapshotView> SnapshotUpdated;

        /// <summary>
        /// Runtime mode from START_SIMULATION (BridgeProtocol.Modes.*). Production
        /// launches are always Interactive; the e2e modes only arm test drivers.
        /// </summary>
        public string CurrentMode { get; private set; } = BridgeProtocol.Modes.Interactive;

        /// <summary>Case id of the in-flight simulation (null before the first START).</summary>
        public string CurrentCaseId => _caseId;

        /// <summary>Raised when a NEW simulation has been loaded and initialised.</summary>
        public event Action SimulationStarted;

        /// <summary>Explicit wiring for tests / custom hosts.</summary>
        public void Configure(IUnityBridge bridge, IClinicalRuntime runtime, ICaseProvider caseProvider)
        {
            Unsubscribe();
            _bridge = bridge;
            _runtime = runtime;
            _caseProvider = caseProvider;
            _bridge.MessageReceived += OnHostMessage;
        }

        // NOTE: no Awake auto-configuration. BridgeBootstrap (or a test) must call
        // Configure() explicitly, so there is exactly one place that decides which
        // IClinicalRuntime implementation is live.

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (_bridge != null)
            {
                _bridge.MessageReceived -= OnHostMessage;
            }
        }

        // --- inbound ------------------------------------------------------

        private void OnHostMessage(string json)
        {
            string type;
            Newtonsoft.Json.Linq.JObject payload;
            try
            {
                (type, payload) = BridgeMessageCodec.DecodeEnvelope(json, BridgeMessageCodec.RnToUnityTypes);
            }
            catch (BridgeProtocolException ex)
            {
                SendFailed(null, BridgeProtocol.FailureCodes.BridgeProtocolError, ex.Message);
                return;
            }

            switch (type)
            {
                case BridgeProtocol.RnToUnity.StartSimulation:
                    HandleStart(BridgeMessageCodec.DecodePayload<StartSimulationPayload>(payload));
                    break;
                case BridgeProtocol.RnToUnity.PauseSimulation:
                    _paused = true;
                    break;
                case BridgeProtocol.RnToUnity.ResumeSimulation:
                    _paused = false;
                    break;
                case BridgeProtocol.RnToUnity.ExitSimulation:
                    HandleExit();
                    break;
            }
        }

        private void HandleStart(StartSimulationPayload p)
        {
            // The host re-sends START until Unity answers (its runtime boots after
            // runEmbedded returns, so early sends are dropped). A duplicate START
            // for the attempt already in flight must NOT reload the simulation —
            // just re-announce readiness.
            if (p.attemptId == _attemptId && _runtime != null && !_runtime.IsTerminated
                && CurrentSnapshot != null)
            {
                _bridge.SendToHost(BridgeMessageCodec.Encode(
                    BridgeProtocol.UnityToRn.SimulationReady,
                    new SimulationReadyPayload
                    {
                        caseId = _caseId,
                        attemptId = _attemptId,
                        warmupSec = 0,
                    }));
                return;
            }

            _caseId = p.caseId;
            _caseVersion = p.caseVersion;
            _attemptId = p.attemptId;
            _seed = p.seed;
            _startedAtIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            _startRealtime = Time.realtimeSinceStartup;
            _paused = false;
            _completed = false;
            CurrentMode = string.IsNullOrEmpty(p.mode) ? BridgeProtocol.Modes.Interactive : p.mode;

            try
            {
                var caseJson = _caseProvider.GetCaseJson(p.caseId, p.caseVersion);
                _runtime.LoadCase(caseJson, unchecked((ulong)p.seed));
                CurrentSnapshot = _runtime.Initialize();
                SnapshotUpdated?.Invoke(CurrentSnapshot);
                SimulationStarted?.Invoke();
            }
            catch (Exception ex)
            {
                SendFailed(_attemptId, BridgeProtocol.FailureCodes.CaseLoadFailed, ex.Message);
                return;
            }

            _bridge.SendToHost(BridgeMessageCodec.Encode(
                BridgeProtocol.UnityToRn.SimulationReady,
                new SimulationReadyPayload
                {
                    caseId = _caseId,
                    attemptId = _attemptId,
                    warmupSec = Math.Max(0f, Time.realtimeSinceStartup - _startRealtime),
                }));

            if (_runtime.IsTerminated)
            {
                CompleteSimulation();
            }
        }

        private void HandleExit()
        {
            _bridge.SendToHost(BridgeMessageCodec.Encode(
                BridgeProtocol.UnityToRn.ExitRequested,
                new ExitRequestedPayload { attemptId = _attemptId, reason = "user_quit" }));
            ExitRequested?.Invoke();
        }

        // --- driven by the in-simulation UI -----------------------------

        /// <summary>
        /// User abort from the in-simulation UI. Emits EXIT_REQUESTED (never
        /// SIMULATION_COMPLETED — an aborted attempt has no engine terminal state).
        /// </summary>
        public void RequestExit() => HandleExit();

        /// <summary>Engine's canonical hidden/disabled/enabled action projection.</summary>
        public IReadOnlyList<ActionAvailabilityView> GetActionAvailability() =>
            _runtime?.GetActionAvailability() ?? new List<ActionAvailabilityView>();

        /// <summary>Engine's canonical attempt timeline so far.</summary>
        public IReadOnlyList<TimelineEntryView> GetTimeline() =>
            _runtime?.GetTimeline() ?? new List<TimelineEntryView>();

        /// <summary>The loaded case's presentation metadata (null before load).</summary>
        public PresentationProfileView GetPresentationProfile() =>
            _runtime?.GetPresentationProfile();

        public ActionOutcomeView SubmitPlayerAction(string actionId, IReadOnlyDictionary<string, string> parameters = null)
        {
            if (_paused || _runtime == null || _runtime.IsTerminated)
            {
                return new ActionOutcomeView { Accepted = false, RejectionReason = "not accepting input" };
            }

            var outcome = _runtime.ApplyAction(
                actionId, parameters ?? new Dictionary<string, string>());

            if (outcome.Snapshot != null)
            {
                CurrentSnapshot = outcome.Snapshot;
                SnapshotUpdated?.Invoke(CurrentSnapshot);
            }

            if (outcome.Terminated || _runtime.IsTerminated)
            {
                CompleteSimulation();
            }
            return outcome;
        }

        // --- outbound --------------------------------------------------

        private void CompleteSimulation()
        {
            // Exactly-once: whatever path notices the terminal state first wins;
            // later calls (late submits, host retries) must not re-emit.
            if (_completed)
            {
                return;
            }
            _completed = true;

            var summary = _runtime.BuildAttemptSummary();
            var dto = new AttemptSummaryDto
            {
                attemptId = _attemptId,
                caseId = string.IsNullOrEmpty(summary.CaseId) ? _caseId : summary.CaseId,
                caseVersion = summary.CaseVersion == 0 ? _caseVersion : summary.CaseVersion,
                seed = _seed,
                startedAt = _startedAtIso,
                completedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                terminalState = summary.TerminalOutcome,
                totalScore = summary.TotalScore,
                scoreBreakdown = new ScoreBreakdownDto
                {
                    critical = summary.ScoreCritical,
                    timing = summary.ScoreTiming,
                    efficiency = summary.ScoreEfficiency,
                    treatment = summary.ScoreTreatment,
                    disposition = summary.ScoreDisposition,
                },
                replayHash = summary.ReplayHash,
                debrief = new DebriefContentDto
                {
                    summary = summary.DebriefSummary,
                    keyTeachingPoints = new List<string>(summary.DebriefKeyTeachingPoints),
                    commonErrors = new List<string>(summary.DebriefCommonErrors),
                },
            };
            foreach (var r in summary.References)
            {
                dto.references.Add(new CaseReferenceDto { label = r.Label, citation = r.Citation });
            }
            foreach (var c in summary.Criteria)
            {
                dto.criteria.Add(new CriterionResultDto
                {
                    id = c.Id,
                    label = c.Label,
                    category = c.Category,
                    criticality = c.Criticality,
                    harmful = c.Harmful,
                    classification = c.Classification,
                    creditedAtSec = c.CreditedAtSec,
                    awardedPoints = c.AwardedPoints,
                    maxPoints = c.MaxPoints,
                    evidenceRefs = new List<string>(c.EvidenceRefs),
                    acceptedActionLabels = new List<string>(c.AcceptedActionLabels),
                });
            }
            foreach (var e in summary.Timeline)
            {
                dto.timeline.Add(new TimelineEntryDto
                {
                    seq = e.Seq,
                    simTimeSec = e.SimTimeSec,
                    actionId = e.ActionId,
                    label = e.Label,
                    classification = e.Classification.ToLowerInvariant(),
                    stateChanges = new List<string>(e.StateChanges),
                });
            }

            _bridge.SendToHost(BridgeMessageCodec.Encode(
                BridgeProtocol.UnityToRn.SimulationCompleted,
                new SimulationCompletedPayload { attemptId = _attemptId, summary = dto }));
        }

        private void SendFailed(string attemptId, string code, string message)
        {
            _bridge.SendToHost(BridgeMessageCodec.Encode(
                BridgeProtocol.UnityToRn.SimulationFailed,
                new SimulationFailedPayload { attemptId = attemptId, code = code, message = message }));
        }
    }
}
