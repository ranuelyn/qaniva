using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Renders category tabs + the action list from the engine's canonical
    /// <see cref="ActionAvailabilityView"/> projection.
    ///
    /// Presentation-only responsibilities: grouping action TYPES into the product's
    /// five navigation categories (Patient/Examine/Orders/Treat/More) and drawing
    /// hidden/disabled/enabled states. It performs NO availability logic of its own
    /// — an action is a button exactly when the engine says Visible, and tappable
    /// exactly when the engine says Enabled.
    ///
    /// Element naming contract (used by the E2E UI driver and tests):
    ///   tabs    -> "tab-&lt;Category&gt;"   e.g. tab-Treat
    ///   actions -> "action-&lt;actionId&gt;" e.g. action-give_atropine
    /// </summary>
    public sealed class ActionListPresenter
    {
        /// <summary>Product taxonomy mapping (presentation, not clinical logic).</summary>
        private static readonly Dictionary<string, string> CategoryByType = new()
        {
            ["communication"] = "Patient",
            ["examine"] = "Examine",
            ["order"] = "Orders",
            ["medication"] = "Treat",
            ["procedure"] = "Treat",
            ["consult"] = "More",
            ["disposition"] = "More",
        };

        private static readonly string[] CategoryOrder = { "Patient", "Examine", "Orders", "Treat", "More" };

        private readonly VisualElement _tabs;
        private readonly ScrollView _list;
        private readonly Action<string> _onSubmit;

        private IReadOnlyList<ActionAvailabilityView> _current = Array.Empty<ActionAvailabilityView>();
        private string _activeCategory = "Examine";

        public ActionListPresenter(VisualElement root, Action<string> onSubmit)
        {
            _tabs = root.Q<VisualElement>("category-tabs");
            _list = root.Q<ScrollView>("action-list");
            _onSubmit = onSubmit;
        }

        public string ActiveCategory => _activeCategory;

        /// <summary>Back to the default tab — called on every SimulationStarted so a
        /// relaunch never inherits the previous attempt's tab position.</summary>
        public void ResetCategory() => _activeCategory = "Examine";

        public static string CategoryFor(string actionType) =>
            CategoryByType.TryGetValue(actionType, out var c) ? c : "More";

        /// <summary>Re-render tabs + list from a fresh engine projection.</summary>
        public void Render(IReadOnlyList<ActionAvailabilityView> availability)
        {
            _current = availability ?? Array.Empty<ActionAvailabilityView>();

            var visibleCategories = CategoryOrder
                .Where(c => _current.Any(a => a.Visible && CategoryFor(a.Type) == c))
                .ToList();

            if (visibleCategories.Count > 0 && !visibleCategories.Contains(_activeCategory))
            {
                _activeCategory = visibleCategories[0];
            }

            RenderTabs(visibleCategories);
            RenderList();
        }

        public void SelectCategory(string category)
        {
            _activeCategory = category;
            RenderTabs(CategoryOrder
                .Where(c => _current.Any(a => a.Visible && CategoryFor(a.Type) == c))
                .ToList());
            RenderList();
        }

        private void RenderTabs(List<string> categories)
        {
            _tabs.Clear();
            foreach (var category in categories)
            {
                var tab = new Button { name = $"tab-{category}", text = category };
                tab.AddToClassList("category-tab");
                if (category == _activeCategory)
                {
                    tab.AddToClassList("category-tab-active");
                }
                string captured = category;
                tab.clicked += () => SelectCategory(captured);
                _tabs.Add(tab);
            }
        }

        private void RenderList()
        {
            _list.Clear();
            foreach (var action in _current)
            {
                if (!action.Visible || CategoryFor(action.Type) != _activeCategory)
                {
                    continue; // HIDDEN, or belongs to another tab
                }

                var button = new Button { name = $"action-{action.ActionId}", text = action.Label };
                button.AddToClassList("action-button");

                if (action.Enabled)
                {
                    string capturedId = action.ActionId;
                    button.clicked += () => _onSubmit(capturedId);
                }
                else
                {
                    // VISIBLE + DISABLED: engine-worded reason, no interaction.
                    button.SetEnabled(false);
                    button.AddToClassList("action-button-disabled");
                }
                _list.Add(button);

                if (!action.Enabled && !string.IsNullOrEmpty(action.DisabledReason))
                {
                    var reason = new Label(action.DisabledReason);
                    reason.AddToClassList("action-disabled-reason");
                    _list.Add(reason);
                }
            }
        }
    }
}
