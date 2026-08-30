using System;
using UnityEngine;

namespace Qaniva.Bridge
{
    /// <summary>
    /// SPIKE STATUS — real transport to the React Native host via "Unity as a Library".
    ///
    /// This class is the single integration seam for the RN &lt;-&gt; Unity embed. The
    /// architecture (contract, codec, controller, fake bridge, tests) is complete and
    /// proven in the Editor; wiring the platform channels below is issue QAN-004.
    ///
    /// iOS   : the host app calls `UnityFramework`'s `sendMessageToGOWithName:...`
    ///         to reach `SimulationBridgeController.OnHostMessage`. Unity -&gt; host
    ///         goes through an exported native function (see manual steps in
    ///         unity/QanivaSimulation/README.md).
    /// Android: the host `UnityPlayer` receives via `UnitySendMessage`; Unity -&gt; host
    ///         calls a registered `AndroidJavaProxy` / plugin method.
    ///
    /// Until QAN-004 lands, the running app uses <see cref="FakeUnityBridge"/> or a
    /// scene-injected bridge; nothing here pretends the native path is live.
    /// </summary>
    public sealed class NativeUnityBridge : MonoBehaviour, IUnityBridge
    {
        private static NativeUnityBridge _instance;

        public event Action<string> MessageReceived;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Entry point the native host invokes (iOS: sendMessageToGO, Android: UnitySendMessage).
        /// </summary>
        public void OnHostMessage(string json)
        {
            MessageReceived?.Invoke(json);
        }

        public void SendToHost(string json)
        {
#if UNITY_IOS && !UNITY_EDITOR
            _QanivaBridge_SendToHost(json);
#elif UNITY_ANDROID && !UNITY_EDITOR
            SendToHostAndroid(json);
#else
            Debug.Log($"[NativeUnityBridge] (no native host in this context) -> {json}");
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _QanivaBridge_SendToHost(string json);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void SendToHostAndroid(string json)
        {
            using (var bridgeClass = new AndroidJavaClass("app.qaniva.unitybridge.QanivaBridgePlugin"))
            {
                bridgeClass.CallStatic("sendToHost", json);
            }
        }
#endif
    }
}
