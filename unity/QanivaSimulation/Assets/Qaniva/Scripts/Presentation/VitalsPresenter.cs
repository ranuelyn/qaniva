using UnityEngine.UIElements;
using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Renders the vitals bar from the engine snapshot. Pure formatting — the
    /// snapshot remains the only authority; nothing is computed or cached here
    /// beyond the labels themselves.
    /// </summary>
    public sealed class VitalsPresenter
    {
        private readonly Label _hr;
        private readonly Label _bp;
        private readonly Label _spo2;
        private readonly Label _rr;
        private readonly Label _clock;
        private readonly Label _status;

        public VitalsPresenter(VisualElement root)
        {
            _hr = root.Q<Label>("vital-hr");
            _bp = root.Q<Label>("vital-bp");
            _spo2 = root.Q<Label>("vital-spo2");
            _rr = root.Q<Label>("vital-rr");
            _clock = root.Q<Label>("sim-clock");
            _status = root.Q<Label>("patient-status");
        }

        public void Render(SimulationSnapshotView s)
        {
            if (s == null)
            {
                return;
            }
            _hr.text = $"HR {s.Hr:0}";
            _bp.text = $"BP {s.SbpMmHg:0}/{s.DbpMmHg:0}";
            _spo2.text = $"SpO2 {s.Spo2:0}%";
            _rr.text = $"RR {s.RrPerMin:0}";
            _clock.text = $"{s.SimTimeSec / 60:00}:{s.SimTimeSec % 60:00}";
            _status.text = $"Rhythm: {s.Rhythm}   Circulation: {s.Circulation}   Neuro: {s.Neuro}";
        }
    }
}
