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
        private VisualElement _completionPanel;
        private Label _completionDetail;

        private float _lastSubmitTime = -999f;
        private bool _inputLocked;

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
            _completionPanel = root.Q<VisualElement>("completion-panel");
            _completionDetail = root.Q<Label>("completion-detail");

            root.Q<Button>("toggle-timeline").clicked += () =>
            {
                _timeline.Render(_controller.GetTimeline());
                _timeline.Toggle();
            };
            root.Q<Button>("exit-button").clicked += () => _controller.RequestExit();

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
            _resultBanner.AddToClassList("hidden");
            _resultText.text = "";
            _completionPanel.AddToClassList("hidden");
            _completionDetail.text = "";
            _timeline.Hide();
            _timeline.Render(_controller.GetTimeline());
            _vitals.Render(_controller.CurrentSnapshot);
            _actions.ResetCategory();
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
                _resultText.text =
                    $"{actionId} — done ({outcome.Classification})\n"
                    + $"t = {snap.SimTimeSec / 60:00}:{snap.SimTimeSec % 60:00}"
                    + (outcome.TriggeredRuleIds.Count > 0
                        ? $"   state change: {string.Join(", ", outcome.TriggeredRuleIds)}"
                        : "");
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
