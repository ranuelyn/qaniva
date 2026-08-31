using UnityEngine;
using Qaniva.Bridge;
using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Minimal integration-proof visual (blueprint sprint §17): renders the live
    /// engine snapshot as text via OnGUI, with zero scene/prefab dependencies.
    /// It is presentation only — every value shown comes from the engine snapshot.
    /// Replaced by the real room/monitor prefabs later (QAN-002).
    /// </summary>
    public sealed class IntegrationHud : MonoBehaviour
    {
        /// <summary>
        /// Self-attach after BridgeBootstrap (Presentation may reference Bridge,
        /// not the other way round, so the HUD hooks itself on).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToBridge()
        {
            var bridgeGo = GameObject.Find(BridgeBootstrap.BridgeGameObjectName);
            if (bridgeGo == null || bridgeGo.GetComponent<IntegrationHud>() != null)
            {
                return;
            }
            var controller = bridgeGo.GetComponent<SimulationBridgeController>();
            if (controller == null)
            {
                return;
            }
            bridgeGo.AddComponent<IntegrationHud>().Bind(controller);
        }

        private SimulationBridgeController _controller;
        private SimulationSnapshotView _snapshot;
        private string _lastAction = "-";
        private GUIStyle _style;

        public void Bind(SimulationBridgeController controller)
        {
            _controller = controller;
            _controller.SnapshotUpdated += OnSnapshot;
        }

        public void NoteAction(string actionId) => _lastAction = actionId;

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.SnapshotUpdated -= OnSnapshot;
            }
        }

        private void OnSnapshot(SimulationSnapshotView snapshot) => _snapshot = snapshot;

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(24, Screen.height / 40),
                normal = { textColor = Color.white },
            };

            string text = _snapshot == null
                ? "Qaniva simulation\nWaiting for START_SIMULATION…"
                : "Qaniva simulation (integration proof)\n"
                  + $"Runtime: {BridgeBootstrap.RuntimeKind}\n"
                  + $"t = {_snapshot.SimTimeSec}s\n"
                  + $"HR {_snapshot.Hr:0}  BP {_snapshot.SbpMmHg:0}/{_snapshot.DbpMmHg:0}  SpO2 {_snapshot.Spo2:0}%\n"
                  + $"Circulation: {_snapshot.Circulation}   Neuro: {_snapshot.Neuro}\n"
                  + $"Last action: {_lastAction}\n"
                  + (_snapshot.IsTerminal ? $"TERMINAL: {_snapshot.TerminalOutcome}" : "running");

            GUI.Label(new Rect(40, 60, Screen.width - 80, Screen.height - 120), text, _style);
        }
    }
}
