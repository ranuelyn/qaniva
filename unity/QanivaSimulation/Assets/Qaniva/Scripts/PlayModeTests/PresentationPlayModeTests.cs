// PlayMode verification of the 3D presentation foundation (QAN-002):
// the REAL EnvironmentBootstrap composes room/patient/monitor from the case's
// presentation profile, canonical snapshots drive the monitor and the patient
// visual state, and a warm-runtime relaunch stays clean.
//
// When the environment variable QANIVA_CAPTURE_DIR is set, the tests also dump
// portrait PNG captures of the composed scene (play mode = the real URP path),
// which serve as the composition-iteration previews.
#if QANIVA_HAS_CLINICAL_CORE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Qaniva.Bridge;
using Qaniva.Clinical.Runtime;
using Qaniva.Presentation;

namespace Qaniva.Simulation.PlayModeTests
{
    public class PresentationPlayModeTests
    {
        private sealed class FileCaseProvider : ICaseProvider
        {
            public string GetCaseJson(string caseId, int caseVersion) => File.ReadAllText(Path.Combine(
                Application.dataPath, "Qaniva/Resources/Qaniva/Cases/demo_sync_bradycardia_001.json"));
        }

        private GameObject _go;
        private SimulationBridgeController _controller;
        private EnvironmentBootstrap _env;
        private FakeUnityBridge _bridge;

        private void CreateRig()
        {
            _go = new GameObject("presentation-under-test");
            _controller = _go.AddComponent<SimulationBridgeController>();
            _bridge = new FakeUnityBridge();
            _controller.Configure(_bridge, new ClinicalRuntime(), new FileCaseProvider());
            _env = _go.AddComponent<EnvironmentBootstrap>();
        }

        private void Start(string attemptId)
        {
            _bridge.PushFromHost(BridgeMessageCodec.Encode(
                BridgeProtocol.RnToUnity.StartSimulation,
                new StartSimulationPayload
                {
                    caseId = "demo_sync_bradycardia_001",
                    caseVersion = 1,
                    attemptId = attemptId,
                    seed = 20260830,
                    mode = BridgeProtocol.Modes.Interactive,
                }));
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }
            foreach (var leftover in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (leftover != null && leftover.name.StartsWith("Environment_"))
                {
                    UnityEngine.Object.DestroyImmediate(leftover);
                }
            }
        }

        private static void Capture(GameObject envInstance, string name)
        {
            var dir = Environment.GetEnvironmentVariable("QANIVA_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir))
            {
                return;
            }
            var cam = envInstance.GetComponentInChildren<Camera>();
            var rt = new RenderTexture(1206, 2622, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            // Linear color space: RT holds linear values; convert to sRGB for PNG.
            var px = tex.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                px[i] = px[i].gamma;
            }
            tex.SetPixels(px);
            tex.Apply();
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name), tex.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(tex);
        }

        [UnityTest]
        public IEnumerator BootstrapComposesRoomPatientAndMonitorFromTheCaseProfile()
        {
            CreateRig();
            Start("44444444-4444-4444-8444-444444444401");
            yield return null;

            Assert.IsNotNull(_env.EnvironmentInstance, "environment must be instantiated from roomKey ed_resus_v1");
            Assert.IsNotNull(_env.Patient, "patient must be instantiated from patientVariant adult_neutral_v1");
            Assert.IsNotNull(_env.Monitor, "bedside monitor must be found in the environment");
            Assert.IsNotNull(_env.EnvironmentInstance.transform.Find("PatientAnchor"), "anchor resolved");

            // Monitor shows the canonical initial vitals (engine truth, not UI copies).
            var hr = _env.EnvironmentInstance.transform.GetComponentInChildren<BedsideMonitorView>()
                .transform.Find("HrValue").GetComponent<TextMesh>();
            Assert.AreEqual("38", hr.text, "monitor HR must equal the canonical initial snapshot");

            // Initial canonical state (poor_perfusion) maps to Distressed.
            Assert.AreEqual(PatientVisualState.Distressed, _env.Patient.CurrentState);

            yield return null; // let breathing Update run once
            Capture(_env.EnvironmentInstance, "scene-initial-distressed.png");
        }

        [UnityTest]
        public IEnumerator CanonicalStateChangeUpdatesPatientAndMonitorPresentation()
        {
            CreateRig();
            Start("44444444-4444-4444-8444-444444444402");
            yield return null;

            // Golden path through the runtime: iv_access then atropine normalises
            // circulation — canonical change, engine-owned.
            _controller.SubmitPlayerAction("iv_access", new Dictionary<string, string>());
            _controller.SubmitPlayerAction("give_atropine", new Dictionary<string, string>());
            yield return null;

            Assert.AreEqual(PatientVisualState.Normal, _env.Patient.CurrentState,
                "canonical circulation normal must map to the Normal visual state");
            var hr = _env.Monitor.transform.Find("HrValue").GetComponent<TextMesh>();
            Assert.AreEqual("68", hr.text, "monitor must show the canonical post-atropine HR");

            Capture(_env.EnvironmentInstance, "scene-after-treatment-normal.png");
        }

        [UnityTest]
        public IEnumerator SameSnapshotAlwaysMapsToTheSameVisualState()
        {
            CreateRig();
            Start("44444444-4444-4444-8444-444444444403");
            yield return null;

            var snapshot = _controller.CurrentSnapshot;
            var a = PatientPresentationMapper.Map(snapshot);
            var b = PatientPresentationMapper.Map(snapshot);
            Assert.AreEqual(a, b, "mapping must be deterministic");
            Assert.AreEqual("poor_perfusion", snapshot.Circulation,
                "mapping must not mutate the snapshot");
        }

        [UnityTest]
        public IEnumerator WarmRelaunchReusesTheRoomAndResetsPatientPresentation()
        {
            CreateRig();
            Start("44444444-4444-4444-8444-444444444404");
            yield return null;

            // Drive to a non-initial visual state.
            _controller.SubmitPlayerAction("iv_access", new Dictionary<string, string>());
            _controller.SubmitPlayerAction("give_atropine", new Dictionary<string, string>());
            yield return null;
            Assert.AreEqual(PatientVisualState.Normal, _env.Patient.CurrentState);
            var firstEnv = _env.EnvironmentInstance;

            // Relaunch (new attempt) on the warm runtime.
            Start("44444444-4444-4444-8444-444444444405");
            yield return null;

            Assert.AreSame(firstEnv, _env.EnvironmentInstance,
                "same roomKey must reuse the environment instance (no duplicate rooms)");
            Assert.AreEqual(1, GameObject.FindObjectsByType<PatientVisualController>(FindObjectsSortMode.None).Length,
                "exactly one patient after relaunch");
            Assert.AreEqual(PatientVisualState.Distressed, _env.Patient.CurrentState,
                "fresh attempt must re-derive the visual state from the fresh canonical snapshot");
            var hr = _env.Monitor.transform.Find("HrValue").GetComponent<TextMesh>();
            Assert.AreEqual("38", hr.text, "monitor must reset to the fresh canonical vitals");
        }

        [UnityTest]
        public IEnumerator UnknownRoomKeyFailsLoudlyWithoutComposing()
        {
            CreateRig();
            LogAssert.Expect(LogType.Error,
                "[EnvironmentBootstrap] unknown roomKey \"no_such_room\" — no environment loaded");
            Assert.IsNull(PresentationRegistry.ResolveEnvironment("no_such_room"));

            // Simulate a profile with an unknown room via the registry contract:
            // resolution is null and the bootstrap reports it (verified through the
            // log expectation when a start with such a case would arrive).
            var method = typeof(EnvironmentBootstrap).GetMethod(
                "ComposeEnvironment",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_env, new object[]
            {
                new Simulation.Core.PresentationProfileView { RoomKey = "no_such_room", PatientVariant = "x" },
            });
            Assert.IsNull(_env.EnvironmentInstance, "nothing may be composed for an unknown room");
            yield return null;
        }
    }
}
#endif
