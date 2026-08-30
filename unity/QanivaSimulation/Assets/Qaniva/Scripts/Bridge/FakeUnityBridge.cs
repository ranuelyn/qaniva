using System;
using System.Collections.Generic;

namespace Qaniva.Bridge
{
    /// <summary>
    /// In-memory loopback bridge for Editor play and EditMode tests. A test pushes
    /// RN-&gt;Unity messages with <see cref="PushFromHost"/> and inspects everything
    /// Unity sent back via <see cref="Sent"/>.
    /// </summary>
    public sealed class FakeUnityBridge : IUnityBridge
    {
        private readonly List<string> _sent = new List<string>();

        public event Action<string> MessageReceived;

        public IReadOnlyList<string> Sent => _sent;

        public void SendToHost(string json) => _sent.Add(json);

        public void PushFromHost(string json) => MessageReceived?.Invoke(json);

        public void Clear() => _sent.Clear();
    }
}
