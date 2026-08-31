// INTERACTIVE-PATH E2E DRIVER — compiled only under QANIVA_INTEGRATION_AUTOPLAY
// and ACTIVE only when the host launches with mode == "e2e_ui".
//
// Unlike IntegrationAutoPlayer (which bypasses the UI on purpose), this driver
// verifies the INTERACTIVE code path: it selects the real category tabs and
// presses the real action Buttons of SimulationUiController through the UI
// Toolkit event system. Input therefore flows exactly like a human tap:
//   Button event -> ActionListPresenter click handler ->
//   SimulationUiController.Submit -> SimulationBridgeController ->
//   ClinicalRuntime -> clinical-core.
// Nothing calls the engine or the bridge controller directly from here.
#if QANIVA_INTEGRATION_AUTOPLAY
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Qaniva.Bridge;

namespace Qaniva.Presentation
{
    public sealed class InteractiveE2eDriver : MonoBehaviour
    {
        /// <summary>Single place that decides whether this driver may act (unit-tested).</summary>
        public static bool ShouldRunFor(string mode) => mode == BridgeProtocol.Modes.E2eUi;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            var bridgeGo = GameObject.Find(BridgeBootstrap.BridgeGameObjectName);
            if (bridgeGo == null || bridgeGo.GetComponent<InteractiveE2eDriver>() != null)
            {
                return;
            }
            if (bridgeGo.GetComponent<SimulationBridgeController>() == null)
            {
                return;
            }
            bridgeGo.AddComponent<InteractiveE2eDriver>();
        }

        private SimulationBridgeController _controller;
        private SimulationUiController _ui;
        private bool _running;
        private int _runIndex; // 1-based per armed run in this Unity process

        private void Awake()
        {
            _controller = GetComponent<SimulationBridgeController>();
            if (_controller != null)
            {
                _controller.SimulationStarted += OnSimulationStarted;
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.SimulationStarted -= OnSimulationStarted;
            }
        }

        private void OnSimulationStarted()
        {
            if (!ShouldRunFor(_controller.CurrentMode) || _running)
            {
                return;
            }
            _running = true;
            _runIndex++;
            StartCoroutine(PlayThroughTheUi());
        }

        private IEnumerator PlayThroughTheUi()
        {
            // Run 1 proves the ABORT path: two real actions, then the real Exit
            // button (EXIT_REQUESTED, no COMPLETED). Run 2 proves the full
            // completion path. Together with the host loop that is:
            //   RN -> Unity -> Exit -> RN -> Unity again -> complete -> RN Results.
            bool abortRun = _runIndex % 2 == 1;
            Debug.Log($"[InteractiveE2eDriver] e2e_ui run {_runIndex}: walking the REAL interactive UI"
                + (abortRun ? " (abort-path run)" : " (completion run)"));
            _ui = GetComponent<SimulationUiController>();
            yield return new WaitForSeconds(2.0f); // let the UI build + first layout

            int stepsTaken = 0;
            foreach (var actionId in IntegrationAutoPlayer.IdealPath)
            {
                if (abortRun && stepsTaken == 2)
                {
                    var exitRoot = _ui != null ? _ui.Root : null;
                    var exitButton = exitRoot?.Q<Button>("exit-button");
                    if (exitButton == null)
                    {
                        Debug.LogError("[InteractiveE2eDriver] exit button not found — aborting");
                        yield break;
                    }
                    Debug.Log("[InteractiveE2eDriver] pressing exit-button (abort path)");
                    Press(exitButton);
                    _running = false;
                    yield break;
                }
                // 1. select the category tab that hosts this action (real tab button)
                var root = _ui != null ? _ui.Root : null;
                if (root == null)
                {
                    Debug.LogError("[InteractiveE2eDriver] UI root missing — aborting");
                    yield break;
                }

                var actionButtonName = $"action-{actionId}";
                var button = root.Q<Button>(actionButtonName);
                if (button == null)
                {
                    // switch tabs through the real tab buttons until it appears
                    foreach (var tabName in new[] { "Patient", "Examine", "Orders", "Treat", "More" })
                    {
                        var tab = root.Q<Button>($"tab-{tabName}");
                        if (tab == null)
                        {
                            continue;
                        }
                        Press(tab);
                        yield return null; // let the list rebuild
                        button = root.Q<Button>(actionButtonName);
                        if (button != null)
                        {
                            break;
                        }
                    }
                }

                if (button == null)
                {
                    Debug.LogError($"[InteractiveE2eDriver] UI button for {actionId} not found — aborting");
                    yield break;
                }
                if (!button.enabledSelf)
                {
                    Debug.LogError($"[InteractiveE2eDriver] {actionId} is disabled in the UI — aborting");
                    yield break;
                }

                Debug.Log($"[InteractiveE2eDriver] pressing {actionButtonName}");
                Press(button);
                stepsTaken++;

                // respect the UI's double-submit debounce like a human would
                yield return new WaitForSeconds(Mathf.Max(1.0f, SimulationUiController.SubmitDebounceSeconds * 2));
            }

            _running = false;
            Debug.Log("[InteractiveE2eDriver] finished walking the ideal path");
        }

        /// <summary>Sends a submit through the real UI event system — dispatch,
        /// Clickable, and every registered clicked handler run exactly as for a tap.</summary>
        private static void Press(Button button)
        {
            using (var evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }
    }
}
#endif
