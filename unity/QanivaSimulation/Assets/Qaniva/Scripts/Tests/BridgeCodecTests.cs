using NUnit.Framework;
using Qaniva.Bridge;

namespace Qaniva.Simulation.Tests
{
    public class BridgeCodecTests
    {
        [Test]
        public void EncodesAndDecodesStartSimulation()
        {
            var json = BridgeMessageCodec.Encode(
                BridgeProtocol.RnToUnity.StartSimulation,
                new StartSimulationPayload
                {
                    caseId = "demo_sync_bradycardia_001",
                    caseVersion = 1,
                    attemptId = "22222222-2222-4222-8222-222222222222",
                    locale = "en",
                    difficulty = "standard",
                    seed = 42,
                });

            var (type, payload) = BridgeMessageCodec.DecodeEnvelope(json, BridgeMessageCodec.RnToUnityTypes);
            Assert.AreEqual(BridgeProtocol.RnToUnity.StartSimulation, type);

            var decoded = BridgeMessageCodec.DecodePayload<StartSimulationPayload>(payload);
            Assert.AreEqual("demo_sync_bradycardia_001", decoded.caseId);
            Assert.AreEqual(1, decoded.caseVersion);
            Assert.AreEqual(42, decoded.seed);
        }

        [Test]
        public void RejectsWrongProtocolVersion()
        {
            var json = BridgeMessageCodec.Encode(
                BridgeProtocol.RnToUnity.StartSimulation, new StartSimulationPayload());
            var tampered = json.Replace("\"protocolVersion\":1", "\"protocolVersion\":999");

            Assert.Throws<BridgeProtocolException>(
                () => BridgeMessageCodec.DecodeEnvelope(tampered, BridgeMessageCodec.RnToUnityTypes));
        }

        [Test]
        public void RejectsMessageOnWrongChannel()
        {
            var json = BridgeMessageCodec.Encode(
                BridgeProtocol.UnityToRn.SimulationReady, new SimulationReadyPayload());

            Assert.Throws<BridgeProtocolException>(
                () => BridgeMessageCodec.DecodeEnvelope(json, BridgeMessageCodec.RnToUnityTypes));
        }

        [Test]
        public void ProtocolVersionMatchesContract()
        {
            // Kept in lockstep with packages/contracts/src/protocol.ts (PROTOCOL_VERSION).
            Assert.AreEqual(1, BridgeProtocol.ProtocolVersion);
        }
    }
}
