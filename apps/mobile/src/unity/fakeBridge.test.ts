import { describe, expect, it, vi } from 'vitest';
import { PROTOCOL_VERSION, type UnityToRnMessage } from '@qaniva/contracts';
import { FakeUnityBridge, buildDeterministicSummary } from './fakeBridge';

function startMessage(seed: number) {
  return {
    protocolVersion: PROTOCOL_VERSION,
    messageId: '00000000-0000-4000-8000-000000000001',
    sentAt: '2026-08-30T10:00:00.000Z',
    type: 'START_SIMULATION' as const,
    payload: {
      caseId: 'demo_sync_bradycardia_001',
      caseVersion: 1,
      attemptId: '22222222-2222-4222-8222-222222222222',
      locale: 'en',
      difficulty: 'standard' as const,
      seed,
    },
  };
}

describe('FakeUnityBridge', () => {
  it('emits SIMULATION_READY then SIMULATION_COMPLETED for a START', async () => {
    vi.useFakeTimers();
    const bridge = new FakeUnityBridge();
    const received: UnityToRnMessage[] = [];
    bridge.subscribe((m) => received.push(m));

    bridge.send(startMessage(7));
    await vi.runAllTimersAsync();

    expect(received.map((m) => m.type)).toEqual(['SIMULATION_READY', 'SIMULATION_COMPLETED']);
    const completed = received[1];
    if (!completed || completed.type !== 'SIMULATION_COMPLETED') {
      throw new Error('expected SIMULATION_COMPLETED');
    }
    expect(completed.payload.summary.caseId).toBe('demo_sync_bradycardia_001');
    bridge.dispose();
    vi.useRealTimers();
  });

  it('rejects a message that violates the protocol contract', () => {
    const bridge = new FakeUnityBridge();
    const bad = { ...startMessage(1), protocolVersion: 42 };
    expect(() => bridge.send(bad as never)).toThrow();
    bridge.dispose();
  });

  it('buildDeterministicSummary is pure (same seed -> same summary)', () => {
    const a = buildDeterministicSummary({
      attemptId: '22222222-2222-4222-8222-222222222222',
      caseId: 'demo_sync_bradycardia_001',
      caseVersion: 1,
      seed: 123,
    });
    const b = buildDeterministicSummary({
      attemptId: '22222222-2222-4222-8222-222222222222',
      caseId: 'demo_sync_bradycardia_001',
      caseVersion: 1,
      seed: 123,
    });
    expect(a).toEqual(b);
  });
});
