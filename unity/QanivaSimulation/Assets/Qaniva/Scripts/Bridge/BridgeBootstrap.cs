using UnityEngine;

namespace Qaniva.Bridge
{
    /// <summary>
    /// Creates the bridge GameObject as soon as the Unity runtime starts, without
    /// requiring any scene wiring. The native host (iOS/Android) sends messages to
    /// the GameObject named <c>SimulationBridge</c> via
    /// <c>sendMessageToGO("SimulationBridge", "OnHostMessage", json)</c> /
    /// <c>UnitySendMessage</c>, so the NAME MUST NOT CHANGE without updating
    /// apps/mobile/modules/unity-host (kUnityBridgeGameObject).
    /// </summary>
    public static class BridgeBootstrap
    {
        public const string BridgeGameObjectName = "SimulationBridge";

        // BeforeSceneLoad so consumers using AfterSceneLoad (e.g. IntegrationHud)
        // can rely on the bridge GameObject already existing.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBridgeExists()
        {
            if (GameObject.Find(BridgeGameObjectName) != null)
            {
                return;
            }

            var go = new GameObject(BridgeGameObjectName);
            Object.DontDestroyOnLoad(go);

            var native = go.AddComponent<NativeUnityBridge>();
            var controller = go.AddComponent<SimulationBridgeController>();
            controller.Configure(native, CreateRuntime(), new ResourcesCaseProvider());

            Debug.Log("[Qaniva] BridgeBootstrap created SimulationBridge (runtime: "
                + RuntimeKind + ")");
        }

        private static Simulation.Core.IClinicalRuntime CreateRuntime()
        {
#if QANIVA_HAS_CLINICAL_CORE
            return new Qaniva.Clinical.Runtime.ClinicalRuntime();
#else
            Debug.LogWarning(
                "[Qaniva] QANIVA_HAS_CLINICAL_CORE is not defined — using StubClinicalRuntime. "
                + "Run scripts/sync-clinical-core-to-unity.sh and set the scripting define.");
            return new StubClinicalRuntime();
#endif
        }

        public static string RuntimeKind =>
#if QANIVA_HAS_CLINICAL_CORE
            "ClinicalRuntime (real deterministic engine)";
#else
            "StubClinicalRuntime (DEV STAND-IN)";
#endif
    }
}
