// Compiles only when the real engine DLL is synced and QANIVA_HAS_CLINICAL_CORE
// is set (Project Settings -> Player -> Scripting Define Symbols). This is the
// QAN-003 proof: Unity -> ClinicalRuntime -> Qaniva.Clinical.Core.dll ->
// deterministic evaluation -> snapshot back to Unity. No stub involved.
#if QANIVA_HAS_CLINICAL_CORE
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Qaniva.Clinical.Runtime;
using Qaniva.Simulation.Core;

namespace Qaniva.Simulation.Tests
{
    public class RealClinicalRuntimeTests
    {
        private static string DemoCaseJson()
        {
            // The sync script copies the fixture into Resources; in EditMode read it
            // straight from Assets so the test also works before a Resources refresh.
            var path = Path.Combine(
                Application.dataPath, "Qaniva/Resources/Qaniva/Cases/demo_sync_bradycardia_001.json");
            Assert.IsTrue(File.Exists(path),
                $"Demo case not found at {path}. Run scripts/sync-clinical-core-to-unity.sh first.");
            return File.ReadAllText(path);
        }

        private static AttemptSummaryView RunIdealPath(ulong seed)
        {
            IClinicalRuntime runtime = new ClinicalRuntime();
            runtime.LoadCase(DemoCaseJson(), seed);
            var initial = runtime.Initialize();
            Assert.IsFalse(initial.IsTerminal);
            Assert.AreEqual(38, initial.Hr, 0.001, "initial HR must come from the case JSON");

            // Same ideal path as clinical-core/Qaniva.Clinical.Tests/Golden/ideal_path.script.json.
            var steps = new[]
            {
                "attach_monitor", "patient_history", "ecg_12lead", "iv_access",
                "give_atropine", "consult_cardiology", "disposition_ccu",
            };
            foreach (var actionId in steps)
            {
                var outcome = runtime.ApplyAction(actionId, new Dictionary<string, string>());
                Assert.IsTrue(outcome.Accepted, $"{actionId} rejected: {outcome.RejectionReason}");
            }

            Assert.IsTrue(runtime.IsTerminated, "ideal path must reach a terminal state");
            return runtime.BuildAttemptSummary();
        }

        [Test]
        public void RealEngineRunsTheDemoIdealPathToTheGoldenResult()
        {
            var summary = RunIdealPath(20260830UL);

            // Locked to the committed golden fixture
            // (clinical-core/Qaniva.Clinical.Tests/Golden/ideal_path.golden.json).
            Assert.AreEqual("complete", summary.TerminalOutcome);
            Assert.AreEqual(80d, summary.TotalScore, 0.0001);
            Assert.AreEqual(40d, summary.ScoreCritical, 0.0001);
            Assert.AreEqual(20d, summary.ScoreTiming, 0.0001);
            Assert.AreEqual(15d, summary.ScoreDisposition, 0.0001);
            Assert.AreEqual(7, summary.Timeline.Count);
            Assert.AreEqual("attach_monitor", summary.Timeline[0].ActionId);
            Assert.AreEqual(
                "fe2191ff684f062290385fd967b47ebb58ba46932f88cbcebcc98d483d24dfc5",
                summary.ReplayHash,
                "replay hash must match the committed golden — determinism across processes");
        }

        [Test]
        public void RealEngineIsDeterministicAcrossRuns()
        {
            var a = RunIdealPath(20260830UL);
            var b = RunIdealPath(20260830UL);
            Assert.AreEqual(a.ReplayHash, b.ReplayHash);
            Assert.AreEqual(a.TotalScore, b.TotalScore, 0.0001);
        }

        [Test]
        public void RealEngineRejectsAnInvalidActionWithoutStateChange()
        {
            IClinicalRuntime runtime = new ClinicalRuntime();
            runtime.LoadCase(DemoCaseJson(), 1UL);
            var initial = runtime.Initialize();

            // give_atropine requires iv_access.
            var outcome = runtime.ApplyAction("give_atropine", new Dictionary<string, string>());
            Assert.IsFalse(outcome.Accepted);
            Assert.AreEqual(initial.StateHash, outcome.Snapshot.StateHash,
                "a rejected action must leave engine state byte-identical");
        }

        [Test]
        public void BridgeControllerUsesTheRealEngineEndToEnd()
        {
            // Full in-Unity round trip over the REAL engine: START_SIMULATION in,
            // SIMULATION_READY + SIMULATION_COMPLETED out — StubClinicalRuntime not involved.
            var go = new GameObject("bridge-under-test");
            try
            {
                var controller = go.AddComponent<Qaniva.Bridge.SimulationBridgeController>();
                var fakeTransport = new Qaniva.Bridge.FakeUnityBridge();
                controller.Configure(
                    fakeTransport,
                    new ClinicalRuntime(),
                    new FileCaseProvider());

                fakeTransport.PushFromHost(Qaniva.Bridge.BridgeMessageCodec.Encode(
                    Qaniva.Bridge.BridgeProtocol.RnToUnity.StartSimulation,
                    new Qaniva.Bridge.StartSimulationPayload
                    {
                        caseId = "demo_sync_bradycardia_001",
                        caseVersion = 1,
                        attemptId = "22222222-2222-4222-8222-222222222222",
                        seed = 20260830,
                    }));

                foreach (var actionId in new[]
                         { "attach_monitor", "patient_history", "ecg_12lead", "iv_access",
                           "give_atropine", "consult_cardiology", "disposition_ccu" })
                {
                    var outcome = controller.SubmitPlayerAction(actionId);
                    Assert.IsTrue(outcome.Accepted, $"{actionId}: {outcome.RejectionReason}");
                }

                Assert.AreEqual(2, fakeTransport.Sent.Count, "expected READY + COMPLETED");
                var (readyType, _) = Qaniva.Bridge.BridgeMessageCodec.DecodeEnvelope(
                    fakeTransport.Sent[0], Qaniva.Bridge.BridgeMessageCodec.UnityToRnTypes);
                Assert.AreEqual(Qaniva.Bridge.BridgeProtocol.UnityToRn.SimulationReady, readyType);

                var (completedType, payload) = Qaniva.Bridge.BridgeMessageCodec.DecodeEnvelope(
                    fakeTransport.Sent[1], Qaniva.Bridge.BridgeMessageCodec.UnityToRnTypes);
                Assert.AreEqual(Qaniva.Bridge.BridgeProtocol.UnityToRn.SimulationCompleted, completedType);
                var completed = Qaniva.Bridge.BridgeMessageCodec
                    .DecodePayload<Qaniva.Bridge.SimulationCompletedPayload>(payload);
                Assert.AreEqual("complete", completed.summary.terminalState);
                Assert.AreEqual(80d, completed.summary.totalScore, 0.0001);
                Assert.AreEqual(
                    "fe2191ff684f062290385fd967b47ebb58ba46932f88cbcebcc98d483d24dfc5",
                    completed.summary.replayHash);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>EditMode-only provider reading the fixture from Assets directly.</summary>
        private sealed class FileCaseProvider : Qaniva.Bridge.ICaseProvider
        {
            public string GetCaseJson(string caseId, int caseVersion) => DemoCaseJson();
        }
    }
}
#endif
