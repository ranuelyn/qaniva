using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Qaniva.Bridge;
using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// The in-simulation interaction surface (UI Toolkit). Orchestrates the
    /// presenters and routes user intent into the deterministic pipeline:
    ///
    ///   user tap -> Submit(actionId) -> SimulationBridgeController.SubmitPlayerAction
    ///            -> ClinicalRuntime -> clinical-core -> ActionOutcomeView
    ///            -> render result + refresh vitals/actions/timeline
    ///
    /// The UI renders engine output only; it never computes availability, state,
    /// scores, or completion. Terminal state and SIMULATION_COMPLETED are owned by
    /// the bridge controller/engine.
    /// </summary>
    public sealed class SimulationUiController : MonoBehaviour
    {
        /// <summary>Ignore taps arriving within this window after an accepted submit
        /// (double-tap / duplicated UI event protection; the engine's own rejection
        /// of non-repeatable actions remains the canonical backstop).</summary>
        public const float SubmitDebounceSeconds = 0.3f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToBridge()
        {
            var bridgeGo = GameObject.Find(BridgeBootstrap.BridgeGameObjectName);
            if (bridgeGo == null || bridgeGo.GetComponent<SimulationUiController>() != null)
            {
                return;
            }
            var controller = bridgeGo.GetComponent<SimulationBridgeController>();
            if (controller == null)
            {
                return;
            }
            bridgeGo.AddComponent<SimulationUiController>().Bind(controller);
        }

        private SimulationBridgeController _controller;
        private UIDocument _document;
        private VitalsPresenter _vitals;
        private ActionListPresenter _actions;
        private TimelinePresenter _timeline;
        private VisualElement _resultBanner;
        private Label _resultText;
        private Button _resultViewButton;
        private ResultViewerPresenter _resultViewer;
        private string _openableAssetId;
        private string _openableAssetLabel;
        private string _openableAssetStatus;
        private VisualElement _completionPanel;
        private Label _completionDetail;

        private VisualElement _actionArea;
        private float _lastSubmitTime = -999f;
        private bool _inputLocked;

        public bool SheetCollapsed => _actionArea != null && _actionArea.ClassListContains("sheet-collapsed");

        private void ToggleSheet()
        {
            if (_actionArea == null)
            {
                return;
            }
            if (SheetCollapsed)
            {
                _actionArea.RemoveFromClassList("sheet-collapsed");
            }
            else
            {
                _actionArea.AddToClassList("sheet-collapsed");
            }
        }

        public VisualElement Root => _document != null ? _document.rootVisualElement : null;

        public void Bind(SimulationBridgeController controller)
        {
            _controller = controller;

            _document = gameObject.GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            _document.panelSettings = Resources.Load<PanelSettings>("Qaniva/UI/QanivaPanelSettings");
            _document.visualTreeAsset = Resources.Load<VisualTreeAsset>("Qaniva/UI/SimulationScreen");

            if (_document.panelSettings == null || _document.visualTreeAsset == null)
            {
                Debug.LogError("[SimulationUi] UI assets missing — run QanivaBuild.CreateUiAssets. "
                    + "The simulation is NOT interactable.");
                return;
            }

            var root = _document.rootVisualElement;
            var sheet = Resources.Load<StyleSheet>("Qaniva/UI/SimulationScreen");
            if (sheet != null)
            {
                root.styleSheets.Add(sheet);
            }

            _vitals = new VitalsPresenter(root);
            _actions = new ActionListPresenter(root, Submit);
            _timeline = new TimelinePresenter(root);
            _resultBanner = root.Q<VisualElement>("result-banner");
            _resultText = root.Q<Label>("result-text");
            _resultViewButton = root.Q<Button>("result-view-button");
            _resultViewer = new ResultViewerPresenter(root);
            _resultViewButton.clicked += () =>
            {
                if (!string.IsNullOrEmpty(_openableAssetId))
                {
                    _resultViewer.Open(_openableAssetId, _openableAssetLabel, _openableAssetStatus);
                }
            };
            _completionPanel = root.Q<VisualElement>("completion-panel");
            _completionDetail = root.Q<Label>("completion-detail");

            root.Q<Button>("toggle-timeline").clicked += () =>
            {
                _timeline.Render(_controller.GetTimeline());
                _timeline.Toggle();
            };
            root.Q<Button>("exit-button").clicked += () => _controller.RequestExit();

            // Action sheet: the grabber and a tap on the already-active category
            // both collapse/expand the decision rows so the patient can take the
            // whole screen. Pure presentation state; engine untouched.
            _actionArea = root.Q<VisualElement>("action-area");
            var handle = root.Q<Button>("sheet-handle");
            if (handle != null)
            {
                var bar = new VisualElement { pickingMode = PickingMode.Ignore };
                bar.AddToClassList("sheet-handle-bar");
                handle.Add(bar);
                handle.clicked += ToggleSheet;
            }
            // Re-tapping the active category only ever EXPANDS (a collapsed sheet
            // must reopen from the dock); collapsing is the grabber's explicit job.
            void ExpandSheet()
            {
                if (SheetCollapsed)
                {
                    ToggleSheet();
                }
            }
            _actions.ActiveCategoryReselected += ExpandSheet;
            _actions.CategoryChanged += ExpandSheet;

            ApplySafeArea(root);

            _controller.SimulationStarted += OnSimulationStarted;
            _controller.SnapshotUpdated += OnSnapshot;

            if (_controller.CurrentSnapshot != null)
            {
                OnSimulationStarted();
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.SimulationStarted -= OnSimulationStarted;
                _controller.SnapshotUpdated -= OnSnapshot;
            }
        }

        /// <summary>Fresh simulation (first run or warm-runtime relaunch): reset ALL
        /// presentation state so nothing leaks between attempts.</summary>
        private void OnSimulationStarted()
        {
            _inputLocked = false;
            _lastSubmitTime = -999f;
            // Close the viewer FIRST: it restores the action area / result banner
            // visibility it borrowed, and the reset below then hides the banner.
            _resultViewer.Close();
            _resultBanner.AddToClassList("hidden");
            _resultText.text = "";
            _resultViewButton.AddToClassList("hidden");
            _openableAssetId = null;
            _openableAssetLabel = null;
            _openableAssetStatus = null;
            _completionPanel.AddToClassList("hidden");
            _completionDetail.text = "";
            _timeline.Hide();
            _timeline.Render(_controller.GetTimeline());
            _vitals.Render(_controller.CurrentSnapshot);
            _actions.ResetCategory();
            _actionArea?.RemoveFromClassList("sheet-collapsed");
            _actions.Render(_controller.GetActionAvailability());
        }

        private void OnSnapshot(SimulationSnapshotView snapshot) => _vitals.Render(snapshot);

        /// <summary>
        /// The single user-intent entry point (also used by the E2E UI driver via the
        /// real buttons). Guards double-submission, forwards the canonical action id,
        /// renders whatever the engine returned.
        /// </summary>
        public void Submit(string actionId)
        {
            if (_controller == null || _inputLocked)
            {
                return;
            }
            if (Time.realtimeSinceStartup - _lastSubmitTime < SubmitDebounceSeconds)
            {
                return; // duplicated UI event / double tap
            }

            _inputLocked = true;
            try
            {
                var outcome = _controller.SubmitPlayerAction(actionId, new Dictionary<string, string>());
                _lastSubmitTime = Time.realtimeSinceStartup;
                RenderOutcome(actionId, outcome);
            }
            finally
            {
                _inputLocked = false;
            }
        }

        private void RenderOutcome(string actionId, ActionOutcomeView outcome)
        {
            _resultBanner.RemoveFromClassList("hidden");
            if (outcome.Accepted)
            {
                var snap = outcome.Snapshot;
                var text = new System.Text.StringBuilder();
                text.Append($"t = {snap.SimTimeSec / 60:00}:{snap.SimTimeSec % 60:00}");

                // Case-authored result narrative + any facts this step disclosed —
                // all engine-provided text; the UI adds nothing clinical.
                if (!string.IsNullOrEmpty(outcome.ResultText))
                {
                    text.Append('\n').Append(outcome.ResultText);
                }
                foreach (var fact in outcome.NewlyDisclosedFacts)
                {
                    if (!string.IsNullOrEmpty(fact.Text))
                    {
                        text.Append('\n').Append(fact.Text);
                    }
                }
                if (string.IsNullOrEmpty(outcome.ResultText) && outcome.NewlyDisclosedFacts.Count == 0)
                {
                    text.Append('\n').Append($"{actionId} — done");
                }
                _resultText.text = text.ToString();

                _openableAssetId = outcome.ResultAssetId;
                _openableAssetLabel = string.IsNullOrEmpty(outcome.ResultAssetLabel)
                    ? actionId
                    : outcome.ResultAssetLabel;
                _openableAssetStatus = outcome.ResultAssetClinicalStatus;
                if (string.IsNullOrEmpty(_openableAssetId))
                {
                    _resultViewButton.AddToClassList("hidden");
                }
                else
                {
                    _resultViewButton.RemoveFromClassList("hidden");
                    _resultViewer.Open(_openableAssetId, _openableAssetLabel, _openableAssetStatus);
                }
            }
            else
            {
                _resultText.text = $"{actionId} — not performed: {outcome.RejectionReason}";
            }

            _actions.Render(_controller.GetActionAvailability());
            if (_timeline.Visible)
            {
                _timeline.Render(_controller.GetTimeline());
            }

            if (outcome.Terminated && outcome.Snapshot != null)
            {
                _completionPanel.RemoveFromClassList("hidden");
                _completionDetail.text =
                    $"Outcome: {outcome.Snapshot.TerminalOutcome}\nReturning to results…";
            }
        }

        private static void ApplySafeArea(VisualElement root)
        {
            // Screen.safeArea is in pixels bottom-left origin; panel works top-down.
            var safe = Screen.safeArea;
            float topInset = Screen.height - (safe.y + safe.height);
            float bottomInset = safe.y;
            var top = root.Q<VisualElement>("safe-top");
            var bottom = root.Q<VisualElement>("safe-bottom");
            if (top != null && topInset > 0)
            {
                top.style.height = topInset;
            }
            if (bottom != null && bottomInset > 0)
            {
                bottom.style.height = bottomInset;
            }
        }
    }
}
