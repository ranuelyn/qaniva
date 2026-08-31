using System.Collections.Generic;
using UnityEngine.UIElements;
using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Renders the in-simulation case log from the engine's canonical timeline
    /// (<see cref="SimulationBridgeController.GetTimeline"/> upstream). There is no
    /// Unity-side action log that could drift from the engine's history.
    /// </summary>
    public sealed class TimelinePresenter
    {
        private readonly VisualElement _panel;
        private readonly ScrollView _scroll;

        public TimelinePresenter(VisualElement root)
        {
            _panel = root.Q<VisualElement>("timeline-panel");
            _scroll = root.Q<ScrollView>("timeline-scroll");
        }

        public bool Visible => !_panel.ClassListContains("hidden");

        public void Toggle()
        {
            if (Visible)
            {
                _panel.AddToClassList("hidden");
            }
            else
            {
                _panel.RemoveFromClassList("hidden");
            }
        }

        public void Hide() => _panel.AddToClassList("hidden");

        public void Render(IReadOnlyList<TimelineEntryView> timeline)
        {
            _scroll.Clear();
            if (timeline == null)
            {
                return;
            }
            foreach (var e in timeline)
            {
                var label = new Label(
                    $"#{e.Seq}  {e.SimTimeSec / 60:00}:{e.SimTimeSec % 60:00}  {e.Label}  [{e.Classification}]")
                {
                    name = $"timeline-{e.Seq}",
                };
                label.AddToClassList("timeline-entry");
                _scroll.Add(label);
            }
        }
    }
}
