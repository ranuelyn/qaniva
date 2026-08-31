// PlayMode proof of the INTERACTIVE path (QAN-006): a scripted "finger" presses
// the real UI Toolkit controls of SimulationUiController; input flows
//   Button event -> ActionListPresenter handler -> SimulationUiController.Submit
//   -> SimulationBridgeController -> ClinicalRuntime -> clinical-core
// and the canonical output must equal the committed golden replay. Nothing here
// calls the engine, the runtime, or SubmitPlayerAction directly.
#if QANIVA_HAS_CLINICAL_CORE
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Qaniva.Bridge;
using Qaniva.Clinical.Runtime;
using Qaniva.Presentation;

namespace Qaniva.Simulation.PlayModeTests
{
    public class InteractiveUiPlayModeTests
    {
        private const string GoldenReplayHash =
            "fe2191ff684f062290385fd967b47ebb58ba46932f88cbcebcc98d483d24dfc5";

        private static readonly string[] IdealPath =
        {
            "attach_monitor", "patient_history", "ecg_12lead", "iv_access",
            "give_atropine", "consult_cardiology", "disposition_ccu",
        };

        private sealed class FileCaseProvider : ICaseProvider
        {
            public string GetCaseJson(string caseId, int caseVersion) => File.ReadAllText(Path.Combine(
                Application.dataPath, "Qaniva/Resources/Qaniva/Cases/demo_sync_bradycardia_001.json"));
        }

        private GameObject _go;
        private SimulationBridgeController _controller;
        private SimulationUiController _ui;
        private FakeUnityBridge _bridge;

        private void CreateRig(string mode)
        {
            _go = new GameObject("interactive-ui-under-test");
            _controller = _go.AddComponent<SimulationBridgeController>();
            _bridge = new FakeUnityBridge();
            _controller.Configure(_bridge, new ClinicalRuntime(), new FileCaseProvider());
            _ui = _go.AddComponent<SimulationUiController>();
            _ui.Bind(_controller);

            _bridge.PushFromHost(BridgeMessageCodec.Encode(
                BridgeProtocol.RnToUnity.StartSimulation,
                new StartSimulationPayload
                {
                    caseId = "demo_sync_bradycardia_001",
                    caseVersion = 1,
                    attemptId = "22222222-2222-4222-8222-222222222222",
                    seed = 20260830,
                    mode = mode,
                }));
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        private static void Press(Button button)
        {
            using (var evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }

        /// <summary>Find the action's real button, switching category tabs through
        /// the real tab buttons when needed (exactly what a user does).</summary>
        private Button FindActionButton(string actionId)
        {
            var root = _ui.Root;
            var button = root.Q<Button>($"action-{actionId}");
            if (button != null)
            {
                return button;
            }
            foreach (var tabName in new[] { "Patient", "Examine", "Orders", "Treat", "More" })
            {
                var tab = root.Q<Button>($"tab-{tabName}");
                if (tab == null)
                {
                    continue;
                }
                Press(tab);
                button = root.Q<Button>($"action-{actionId}");
                if (button != null)
                {
                    return button;
                }
            }
            return null;
        }

        [UnityTest]
        public IEnumerator ManualUiPlayReproducesTheGoldenReplay()
        {
            CreateRig(BridgeProtocol.Modes.Interactive);
            yield return null; // one frame for UI layout

            Assert.IsNotNull(_ui.Root, "UI must have built");
            Assert.IsNotNull(_ui.Root.Q<Button>("tab-Examine"), "category tabs must render");

            foreach (var actionId in IdealPath)
            {
                var button = FindActionButton(actionId);
                Assert.IsNotNull(button, $"UI button for {actionId} must exist when the engine offers it");
                Assert.IsTrue(button.enabledSelf, $"{actionId} must be enabled at this point of the golden path");
                Press(button);
                // wait out the double-submit debounce like a human tap cadence
                yield return new WaitForSecondsRealtime(SimulationUiController.SubmitDebounceSeconds + 0.05f);
            }

            // Exactly one COMPLETED, carrying the golden canonical result.
            int completedCount = 0;
            SimulationCompletedPayload completed = null;
            foreach (var raw in _bridge.Sent)
            {
                var (type, payload) = BridgeMessageCodec.DecodeEnvelope(raw, BridgeMessageCodec.UnityToRnTypes);
                if (type == BridgeProtocol.UnityToRn.SimulationCompleted)
                {
                    completedCount++;
                    completed = BridgeMessageCodec.DecodePayload<SimulationCompletedPayload>(payload);
                }
            }

            Assert.AreEqual(1, completedCount, "SIMULATION_COMPLETED exactly once");
            Assert.AreEqual("complete", completed.summary.terminalState);
            Assert.AreEqual(80d, completed.summary.totalScore, 0.0001);
            Assert.AreEqual(7, completed.summary.timeline.Count);
            Assert.AreEqual(GoldenReplayHash, completed.summary.replayHash,
                "manual UI play must reproduce the exact golden canonical replay hash");
        }

        [UnityTest]
        public IEnumerator HiddenAndDisabledStatesRenderFromTheEngineProjection()
        {
            CreateRig(BridgeProtocol.Modes.Interactive);
            yield return null;
            var root = _ui.Root;

            // transcutaneous_pacing is HIDDEN at t=0 (visibleWhen unmet) — no button on any tab.
            Assert.IsNull(FindActionButton("transcutaneous_pacing"),
                "hidden actions must not render at all");

            // give_atropine is VISIBLE + DISABLED (precondition iv_access) on the Treat tab.
            var atropine = FindActionButton("give_atropine");
            Assert.IsNotNull(atropine, "disabled actions must still be visible");
            Assert.IsFalse(atropine.enabledSelf, "unmet precondition renders as disabled");

            // After IV access through the UI, atropine becomes enabled.
            var iv = FindActionButton("iv_access");
            Press(iv);
            yield return new WaitForSecondsRealtime(SimulationUiController.SubmitDebounceSeconds + 0.05f);
            atropine = FindActionButton("give_atropine");
            Assert.IsNotNull(atropine);
            Assert.IsTrue(atropine.enabledSelf, "met precondition renders as enabled");
        }

        [UnityTest]
        public IEnumerator DoubleTapSubmitsOnlyOnce()
        {
            CreateRig(BridgeProtocol.Modes.Interactive);
            yield return null;

            var button = FindActionButton("attach_monitor");
            Assert.IsNotNull(button);
            Press(button);
            Press(button); // duplicated UI event inside the debounce window

            yield return new WaitForSecondsRealtime(0.1f);
            Assert.AreEqual(1, _controller.GetTimeline().Count,
                "a double tap must execute the action exactly once");
        }

#if QANIVA_INTEGRATION_AUTOPLAY
        [UnityTest]
        public IEnumerator InteractiveModeNeverAutoplays()
        {
            CreateRig(BridgeProtocol.Modes.Interactive);
            _go.AddComponent<IntegrationAutoPlayer>();
            _go.AddComponent<InteractiveE2eDriver>();

            // Fresh interactive START after the drivers subscribed, so their
            // SimulationStarted gate is genuinely exercised.
            _bridge.PushFromHost(BridgeMessageCodec.Encode(
                BridgeProtocol.RnToUnity.StartSimulation,
                new StartSimulationPayload
                {
                    caseId = "demo_sync_bradycardia_001",
                    caseVersion = 1,
                    attemptId = "33333333-3333-4333-8333-333333333333",
                    seed = 20260830,
                    mode = BridgeProtocol.Modes.Interactive,
                }));

            // Give any (wrongly) armed driver ample time to act (their first step
            // fires ~1.5-2s after start).
            yield return new WaitForSecondsRealtime(3.0f);

            Assert.AreEqual(0, _controller.GetTimeline().Count,
                "interactive mode must not auto-play a single action");
        }
#endif
    }
}
#endif
