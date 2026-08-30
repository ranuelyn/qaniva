using UnityEngine;
using Qaniva.Bridge;
using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Binds the vitals monitor display to engine snapshots. Contains ZERO clinical
    /// logic: it formats numbers the engine already produced. Attach to the monitor
    /// prefab and assign the TMP_Text fields in the Editor (manual step, see README).
    /// </summary>
    public sealed class VitalsMonitorBinding : MonoBehaviour, IPresentationAdapter
    {
        [SerializeField] private SimulationBridgeController controller;

        // Formatted strings ready to drop onto TMP_Text fields once the monitor
        // prefab exists (that wiring is a manual Editor step — see README).
        public string HrText { get; private set; } = "--";
        public string BpText { get; private set; } = "--/--";
        public string Spo2Text { get; private set; } = "--";
        public string ClockText { get; private set; } = "00:00";

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.SnapshotUpdated += Apply;
                if (controller.CurrentSnapshot != null)
                {
                    Apply(controller.CurrentSnapshot);
                }
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.SnapshotUpdated -= Apply;
            }
        }

        public void Apply(SimulationSnapshotView snapshot)
        {
            if (snapshot == null)
            {
                return;
            }
            HrText = Mathf.RoundToInt((float)snapshot.Hr).ToString();
            BpText = $"{Mathf.RoundToInt((float)snapshot.SbpMmHg)}/{Mathf.RoundToInt((float)snapshot.DbpMmHg)}";
            Spo2Text = $"{Mathf.RoundToInt((float)snapshot.Spo2)}%";
            ClockText = $"{snapshot.SimTimeSec / 60:00}:{snapshot.SimTimeSec % 60:00}";
        }

        public void OnPresentationCue(string cue)
        {
            // Monitor alarms etc. are wired later; the cue string comes from the engine.
        }
    }
}
