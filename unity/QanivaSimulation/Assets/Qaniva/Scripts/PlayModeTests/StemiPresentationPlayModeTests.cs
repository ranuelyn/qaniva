// PlayMode verification of the STEMI MVP case inside the REAL presentation
// stack: rigged patient composition, canonical monitor values, the generic
// result viewer (ECG asset), the interactive UI walking the real case to its
// terminal state, and a clean warm relaunch. QANIVA_CAPTURE_DIR dumps portrait
// captures like PresentationPlayModeTests.
#if QANIVA_HAS_CLINICAL_CORE
using System;
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
    public class StemiPresentationPlayModeTests
    {
        // Mirrors clinical-core/.../Golden/stemi_ideal_path.script.json (test-assembly
        // copy — internals of Qaniva.Presentation are not visible here by design).
        private static readonly string[] StemiIdealPath =
        {
            "focused_history", "attach_monitor", "ecg_12lead", "give_aspirin",
            "iv_access", "give_ticagrelor", "give_heparin_ufh",
            "activate_cath_lab", "start_statin", "disposition_cath_lab",
        };

        private sealed class FileCaseProvider : ICaseProvider
        {
            public string GetCaseJson(string caseId, int caseVersion) => File.ReadAllText(Path.Combine(
                Application.dataPath, $"Qaniva/Resources/Qaniva/Cases/{caseId}.json"));
        }

        private GameObject _go;
        private SimulationBridgeController _controller;
        private EnvironmentBootstrap _env;
        private FakeUnityBridge _bridge;

        private void CreateRig()
        {
            _go = new GameObject("stemi-presentation-under-test");
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
                    caseId = "stemi_anterior_001",
                    caseVersion = 1,
                    attemptId = attemptId,
                    seed = 20260831,
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
            var px = tex.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                px[i] = px[i].gamma; // linear -> sRGB for PNG
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
        public IEnumerator StemiCaseComposesTheRiggedPatientInTheSharedRoom()
        {
            CreateRig();
            Start("55555555-5555-4555-8555-555555555501");
            yield return null;

            Assert.IsNotNull(_env.EnvironmentInstance, "shared ed_resus_v1 room must load for the STEMI case");
            Assert.IsNotNull(_env.Patient, "adult_rigged_v1 must be instantiated from the profile");
            Assert.IsNotNull(_env.Patient.GetComponentInChildren<SkinnedMeshRenderer>(),
                "the STEMI patient must be the RIGGED model (skinned mesh), not primitives");

            // Monitor shows the canonical initial vitals of THIS case.
            var hr = _env.Monitor.transform.Find("HrValue").GetComponent<TextMesh>();
            Assert.AreEqual("96", hr.text, "monitor HR must equal the STEMI initial snapshot");
            Assert.AreEqual(PatientVisualState.Distressed, _env.Patient.CurrentState,
                "initial poor_perfusion maps to Distressed");

            yield return null;
            Capture(_env.EnvironmentInstance, "stemi-initial-rigged.png");
        }

        [UnityTest]
        public IEnumerator EcgResultOpensTheViewerWithTheTracingAsset()
        {
            CreateRig();
            _go.AddComponent<SimulationUiController>().Bind(_controller);
            Start("55555555-5555-4555-8555-555555555502");
            yield return null;

            var ui = _go.GetComponent<SimulationUiController>();
            var root = ui.Root;
            Assert.IsNotNull(root, "UI root must build");

            // Through the real UI path (Submit -> engine -> RenderOutcome), which
            // is what auto-opens the viewer for asset-bearing results.
            ui.Submit("ecg_12lead");
            yield return null;

            var viewer = root.Q<VisualElement>("result-viewer");
            Assert.IsFalse(viewer.ClassListContains("hidden"), "the ECG result must open the viewer");
            Assert.AreEqual("12-lead ECG", root.Q<Label>("result-viewer-title").text);
            Assert.IsNotNull(Resources.Load<Texture2D>(
                ResultViewerPresenter.AssetResourceFolder + "ecg_stemi_anterior_v1"),
                "the bundled tracing asset must resolve");

            // Close through the real button; the sim continues.
            using (var evt = NavigationSubmitEvent.GetPooled())
            {
                var close = root.Q<Button>("result-viewer-close");
                evt.target = close;
                close.SendEvent(evt);
            }
            yield return null;
            Assert.IsTrue(viewer.ClassListContains("hidden"), "close must hide the viewer");
        }

        [UnityTest]
        public IEnumerator IdealPathThroughTheRuntimeReachesTheCathLabHandoff()
        {
            CreateRig();
            Start("55555555-5555-4555-8555-555555555503");
            yield return null;

            foreach (var actionId in StemiIdealPath)
            {
                var outcome = _controller.SubmitPlayerAction(actionId, new Dictionary<string, string>());
                Assert.IsTrue(outcome.Accepted, $"{actionId} must be accepted on the ideal path");
            }

            Assert.IsTrue(_controller.CurrentSnapshot.IsTerminal);
            Assert.AreEqual("complete", _controller.CurrentSnapshot.TerminalOutcome);

            SimulationCompletedPayload completed = null;
            foreach (var json in _bridge.Sent)
            {
                if (json.Contains(BridgeProtocol.UnityToRn.SimulationCompleted))
                {
                    var (_, payload) = BridgeMessageCodec.DecodeEnvelope(
                        json, BridgeMessageCodec.UnityToRnTypes);
                    completed = BridgeMessageCodec.DecodePayload<SimulationCompletedPayload>(payload);
                }
            }
            Assert.IsNotNull(completed, "SIMULATION_COMPLETED must be emitted");
            var summary = completed.summary;
            Assert.AreEqual(88, summary.totalScore, 0.001, "ideal path earns the full 88");
            Assert.IsTrue(summary.criteria.Count > 0, "per-criterion debrief data must ship to RN");
            Assert.IsTrue(summary.debrief.keyTeachingPoints.Count > 0, "case debrief metadata must ship to RN");

            Capture(_env.EnvironmentInstance, "stemi-completed.png");
        }

        [UnityTest]
        public IEnumerator WarmRelaunchResetsTheStemiPresentation()
        {
            CreateRig();
            Start("55555555-5555-4555-8555-555555555504");
            yield return null;
            _controller.SubmitPlayerAction("ecg_12lead", new Dictionary<string, string>());
            yield return null;

            Start("55555555-5555-4555-8555-555555555505");
            yield return null;

            Assert.AreEqual(1,
                GameObject.FindObjectsByType<PatientVisualController>(FindObjectsSortMode.None).Length,
                "exactly one patient after relaunch");
            var hr = _env.Monitor.transform.Find("HrValue").GetComponent<TextMesh>();
            Assert.AreEqual("96", hr.text, "monitor resets to the fresh canonical vitals");
            Assert.AreEqual(PatientVisualState.Distressed, _env.Patient.CurrentState);
        }
    }
}
#endif
