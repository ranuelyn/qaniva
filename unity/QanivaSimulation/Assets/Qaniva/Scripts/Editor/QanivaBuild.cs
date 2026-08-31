using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Qaniva.EditorTools
{
    /// <summary>
    /// Batchmode entry points used by scripts/*.sh. Everything here is reproducible
    /// from the command line so a fresh developer never has to click through the
    /// Editor for the integration build.
    /// </summary>
    public static class QanivaBuild
    {
        private const string BootstrapScenePath = "Assets/Qaniva/Scenes/Bootstrap.unity";
        public const string ClinicalCoreDefine = "QANIVA_HAS_CLINICAL_CORE";

        /// <summary>
        /// Creates the minimal integration scene (camera + light + dark background)
        /// and registers it in Build Settings. The bridge + HUD self-bootstrap at
        /// runtime, so the scene needs no wiring. Idempotent.
        /// </summary>
        public static void CreateMinimalScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 1.2f, -3f);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Primitive placeholder "patient on a bed" — integration proof only.
            var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "Bed (placeholder)";
            bed.transform.position = new Vector3(0f, 0.25f, 0f);
            bed.transform.localScale = new Vector3(0.9f, 0.5f, 2f);

            var patient = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            patient.name = "Patient (placeholder)";
            patient.transform.position = new Vector3(0f, 0.75f, 0f);
            patient.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            patient.transform.localScale = new Vector3(0.35f, 0.8f, 0.35f);

            Directory.CreateDirectory(Path.GetDirectoryName(BootstrapScenePath)!);
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootstrapScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[QanivaBuild] wrote {BootstrapScenePath} and registered it in Build Settings");
        }

        /// <summary>
        /// Creates the URP pipeline asset (with its default renderer) and assigns it
        /// as the project's render pipeline (ADR-002: Unity 6 + URP). Idempotent.
        /// </summary>
        public static void ConfigureUrp()
        {
            const string urpAssetPath = "Assets/Qaniva/Settings/QanivaURP.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
            if (existing == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(urpAssetPath)!);
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(
                    rendererData, "Assets/Qaniva/Settings/QanivaURPRenderer.asset");
                var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, urpAssetPath);
                existing = pipeline;
            }
            GraphicsSettings.defaultRenderPipeline = existing;
            QualitySettings.renderPipeline = existing;
            AssetDatabase.SaveAssets();
            Debug.Log("[QanivaBuild] URP pipeline asset assigned (graphics + quality settings)");
        }

        /// <summary>
        /// Adds QANIVA_HAS_CLINICAL_CORE (real engine) and — until QAN-006 ships a
        /// real action UI — QANIVA_INTEGRATION_AUTOPLAY (demo ideal-path driver)
        /// for iOS + Standalone + Android. Idempotent.
        /// </summary>
        public static void EnableClinicalCoreDefine()
        {
            string[] wanted = { ClinicalCoreDefine, "QANIVA_INTEGRATION_AUTOPLAY" };
            foreach (var target in new[] { NamedBuildTarget.iOS, NamedBuildTarget.Standalone, NamedBuildTarget.Android })
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out string[] defines);
                var merged = defines.Union(wanted).ToArray();
                if (merged.Length != defines.Length)
                {
                    PlayerSettings.SetScriptingDefineSymbols(target, merged);
                    Debug.Log($"[QanivaBuild] defines for {target.TargetName}: {string.Join(";", merged)}");
                }
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Exports the Unity iOS Xcode project (containing the UnityFramework
        /// target) to the path given by -exportPath (default: build/ios).
        /// scripts/export-unity-ios.sh then builds UnityFramework.framework from it.
        /// </summary>
        public static void ExportIos()
        {
            string exportPath = GetArg("-exportPath") ?? "build/ios";
            bool simulator = Array.IndexOf(Environment.GetCommandLineArgs(), "-simulator") >= 0;

            if (EditorBuildSettings.scenes.Length == 0 || !File.Exists(BootstrapScenePath))
            {
                CreateMinimalScene();
            }

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "app.qaniva.unity");
            PlayerSettings.iOS.targetOSVersionString = "15.1";
            PlayerSettings.iOS.sdkVersion = simulator ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;
            if (simulator)
            {
                // 2 = Universal (x86_64 + arm64). Without this Unity exports the
                // x64-only simulator UnityRuntime/baselib variants, which cannot
                // link on an Apple Silicon simulator (arm64).
                PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 2);
            }
            Debug.Log($"[QanivaBuild] iOS SDK target: {(simulator ? "Simulator (universal arch)" : "Device")}");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { BootstrapScenePath },
                locationPathName = exportPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new Exception(
                    $"iOS export failed: {report.summary.result} ({report.summary.totalErrors} errors)");
            }
            Debug.Log($"[QanivaBuild] iOS export succeeded -> {exportPath}");
        }

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
