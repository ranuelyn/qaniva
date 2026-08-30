import {
  PROTOCOL_VERSION,
  decodeRnToUnity,
  encodeMessage,
  type AttemptSummary,
  type RnToUnityMessage,
  type UnityToRnMessage,
} from '@qaniva/contracts';

/**
 * Transport contract the RN `UnityHostScreen` talks to. The real implementation
 * bridges to the embedded Unity view (QAN-004); `FakeUnityBridge` below is a
 * deterministic stand-in that lets the whole RN flow — Home -> Case -> Briefing ->
 * START_SIMULATION -> SIMULATION_COMPLETED -> Results — run and be tested today.
 */
export interface UnityBridgeTransport {
  send(message: RnToUnityMessage): void;
  subscribe(handler: (message: UnityToRnMessage) => void): () => void;
  dispose(): void;
}

let counter = 0;
function messageId(): string {
  counter += 1;
  // Deterministic, RFC-4122-shaped id so contract validation passes in tests.
  const n = counter.toString(16).padStart(12, '0');
  return `00000000-0000-4000-8000-${n}`;
}

/** Pure: same attempt inputs always yield the same summary. */
export function buildDeterministicSummary(input: {
  attemptId: string;
  caseId: string;
  caseVersion: number;
  seed: number;
}): AttemptSummary {
  const base = (input.seed % 20) + 70; // 70..89, stable per seed
  return {
    attemptId: input.attemptId,
    caseId: input.caseId,
    caseVersion: input.caseVersion,
    seed: input.seed,
    startedAt: '2026-08-30T10:00:00.000Z',
    completedAt: '2026-08-30T10:08:00.000Z',
    terminalState: 'complete',
    totalScore: base,
    scoreBreakdown: { critical: 40, timing: base - 60, efficiency: 0, treatment: 5, disposition: 15 },
    timeline: [
      {
        seq: 0,
        simTimeSec: 20,
        actionId: 'attach_monitor',
        label: 'Attach cardiac monitor',
        classification: 'correct',
      },
      {
        seq: 1,
        simTimeSec: 110,
        actionId: 'ecg_12lead',
        label: '12-lead ECG',
        classification: 'correct',
      },
    ],
    replayHash: `fake-${input.caseId}-${input.seed}`,
  };
}

export class FakeUnityBridge implements UnityBridgeTransport {
  private readonly handlers = new Set<(m: UnityToRnMessage) => void>();
  private timers: ReturnType<typeof setTimeout>[] = [];

  send(message: RnToUnityMessage): void {
    // Round-trips through the real contract validator, exactly like the native path.
    const decoded = decodeRnToUnity(encodeMessage(message));

    if (decoded.type === 'START_SIMULATION') {
      const { caseId, caseVersion, attemptId, seed } = decoded.payload;
      this.later(() =>
        this.emit({
          protocolVersion: PROTOCOL_VERSION,
          messageId: messageId(),
          sentAt: new Date().toISOString(),
          type: 'SIMULATION_READY',
          payload: { caseId, attemptId, warmupSec: 0.4 },
        }),
      );
      this.later(() =>
        this.emit({
          protocolVersion: PROTOCOL_VERSION,
          messageId: messageId(),
          sentAt: new Date().toISOString(),
          type: 'SIMULATION_COMPLETED',
          payload: {
            attemptId,
            summary: buildDeterministicSummary({ attemptId, caseId, caseVersion, seed }),
          },
        }),
      );
    }

    if (decoded.type === 'EXIT_SIMULATION') {
      this.later(() =>
        this.emit({
          protocolVersion: PROTOCOL_VERSION,
          messageId: messageId(),
          sentAt: new Date().toISOString(),
          type: 'EXIT_REQUESTED',
          payload: { attemptId: undefined, reason: 'user_quit' },
        }),
      );
    }
  }

  subscribe(handler: (message: UnityToRnMessage) => void): () => void {
    this.handlers.add(handler);
    return () => this.handlers.delete(handler);
  }

  dispose(): void {
    this.timers.forEach(clearTimeout);
    this.timers = [];
    this.handlers.clear();
  }

  private later(fn: () => void): void {
    const t = setTimeout(fn, 0);
    this.timers.push(t);
  }

  private emit(message: UnityToRnMessage): void {
    this.handlers.forEach((h) => h(message));
  }
}
