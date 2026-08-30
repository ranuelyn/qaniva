using System;

namespace Qaniva.Bridge
{
    /// <summary>
    /// Transport abstraction between the native host (React Native) and Unity.
    /// The controller talks to THIS, not to platform APIs, so it is testable in
    /// the Editor with <see cref="FakeUnityBridge"/>.
    /// </summary>
    public interface IUnityBridge
    {
        /// <summary>Raised with the raw JSON of every message received FROM React Native.</summary>
        event Action<string> MessageReceived;

        /// <summary>Send raw JSON to React Native.</summary>
        void SendToHost(string json);
    }
}
