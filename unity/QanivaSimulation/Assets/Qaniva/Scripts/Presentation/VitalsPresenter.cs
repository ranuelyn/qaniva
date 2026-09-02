using System.Text;
using UnityEngine.UIElements;
using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Renders the vitals strip from the engine snapshot. Pure formatting — the
    /// snapshot remains the only authority; nothing is computed or cached here
    /// beyond the labels themselves. Captions ("HR", "BP", …) live in UXML; the
    /// labels carry values only. Engine state identifiers (e.g. "sinus_rhythm")
    /// are displayed in humanized form — the identifier itself is never altered.
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

        /// <summary>Turkish display copy for the engine's state identifiers (schema
        /// enums + case rhythm ids). Unknown identifiers fall back to a readable
        /// "snake_case → Sentence case" form so nothing is ever hidden.</summary>
        private static readonly System.Collections.Generic.Dictionary<string, string> Display = new()
        {
            ["sinus_rhythm"] = "Sinüs ritmi",
            ["sinus_tachycardia"] = "Sinüs taşikardisi",
            ["sinus_bradycardia"] = "Sinüs bradikardisi",
            ["demo_bradycardia"] = "Bradikardi (demo)",
            ["ventricular_fibrillation"] = "Ventriküler fibrilasyon",
            ["asystole"] = "Asistoli",
            ["normal"] = "Dolaşım normal",
            ["poor_perfusion"] = "Zayıf perfüzyon",
            ["shock"] = "Şok",
            ["arrest"] = "Arrest",
            ["alert"] = "Uyanık",
            ["voice"] = "Sese yanıt veriyor",
            ["pain"] = "Ağrıya yanıt veriyor",
            ["unresponsive"] = "Yanıtsız",
            ["patent"] = "Hava yolu açık",
            ["at_risk"] = "Hava yolu riskli",
            ["obstructed"] = "Hava yolu tıkalı",
            ["spontaneous"] = "Spontan solunum",
            ["labored"] = "Zorlu solunum",
            ["assisted"] = "Destekli solunum",
            ["apneic"] = "Apneik",
            ["correct"] = "Doğru",
            ["delayed"] = "Gecikmiş",
            ["missed"] = "Kaçırıldı",
            ["harmful"] = "Zararlı",
            ["neutral"] = "Nötr",
            ["unnecessary"] = "Gereksiz",
        };

        /// <summary>"sinus_rhythm" → "Sinüs ritmi" (known) or "Sinus rhythm" (fallback).</summary>
        public static string Humanize(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return "";
            }
            if (Display.TryGetValue(identifier, out var shown))
            {
                return shown;
            }
            var sb = new StringBuilder(identifier.Length);
            foreach (char c in identifier)
            {
                sb.Append(c == '_' ? ' ' : c);
            }
            var text = sb.ToString().Trim();
            return text.Length == 0 ? "" : char.ToUpperInvariant(text[0]) + text.Substring(1);
        }

        public void Render(SimulationSnapshotView s)
        {
            if (s == null)
            {
                return;
            }
            _hr.text = $"{s.Hr:0}";
            _bp.text = $"{s.SbpMmHg:0}/{s.DbpMmHg:0}";
            _spo2.text = $"{s.Spo2:0}%";
            _rr.text = $"{s.RrPerMin:0}";
            _clock.text = $"{s.SimTimeSec / 60:00}:{s.SimTimeSec % 60:00}";

            var parts = new System.Collections.Generic.List<string>(3);
            if (!string.IsNullOrEmpty(s.Rhythm)) parts.Add(Humanize(s.Rhythm));
            if (!string.IsNullOrEmpty(s.Circulation)) parts.Add(Humanize(s.Circulation));
            if (!string.IsNullOrEmpty(s.Neuro)) parts.Add(Humanize(s.Neuro));
            _status.text = string.Join("  ·  ", parts);
        }
    }
}
