using System;
using System.Collections.Generic;

namespace Qaniva.Bridge
{
    /// <summary>
    /// Typed, versioned envelope for every RN &lt;-&gt; Unity message. Mirrors
    /// packages/contracts/src/bridge-messages.ts. Keep the two in sync (the parity
    /// test packages/contracts/src/__tests__/csharp-parity.test.ts guards the
    /// constants; payload shapes are reviewed by hand).
    /// </summary>
    [Serializable]
    public sealed class BridgeEnvelope
    {
        public int protocolVersion;
        public string type;
        public string messageId;
        public string sentAt;

        public static BridgeEnvelope Create(string type)
        {
            return new BridgeEnvelope
            {
                protocolVersion = BridgeProtocol.ProtocolVersion,
                type = type,
                messageId = Guid.NewGuid().ToString("D"),
                sentAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            };
        }
    }

    // --- RN -> Unity payloads ---------------------------------------------

    [Serializable]
    public sealed class StartSimulationPayload
    {
        public string caseId;
        public int caseVersion;
        public string attemptId;
        public string locale = "en";
        public string difficulty = "standard";
        public long seed;
        public string mode = BridgeProtocol.Modes.Interactive;
    }

    [Serializable]
    public sealed class ExitSimulationPayload
    {
        public string reason = "user_quit";
    }

    // --- Unity -> RN payloads -------------------------------------------

    [Serializable]
    public sealed class SimulationReadyPayload
    {
        public string caseId;
        public string attemptId;
        public double warmupSec;
    }

    [Serializable]
    public sealed class SimulationCompletedPayload
    {
        public string attemptId;
        public AttemptSummaryDto summary;
    }

    [Serializable]
    public sealed class SimulationFailedPayload
    {
        public string attemptId;
        public string code = BridgeProtocol.FailureCodes.Unknown;
        public string message = "";
    }

    [Serializable]
    public sealed class ExitRequestedPayload
    {
        public string attemptId;
        public string reason = "user_quit";
    }

    [Serializable]
    public sealed class AttemptSummaryDto
    {
        public string attemptId;
        public string caseId;
        public int caseVersion;
        public long seed;
        public string startedAt;
        public string completedAt;
        public string terminalState = "aborted";
        public double totalScore;
        public ScoreBreakdownDto scoreBreakdown = new ScoreBreakdownDto();
        public List<TimelineEntryDto> timeline = new List<TimelineEntryDto>();
        public string replayHash = "";
    }

    [Serializable]
    public sealed class ScoreBreakdownDto
    {
        public double critical;
        public double timing;
        public double efficiency;
        public double treatment;
        public double disposition;
    }

    [Serializable]
    public sealed class TimelineEntryDto
    {
        public int seq;
        public double simTimeSec;
        public string actionId = "";
        public string label = "";
        public string classification = "neutral";
    }
}
