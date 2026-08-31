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
        /// Creates the Bootstrap scene and registers it in Build Settings. The scene
        /// is intentionally near-empty: the ED environment (room, lights, composed
        /// camera) comes from the case-selected environment prefab at runtime
        /// (EnvironmentBootstrap); here we only set hospital-appropriate ambient
        /// lighting and a dark fallback camera (disabled once the environment
        /// camera spawns; keeps rendering honest if a presentation profile fails).
        /// Idempotent.
        /// </summary>
        public static void CreateMinimalScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.32f, 0.35f);

            var cameraGo = new GameObject("FallbackCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 1.2f, -3f);

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

            // Mobile presentation profile (evidence-based, not blanket-disabled):
            // one soft-shadowed key light close to the bed => short shadow distance
            // + modest map; HDR off (no HDR content). MSAA is OFF for now: with
            // this URP version the simulator Metal path floods
            // "RenderPass: Attachment 0 was created with 1 samples but 4 samples
            // were requested" every frame when MSAA>1 (observed live on the
            // iPhone 16 Pro simulator) — re-enable after verifying on device.
            existing.shadowDistance = 12f;
            existing.mainLightShadowmapResolution = 1024;
            existing.msaaSampleCount = 1;
            existing.supportsHDR = false;
            existing.renderScale = 1.0f;
            EditorUtility.SetDirty(existing);

            AssetDatabase.SaveAssets();
            Debug.Log("[QanivaBuild] URP pipeline asset assigned + mobile profile applied");
        }

        /// <summary>
        /// Creates the runtime PanelSettings asset for the UI Toolkit simulation UI
        /// (uxml/uss/tss are committed text files; PanelSettings must be an asset).
        /// Idempotent.
        /// </summary>
        public static void CreateUiAssets()
        {
            const string panelPath = "Assets/Qaniva/Resources/Qaniva/UI/QanivaPanelSettings.asset";
            var panel = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(panelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
                AssetDatabase.CreateAsset(panel, panelPath);
            }
            var theme = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.ThemeStyleSheet>(
                "Assets/Qaniva/Resources/Qaniva/UI/QanivaTheme.tss");
            if (theme == null)
            {
                throw new Exception("QanivaTheme.tss not imported — is the UI folder present?");
            }
            panel.themeStyleSheet = theme;
            panel.scaleMode = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1206, 2622); // iPhone 16 Pro native
            panel.match = 0.5f;
            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            Debug.Log("[QanivaBuild] PanelSettings ready at " + panelPath);
        }

        /// <summary>
        /// Adds QANIVA_HAS_CLINICAL_CORE (real engine) and QANIVA_INTEGRATION_AUTOPLAY
        /// (compiles the mode-gated e2e drivers; inert unless the host sends an e2e
        /// mode) for iOS + Standalone + Android. Idempotent.
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
