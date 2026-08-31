// Compiles only when the deterministic engine DLL is present and the scripting
// define QANIVA_HAS_CLINICAL_CORE is set. See unity/QanivaSimulation/README.md
// ("Sync the clinical engine") and scripts/sync-clinical-core-to-unity.sh.
#if QANIVA_HAS_CLINICAL_CORE
using System.Collections.Generic;
using System.Linq;
using Qaniva.Clinical.Core.Engine;
using Qaniva.Clinical.Core.Model;
using Qaniva.Clinical.Core.Replay;
using Qaniva.Clinical.Core.Serialization;
using Qaniva.Simulation.Core;
// Inside namespace Qaniva.Clinical.Runtime the bare name "Simulation" resolves to
// the sibling namespace Qaniva.Simulation, not the engine class — alias it.
using EngineSimulation = Qaniva.Clinical.Core.Engine.Simulation;

namespace Qaniva.Clinical.Runtime
{
    /// <summary>
    /// The real <see cref="IClinicalRuntime"/> — a thin adapter over
    /// <c>Qaniva.Clinical.Core</c>. It performs NO clinical logic of its own; it
    /// only translates engine types into the presentation-facing *View types.
    /// </summary>
    public sealed class ClinicalRuntime : IClinicalRuntime
    {
        private CaseDefinition _case;
        private EngineSimulation _sim;
        private readonly List<string> _appliedActionIds = new List<string>();
        private ulong _seed;

        public bool IsTerminated => _sim != null && _sim.IsTerminated;

        public void LoadCase(string caseJson, ulong seed)
        {
            _case = CaseLoader.FromJson(caseJson);
            _seed = seed;
            _sim = new EngineSimulation(_case, seed);
            _appliedActionIds.Clear();
        }

        public SimulationSnapshotView Initialize()
        {
            _sim.Initialize();
            return Map(_sim.Snapshot());
        }

        public IReadOnlyList<string> GetAvailableActionIds() =>
            _sim.GetAvailableActions().Select(a => a.Id).ToList();

        public IReadOnlyList<ActionAvailabilityView> GetActionAvailability() =>
            _sim.GetActionAvailability().Select(a => new ActionAvailabilityView
            {
                ActionId = a.ActionId,
                Label = a.Label,
                Type = a.Type,
                Visible = a.Visible,
                Enabled = a.Enabled,
                DisabledReason = a.DisabledReason,
            }).ToList();

        public PresentationProfileView GetPresentationProfile() => _case == null
            ? null
            : new PresentationProfileView
            {
                RoomKey = _case.PresentationProfile.RoomKey,
                PatientVariant = _case.PresentationProfile.PatientVariant,
                AnimationStateAtStart = _case.PresentationProfile.AnimationStateAtStart,
                MonitorLayout = _case.PresentationProfile.MonitorLayout,
                CameraPreset = _case.PresentationProfile.CameraPreset,
            };

        public IReadOnlyList<TimelineEntryView> GetTimeline() =>
            _sim.Timeline.Events.Select(e => new TimelineEntryView
            {
                Seq = e.Seq,
                SimTimeSec = e.SimTimeSec,
                ActionId = e.ActionId,
                Label = e.Label,
                Classification = e.Classification.ToString(),
            }).ToList();

        public ActionOutcomeView ApplyAction(string actionId, IReadOnlyDictionary<string, string> parameters)
        {
            var result = _sim.ApplyAction(actionId, (IReadOnlyDictionary<string, string>)parameters);
            if (result.Accepted)
            {
                _appliedActionIds.Add(actionId);
            }
            return new ActionOutcomeView
            {
                Accepted = result.Accepted,
                RejectionReason = result.RejectionReason,
                Terminated = result.Terminated,
                Classification = result.Event?.Classification.ToString() ?? "Neutral",
                ScoreDelta = result.Event?.ScoreDelta ?? 0,
                TriggeredRuleIds = result.Event?.TriggeredRuleIds.ToList() ?? new List<string>(),
                PresentationCues = result.PresentationCues.ToList(),
                Snapshot = Map(_sim.Snapshot()),
                ResultText = result.ResultText,
                ResultAssetId = result.ResultAssetId,
                ResultAssetLabel = result.ResultAssetLabel,
                NewlyDisclosedFacts = result.NewlyDisclosedFacts
                    .Select(f => new DisclosedFactView { Id = f.Id, Text = f.Text })
                    .ToList(),
            };
        }

        public SimulationSnapshotView AdvanceTime(int seconds)
        {
            _sim.AdvanceTime(seconds);
            return Map(_sim.Snapshot());
        }

        public AttemptSummaryView BuildAttemptSummary()
        {
            var score = _sim.Score();
            var snap = _sim.Snapshot();
            var summary = new AttemptSummaryView
            {
                CaseId = _case.Id,
                CaseVersion = _case.Version,
                Seed = unchecked((long)_seed),
                TerminalOutcome = snap.TerminalOutcome ?? "aborted",
                TotalScore = score.Total,
                ScoreCritical = score.Breakdown.Critical,
                ScoreTiming = score.Breakdown.Timing,
                ScoreEfficiency = score.Breakdown.Efficiency,
                ScoreTreatment = score.Breakdown.Treatment,
                ScoreDisposition = score.Breakdown.Disposition,
                ReplayHash = Replayer.ComputeReplayHash(_case, _seed, _appliedActionIds, snap.StateHash),
                DebriefSummary = _case.DebriefMetadata.Summary,
                DebriefKeyTeachingPoints = _case.DebriefMetadata.KeyTeachingPoints.ToList(),
                DebriefCommonErrors = _case.DebriefMetadata.CommonErrors.ToList(),
            };
            foreach (var c in _sim.CriterionResults())
            {
                summary.Criteria.Add(new CriterionResultView
                {
                    Id = c.Id,
                    Label = c.Label,
                    Category = c.Category,
                    Criticality = c.Criticality,
                    Harmful = c.Harmful,
                    Classification = c.Classification,
                    CreditedAtSec = c.CreditedAtSec,
                    AwardedPoints = c.AwardedPoints,
                    MaxPoints = c.MaxPoints,
                });
            }
            foreach (var e in _sim.Timeline.Events)
            {
                summary.Timeline.Add(new TimelineEntryView
                {
                    Seq = e.Seq,
                    SimTimeSec = e.SimTimeSec,
                    ActionId = e.ActionId,
                    Label = e.Label,
                    Classification = e.Classification.ToString(),
                });
            }
            return summary;
        }

        private static SimulationSnapshotView Map(SimulationSnapshot s) => new SimulationSnapshotView
        {
            SimTimeSec = s.SimTimeSec,
            Hr = s.Vitals.Hr,
            SbpMmHg = s.Vitals.SbpMmHg,
            DbpMmHg = s.Vitals.DbpMmHg,
            Spo2 = s.Vitals.Spo2,
            RrPerMin = s.Vitals.RrPerMin,
            TempC = s.Vitals.TempC,
            Rhythm = s.Rhythm,
            Airway = s.Airway,
            Breathing = s.Breathing,
            Circulation = s.Circulation,
            Neuro = s.Neuro,
            PainScore = s.PainScore,
            Flags = s.Flags.ToList(),
            DisclosedFacts = s.DisclosedFacts.ToList(),
            IsTerminal = s.IsTerminal,
            TerminalOutcome = s.TerminalOutcome,
            StateHash = s.StateHash,
        };
    }
}
#endif
