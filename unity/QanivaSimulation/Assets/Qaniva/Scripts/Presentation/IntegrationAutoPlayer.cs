// E2E REGRESSION DRIVER — compiled only under QANIVA_INTEGRATION_AUTOPLAY and
// ACTIVE only when the host launches with START_SIMULATION.payload.mode ==
// "e2e_autoplay" (BridgeProtocol.Modes.E2eAutoplay). Interactive launches (the
// production default) never trigger it, even in builds that carry the define.
//
// It applies the demo golden script directly to the runtime via
// SubmitPlayerAction — deliberately BYPASSING the UI — to regression-test the
// bridge/engine/lifecycle path in isolation. The interactive UI path is covered
// separately by InteractiveE2eDriver (mode == "e2e_ui"), which presses the real
// UI controls. Do not confuse the two.
#if QANIVA_INTEGRATION_AUTOPLAY
using System.Collections;
using UnityEngine;
using Qaniva.Bridge;

namespace Qaniva.Presentation
{
    public sealed class IntegrationAutoPlayer : MonoBehaviour
    {
        // Mirrors clinical-core/Qaniva.Clinical.Tests/Golden/ideal_path.script.json.
        internal static readonly string[] IdealPath =
        {
            "attach_monitor", "patient_history", "ecg_12lead", "iv_access",
            "give_atropine", "consult_cardiology", "disposition_ccu",
        };

        /// <summary>Single place that decides whether this driver may act (unit-tested).</summary>
        public static bool ShouldRunFor(string mode) => mode == BridgeProtocol.Modes.E2eAutoplay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            var bridgeGo = GameObject.Find(BridgeBootstrap.BridgeGameObjectName);
            if (bridgeGo == null || bridgeGo.GetComponent<IntegrationAutoPlayer>() != null)
            {
                return;
            }
            if (bridgeGo.GetComponent<SimulationBridgeController>() == null)
            {
                return;
            }
            bridgeGo.AddComponent<IntegrationAutoPlayer>();
        }

        private SimulationBridgeController _controller;
        private bool _running;

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
            if (!ShouldRunFor(_controller.CurrentMode))
            {
                return; // interactive / e2e_ui launches: this driver stays inert
            }
            if (!_running)
            {
                _running = true;
                StartCoroutine(PlayIdealPath());
            }
        }

        private IEnumerator PlayIdealPath()
        {
            Debug.Log("[IntegrationAutoPlayer] e2e_autoplay: driving the demo ideal path (runtime-direct)");
            yield return new WaitForSeconds(1.5f);
            foreach (var actionId in IdealPath)
            {
                var outcome = _controller.SubmitPlayerAction(actionId);
                Debug.Log($"[IntegrationAutoPlayer] {actionId}: accepted={outcome.Accepted} terminated={outcome.Terminated}");
                if (outcome.Terminated)
                {
                    break;
                }
                yield return new WaitForSeconds(1.0f);
            }
            _running = false;
        }
    }
}
#endif
