using UnityEngine;
using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// World-space bedside monitor readout. Renders ONLY values present in the
    /// canonical <see cref="SimulationSnapshotView"/> — no independent monitor
    /// simulation, no fabricated waveforms, no alarm thresholds (thresholds would
    /// be clinical logic; the screen-space vitals bar and this monitor both just
    /// show the engine's numbers).
    ///
    /// Prefab contract (children by name, each carrying a TextMesh):
    /// "HrValue", "BpValue", "Spo2Value", "RrValue", "ClockValue".
    /// </summary>
    public sealed class BedsideMonitorView : MonoBehaviour
    {
        private TextMesh _hr;
        private TextMesh _bp;
        private TextMesh _spo2;
        private TextMesh _rr;
        private TextMesh _clock;

        private void Awake() => CacheLabels();

        private void CacheLabels()
        {
            _hr = Find("HrValue");
            _bp = Find("BpValue");
            _spo2 = Find("Spo2Value");
            _rr = Find("RrValue");
            _clock = Find("ClockValue");
        }

        private TextMesh Find(string childName)
        {
            var child = transform.Find("Screen/" + childName) ?? transform.Find(childName);
            return child != null ? child.GetComponent<TextMesh>() : null;
        }

        public void SetVitals(SimulationSnapshotView s)
        {
            if (s == null)
            {
                return;
            }
            if (_hr == null)
            {
                CacheLabels(); // instantiated-from-prefab timing safety
            }
            Set(_hr, $"{s.Hr:0}");
            Set(_bp, $"{s.SbpMmHg:0}/{s.DbpMmHg:0}");
            Set(_spo2, $"{s.Spo2:0}");
            Set(_rr, $"{s.RrPerMin:0}");
            Set(_clock, $"{s.SimTimeSec / 60:00}:{s.SimTimeSec % 60:00}");
        }

        private static void Set(TextMesh label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }
    }
}
