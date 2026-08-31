import { describe, expect, it } from 'vitest';
import {
  BridgeDecodeError,
  decodeRnToUnity,
  decodeUnityToRn,
  encodeMessage,
  type RnToUnityMessage,
  type UnityToRnMessage,
} from '../bridge-messages';
import { PROTOCOL_VERSION } from '../protocol';

const START: RnToUnityMessage = {
  protocolVersion: PROTOCOL_VERSION,
  messageId: '11111111-1111-4111-8111-111111111111',
  sentAt: '2026-08-30T10:00:00.000Z',
  type: 'START_SIMULATION',
  payload: {
    caseId: 'stemi_001',
    caseVersion: 1,
    attemptId: '22222222-2222-4222-8222-222222222222',
    locale: 'en',
    difficulty: 'standard',
    seed: 42,
    mode: 'interactive',
  },
};

const COMPLETED: UnityToRnMessage = {
  protocolVersion: PROTOCOL_VERSION,
  messageId: '33333333-3333-4333-8333-333333333333',
  sentAt: '2026-08-30T10:12:00.000Z',
  type: 'SIMULATION_COMPLETED',
  payload: {
    attemptId: '22222222-2222-4222-8222-222222222222',
    summary: {
      attemptId: '22222222-2222-4222-8222-222222222222',
      caseId: 'stemi_001',
      caseVersion: 1,
      seed: 42,
      startedAt: '2026-08-30T10:00:01.000Z',
      completedAt: '2026-08-30T10:12:00.000Z',
      terminalState: 'complete',
      totalScore: 82,
      scoreBreakdown: { critical: 40, timing: 18, efficiency: 8, treatment: 12, disposition: 4 },
      timeline: [
        {
          seq: 0,
          simTimeSec: 132,
          actionId: 'ecg_12lead',
          label: '12-lead ECG',
          classification: 'correct',
        },
      ],
      replayHash: 'deadbeefcafe',
    },
  },
};

describe('bridge message codec', () => {
  it('round-trips an RN->Unity message', () => {
    expect(decodeRnToUnity(encodeMessage(START))).toEqual(START);
  });

  it('round-trips a Unity->RN message', () => {
    expect(decodeUnityToRn(encodeMessage(COMPLETED))).toEqual(COMPLETED);
  });

  it('rejects a message with the wrong protocol version', () => {
    const bad = { ...START, protocolVersion: 999 };
    expect(() => decodeRnToUnity(JSON.stringify(bad))).toThrow(BridgeDecodeError);
  });

  it('rejects a Unity message type on the RN->Unity channel', () => {
    expect(() => decodeRnToUnity(encodeMessage(COMPLETED as unknown as RnToUnityMessage))).toThrow(
      BridgeDecodeError,
    );
  });

  it('rejects non-JSON', () => {
    expect(() => decodeUnityToRn('<not json>')).toThrow(BridgeDecodeError);
  });

  it('rejects unknown extra keys in an empty payload', () => {
    const bad = {
      ...START,
      type: 'PAUSE_SIMULATION',
      payload: { sneaky: true },
    };
    expect(() => decodeRnToUnity(JSON.stringify(bad))).toThrow(BridgeDecodeError);
  });
});
