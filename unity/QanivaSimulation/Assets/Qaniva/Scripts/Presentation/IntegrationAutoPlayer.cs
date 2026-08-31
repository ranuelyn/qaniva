// INTEGRATION-PROOF DRIVER — compiled only under QANIVA_INTEGRATION_AUTOPLAY.
// Until the real in-simulation action UI exists (QAN-006), this steps the demo
// case's ideal path through SimulationBridgeController.SubmitPlayerAction —
// the exact entry point the future action drawer will use. It contains no
// clinical logic: the action list mirrors the committed golden replay script
// (clinical-core/Qaniva.Clinical.Tests/Golden/ideal_path.script.json) and every
// outcome comes from the deterministic engine. Remove with QAN-006.
#if QANIVA_INTEGRATION_AUTOPLAY
using System.Collections;
using UnityEngine;
using Qaniva.Bridge;
using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    public sealed class IntegrationAutoPlayer : MonoBehaviour
    {
        private static readonly string[] IdealPath =
        {
            "attach_monitor", "patient_history", "ecg_12lead", "iv_access",
            "give_atropine", "consult_cardiology", "disposition_ccu",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            var bridgeGo = GameObject.Find(BridgeBootstrap.BridgeGameObjectName);
            if (bridgeGo == null || bridgeGo.GetComponent<IntegrationAutoPlayer>() != null)
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
                _controller.SnapshotUpdated += OnSnapshot;
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.SnapshotUpdated -= OnSnapshot;
            }
        }

        private void OnSnapshot(SimulationSnapshotView snapshot)
        {
            // A fresh simulation announces itself with a t=0 snapshot.
            if (!_running && snapshot != null && snapshot.SimTimeSec == 0 && !snapshot.IsTerminal)
            {
                _running = true;
                StartCoroutine(PlayIdealPath());
            }
        }

        private IEnumerator PlayIdealPath()
        {
            Debug.Log("[IntegrationAutoPlayer] driving the demo ideal path");
            yield return new WaitForSeconds(1.5f);
            var hud = GetComponent<IntegrationHud>();
            foreach (var actionId in IdealPath)
            {
                var outcome = _controller.SubmitPlayerAction(actionId);
                hud?.NoteAction($"{actionId} -> {(outcome.Accepted ? "ok" : outcome.RejectionReason)}");
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
