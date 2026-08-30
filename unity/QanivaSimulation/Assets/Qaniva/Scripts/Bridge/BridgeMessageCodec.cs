using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Qaniva.Bridge
{
    public sealed class BridgeProtocolException : Exception
    {
        public BridgeProtocolException(string message) : base(message) { }
    }

    /// <summary>
    /// Encodes/decodes versioned bridge messages. Rejects anything whose
    /// protocolVersion does not match <see cref="BridgeProtocol.ProtocolVersion"/>
    /// or whose type is not on the expected channel.
    /// </summary>
    public static class BridgeMessageCodec
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include,
            Formatting = Formatting.None,
        };

        public static string Encode<TPayload>(string type, TPayload payload)
        {
            var envelope = JObject.FromObject(BridgeEnvelope.Create(type));
            envelope["payload"] = payload == null ? new JObject() : JToken.FromObject(payload);
            return envelope.ToString(Formatting.None);
        }

        /// <summary>Parse the envelope, verifying the protocol version and channel.</summary>
        public static (string Type, JObject Payload) DecodeEnvelope(string json, string[] expectedTypes)
        {
            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new BridgeProtocolException($"Bridge payload is not valid JSON: {ex.Message}");
            }

            var version = root.Value<int?>("protocolVersion");
            if (version != BridgeProtocol.ProtocolVersion)
            {
                throw new BridgeProtocolException(
                    $"Unsupported protocolVersion {version} (this runtime speaks {BridgeProtocol.ProtocolVersion}).");
            }

            var type = root.Value<string>("type");
            if (string.IsNullOrEmpty(type) || Array.IndexOf(expectedTypes, type) < 0)
            {
                throw new BridgeProtocolException($"Unexpected message type \"{type}\" on this channel.");
            }

            var payload = root["payload"] as JObject ?? new JObject();
            return (type, payload);
        }

        public static TPayload DecodePayload<TPayload>(JObject payload)
        {
            return payload.ToObject<TPayload>() ?? throw new BridgeProtocolException("Empty payload.");
        }

        public static readonly string[] RnToUnityTypes =
        {
            BridgeProtocol.RnToUnity.StartSimulation,
            BridgeProtocol.RnToUnity.PauseSimulation,
            BridgeProtocol.RnToUnity.ResumeSimulation,
            BridgeProtocol.RnToUnity.ExitSimulation,
        };

        public static readonly string[] UnityToRnTypes =
        {
            BridgeProtocol.UnityToRn.SimulationReady,
            BridgeProtocol.UnityToRn.SimulationCompleted,
            BridgeProtocol.UnityToRn.SimulationFailed,
            BridgeProtocol.UnityToRn.ExitRequested,
        };
    }
}
