// -----------------------------------------------------------------------------
// C# MIRROR of the RN <-> Unity bridge protocol.
// The SINGLE SOURCE OF TRUTH is packages/contracts/src/protocol.ts.
// The test packages/contracts/src/__tests__/csharp-parity.test.ts fails CI if
// this file drifts from it. When you change the protocol:
//   1. edit packages/contracts/src/protocol.ts
//   2. mirror the change here
//   3. bump ProtocolVersion on BOTH sides for any breaking change
// -----------------------------------------------------------------------------

namespace Qaniva.Bridge
{
    public static class BridgeProtocol
    {
        /// <summary>Bump on ANY breaking change to an envelope or payload shape.</summary>
        public const int ProtocolVersion = 1;

        // Messages sent from React Native into the Unity simulation runtime.
        public static class RnToUnity
        {
            public const string StartSimulation = "START_SIMULATION";
            public const string PauseSimulation = "PAUSE_SIMULATION";
            public const string ResumeSimulation = "RESUME_SIMULATION";
            public const string ExitSimulation = "EXIT_SIMULATION";
        }

        // Messages emitted by the Unity simulation runtime back to React Native.
        public static class UnityToRn
        {
            public const string SimulationReady = "SIMULATION_READY";
            public const string SimulationCompleted = "SIMULATION_COMPLETED";
            public const string SimulationFailed = "SIMULATION_FAILED";
            public const string ExitRequested = "EXIT_REQUESTED";
        }

        // Runtime modes carried in START_SIMULATION.payload.mode.
        // Production always sends "interactive"; the e2e modes drive automated
        // verification and are inert without QANIVA_INTEGRATION_AUTOPLAY.
        public static class Modes
        {
            public const string Interactive = "interactive";
            public const string E2eAutoplay = "e2e_autoplay";
            public const string E2eUi = "e2e_ui";
        }

        // Stable failure codes for SIMULATION_FAILED.
        public static class FailureCodes
        {
            public const string CaseLoadFailed = "CASE_LOAD_FAILED";
            public const string CaseVersionMismatch = "CASE_VERSION_MISMATCH";
            public const string EngineError = "ENGINE_ERROR";
            public const string RenderError = "RENDER_ERROR";
            public const string BridgeProtocolError = "BRIDGE_PROTOCOL_ERROR";
            public const string Unknown = "UNKNOWN";
        }
    }
}
