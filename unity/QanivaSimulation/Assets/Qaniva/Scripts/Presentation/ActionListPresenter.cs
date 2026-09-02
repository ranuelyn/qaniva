using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Renders the category dock + the decision rows from the engine's canonical
    /// <see cref="ActionAvailabilityView"/> projection.
    ///
    /// Presentation-only responsibilities: grouping action TYPES into the product's
    /// five navigation categories (Patient/Examine/Orders/Treat/More), splitting an
    /// authored label into title/secondary lines, and translating the engine's
    /// machine-worded disabled reason into display copy. It performs NO
    /// availability logic of its own — an action is a row exactly when the engine
    /// says Visible, and tappable exactly when the engine says Enabled.
    ///
    /// Element naming contract (used by the E2E UI driver and tests):
    ///   tabs    -> "tab-&lt;Category&gt;"   e.g. tab-Treat        (Button)
    ///   actions -> "action-&lt;actionId&gt;" e.g. action-give_atropine (Button)
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

        /// <summary>Display names (Turkish product language). Element NAMES keep the
        /// English category keys (tab-Examine …) — that is the driver/test contract.</summary>
        private static readonly Dictionary<string, string> CategoryDisplay = new()
        {
            ["Patient"] = "Hasta",
            ["Examine"] = "Muayene",
            ["Orders"] = "İstemler",
            ["Treat"] = "Tedavi",
            ["More"] = "Diğer",
        };

        private static readonly string[] CategoryOrder = { "Patient", "Examine", "Orders", "Treat", "More" };

        private readonly VisualElement _tabs;
        private readonly ScrollView _list;
        private readonly Action<string> _onSubmit;

        private IReadOnlyList<ActionAvailabilityView> _current = Array.Empty<ActionAvailabilityView>();
        private string _activeCategory = "Examine";

        /// <summary>Raised when the user taps the already-active category — the
        /// controller uses it to collapse/expand the sheet.</summary>
        public event Action ActiveCategoryReselected;

        /// <summary>Raised when a DIFFERENT category is chosen — the controller
        /// expands the sheet so the newly chosen rows are actually visible.</summary>
        public event Action CategoryChanged;

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

        /// <summary>Display copy for the engine's disabled reason. The engine's
        /// wording is machine-oriented ("already performed", "requires: expr");
        /// the row shows a short clinical status instead. Unknown reasons pass
        /// through unchanged so nothing is hidden.</summary>
        public static string StatusFor(string disabledReason)
        {
            if (string.IsNullOrEmpty(disabledReason))
            {
                return "Kullanılamaz";
            }
            if (disabledReason == "already performed")
            {
                return "Yapıldı";
            }
            if (disabledReason.StartsWith("requires:", StringComparison.Ordinal))
            {
                return "Henüz uygun değil";
            }
            return disabledReason;
        }

        /// <summary>Splits "Epinephrine 0.5 mg IM — anterolateral thigh" into a
        /// title and a secondary descriptor line. Labels without a dash stay whole.</summary>
        public static (string title, string secondary) SplitLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return ("", "");
            }
            foreach (var separator in new[] { " — ", " – ", " - " })
            {
                int i = label.IndexOf(separator, StringComparison.Ordinal);
                if (i > 0 && i < label.Length - separator.Length)
                {
                    return (label.Substring(0, i).Trim(), label.Substring(i + separator.Length).Trim());
                }
            }
            return (label, "");
        }

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
            if (category == _activeCategory)
            {
                ActiveCategoryReselected?.Invoke();
                return;
            }
            _activeCategory = category;
            CategoryChanged?.Invoke();
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
                var tab = new Button { name = $"tab-{category}", text = CategoryDisplay.TryGetValue(category, out var shown) ? shown : category };
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

                // The row IS a Button (name contract + real click semantics); its
                // text is empty and the content is composed from child labels.
                var row = new Button { name = $"action-{action.ActionId}", text = "" };
                row.AddToClassList("action-row");

                var (title, secondary) = SplitLabel(action.Label);
                var copy = new VisualElement { pickingMode = PickingMode.Ignore };
                copy.AddToClassList("action-row-copy");
                var titleLabel = new Label(title) { pickingMode = PickingMode.Ignore };
                titleLabel.AddToClassList("action-row-title");
                copy.Add(titleLabel);
                if (!string.IsNullOrEmpty(secondary))
                {
                    var secondaryLabel = new Label(secondary) { pickingMode = PickingMode.Ignore };
                    secondaryLabel.AddToClassList("action-row-secondary");
                    copy.Add(secondaryLabel);
                }
                row.Add(copy);

                if (action.Enabled)
                {
                    var chevron = new Label("›") { pickingMode = PickingMode.Ignore };
                    chevron.AddToClassList("action-row-chevron");
                    row.Add(chevron);

                    string capturedId = action.ActionId;
                    row.clicked += () => _onSubmit(capturedId);
                }
                else
                {
                    // VISIBLE + DISABLED: engine-worded reason → inline status, no interaction.
                    row.SetEnabled(false);
                    row.AddToClassList("action-row-disabled");
                    if (action.DisabledReason == "already performed")
                    {
                        row.AddToClassList("action-row-done");
                    }
                    var status = new Label(StatusFor(action.DisabledReason)) { pickingMode = PickingMode.Ignore };
                    status.AddToClassList("action-row-status");
                    row.Add(status);
                }
                _list.Add(row);
            }
        }
    }
}
