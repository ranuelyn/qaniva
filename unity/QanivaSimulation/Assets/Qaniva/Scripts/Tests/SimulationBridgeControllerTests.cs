using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Qaniva.Bridge;

namespace Qaniva.Simulation.Tests
{
    /// <summary>
    /// Proves the RN -> Unity -> RN round trip end to end with a fake transport and
    /// the stub runtime: START_SIMULATION in, SIMULATION_READY + SIMULATION_COMPLETED out.
    /// This is the "fake bridge + typed protocol + integration test" the blueprint
    /// asks for while the native embed (QAN-004) is still a spike.
    /// </summary>
    public class SimulationBridgeControllerTests
    {
        private sealed class InlineCaseProvider : ICaseProvider
        {
            public string GetCaseJson(string caseId, int caseVersion) =>
                $"{{\"id\":\"{caseId}\",\"version\":{caseVersion}}}";
        }

        private GameObject _go;
        private SimulationBridgeController _controller;
        private FakeUnityBridge _bridge;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("controller");
            _controller = _go.AddComponent<SimulationBridgeController>();
            _bridge = new FakeUnityBridge();
            _controller.Configure(_bridge, new StubClinicalRuntime(), new InlineCaseProvider());
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        private void StartSimulation()
        {
            var start = BridgeMessageCodec.Encode(
                BridgeProtocol.RnToUnity.StartSimulation,
                new StartSimulationPayload
                {
                    caseId = "demo_sync_bradycardia_001",
                    caseVersion = 1,
                    attemptId = "22222222-2222-4222-8222-222222222222",
                    seed = 7,
                });
            _bridge.PushFromHost(start);
        }

        [Test]
        public void StartProducesSimulationReady()
        {
            StartSimulation();

            Assert.AreEqual(1, _bridge.Sent.Count);
            var (type, _) = BridgeMessageCodec.DecodeEnvelope(_bridge.Sent[0], BridgeMessageCodec.UnityToRnTypes);
            Assert.AreEqual(BridgeProtocol.UnityToRn.SimulationReady, type);
        }

        [Test]
        public void PlayingToTerminalProducesSimulationCompleted()
        {
            StartSimulation();
            _bridge.Clear();

            for (int i = 0; i < 4; i++)
            {
                _controller.SubmitPlayerAction("treat", new Dictionary<string, string>());
            }

            Assert.AreEqual(1, _bridge.Sent.Count);
            var (type, payload) = BridgeMessageCodec.DecodeEnvelope(
                _bridge.Sent[0], BridgeMessageCodec.UnityToRnTypes);
            Assert.AreEqual(BridgeProtocol.UnityToRn.SimulationCompleted, type);

            var completed = BridgeMessageCodec.DecodePayload<SimulationCompletedPayload>(payload);
            Assert.AreEqual("22222222-2222-4222-8222-222222222222", completed.attemptId);
            Assert.AreEqual("complete", completed.summary.terminalState);
            Assert.AreEqual(4, completed.summary.timeline.Count);
        }

        [Test]
        public void ExitSimulationEmitsExitRequestedAndEvent()
        {
            StartSimulation();
            _bridge.Clear();
            bool raised = false;
            _controller.ExitRequested += () => raised = true;

            _bridge.PushFromHost(BridgeMessageCodec.Encode(
                BridgeProtocol.RnToUnity.ExitSimulation, new ExitSimulationPayload { reason = "user_quit" }));

            Assert.IsTrue(raised);
            var (type, _) = BridgeMessageCodec.DecodeEnvelope(_bridge.Sent[0], BridgeMessageCodec.UnityToRnTypes);
            Assert.AreEqual(BridgeProtocol.UnityToRn.ExitRequested, type);
        }

        [Test]
        public void DuplicateStartForSameAttemptReAnnouncesReadyWithoutReload()
        {
            StartSimulation();
            _controller.SubmitPlayerAction("treat", new Dictionary<string, string>());
            int timelineAfterOneAction = 1;
            _bridge.Clear();

            // Host retry: same attemptId again mid-run.
            StartSimulation();

            Assert.AreEqual(1, _bridge.Sent.Count);
            var (type, _) = BridgeMessageCodec.DecodeEnvelope(_bridge.Sent[0], BridgeMessageCodec.UnityToRnTypes);
            Assert.AreEqual(BridgeProtocol.UnityToRn.SimulationReady, type, "duplicate START must re-announce READY");

            // The in-flight simulation was NOT reloaded: finishing takes the
            // remaining 3 actions (stub terminates after 4), not a fresh 4.
            _bridge.Clear();
            for (int i = 0; i < 4 - timelineAfterOneAction; i++)
            {
                _controller.SubmitPlayerAction("treat", new Dictionary<string, string>());
            }
            var (doneType, payload) = BridgeMessageCodec.DecodeEnvelope(_bridge.Sent[0], BridgeMessageCodec.UnityToRnTypes);
            Assert.AreEqual(BridgeProtocol.UnityToRn.SimulationCompleted, doneType);
            var completed = BridgeMessageCodec.DecodePayload<SimulationCompletedPayload>(payload);
            Assert.AreEqual(4, completed.summary.timeline.Count, "timeline must span the ORIGINAL run");
        }

        [Test]
        public void CompletionIsEmittedExactlyOnce()
        {
            StartSimulation();
            _bridge.Clear();

            // Stub terminates after 4 accepted actions; submit extra ones after.
            for (int i = 0; i < 6; i++)
            {
                _controller.SubmitPlayerAction("treat", new Dictionary<string, string>());
            }
            // A late duplicate START retry after completion must not re-emit either.
            StartSimulation();

            int completed = 0;
            foreach (var raw in _bridge.Sent)
            {
                var (type, _) = BridgeMessageCodec.DecodeEnvelope(raw, BridgeMessageCodec.UnityToRnTypes);
                if (type == BridgeProtocol.UnityToRn.SimulationCompleted)
                {
                    completed++;
                }
            }
            Assert.AreEqual(1, completed, "SIMULATION_COMPLETED must be emitted exactly once per attempt");
        }

        [Test]
        public void ExposesRuntimeAvailabilityAndTimelineToTheUi()
        {
            StartSimulation();

            var availability = _controller.GetActionAvailability();
            Assert.IsTrue(availability.Count > 0, "availability projection must be exposed");
            foreach (var a in availability)
            {
                Assert.IsTrue(a.Visible && a.Enabled, "stub exposes enabled actions");
            }

            _controller.SubmitPlayerAction("treat", new Dictionary<string, string>());
            var timeline = _controller.GetTimeline();
            Assert.AreEqual(1, timeline.Count, "canonical timeline must be exposed");
            Assert.AreEqual("treat", timeline[0].ActionId);
        }

        [Test]
        public void StartPayloadModeDefaultsToInteractiveAndIsExposed()
        {
            StartSimulation(); // helper sends no mode -> C# field default
            Assert.AreEqual(BridgeProtocol.Modes.Interactive, _controller.CurrentMode);
        }

        [Test]
        public void RequestExitEmitsExitRequestedNotCompleted()
        {
            StartSimulation();
            _bridge.Clear();

            _controller.RequestExit();

            Assert.AreEqual(1, _bridge.Sent.Count);
            var (type, _) = BridgeMessageCodec.DecodeEnvelope(_bridge.Sent[0], BridgeMessageCodec.UnityToRnTypes);
            Assert.AreEqual(BridgeProtocol.UnityToRn.ExitRequested, type,
                "user abort must emit EXIT_REQUESTED and never SIMULATION_COMPLETED");
        }

#if QANIVA_INTEGRATION_AUTOPLAY
        [Test]
        public void E2eDriversAreInertOutsideTheirOwnMode()
        {
            Assert.IsFalse(Qaniva.Presentation.IntegrationAutoPlayer.ShouldRunFor(BridgeProtocol.Modes.Interactive));
            Assert.IsFalse(Qaniva.Presentation.IntegrationAutoPlayer.ShouldRunFor(BridgeProtocol.Modes.E2eUi));
            Assert.IsTrue(Qaniva.Presentation.IntegrationAutoPlayer.ShouldRunFor(BridgeProtocol.Modes.E2eAutoplay));

            Assert.IsFalse(Qaniva.Presentation.InteractiveE2eDriver.ShouldRunFor(BridgeProtocol.Modes.Interactive));
            Assert.IsFalse(Qaniva.Presentation.InteractiveE2eDriver.ShouldRunFor(BridgeProtocol.Modes.E2eAutoplay));
            Assert.IsTrue(Qaniva.Presentation.InteractiveE2eDriver.ShouldRunFor(BridgeProtocol.Modes.E2eUi));
        }
#endif

        [Test]
        public void MalformedHostMessageProducesSimulationFailed()
        {
            _bridge.PushFromHost("{ not json");

            var (type, payload) = BridgeMessageCodec.DecodeEnvelope(_bridge.Sent[0], BridgeMessageCodec.UnityToRnTypes);
            Assert.AreEqual(BridgeProtocol.UnityToRn.SimulationFailed, type);
            var failed = BridgeMessageCodec.DecodePayload<SimulationFailedPayload>(payload);
            Assert.AreEqual(BridgeProtocol.FailureCodes.BridgeProtocolError, failed.code);
        }
    }
}
