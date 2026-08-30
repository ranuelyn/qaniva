import { describe, expect, it } from 'vitest';
import { ANALYTICS_EVENT_NAMES, AnalyticsValidationError, parseAnalyticsEvent } from '../events';

const commonBase = {
  occurredAt: '2026-08-30T10:00:00.000Z',
  sessionId: 'sess-abc',
  attemptId: '22222222-2222-4222-8222-222222222222',
  source: 'unity' as const,
  appVersion: '0.1.0',
};

describe('analytics event contract', () => {
  it('accepts a well-formed case_start event', () => {
    const evt = parseAnalyticsEvent({
      ...commonBase,
      event: 'case_start',
      caseId: 'demo_sync_bradycardia_001',
      caseVersion: 1,
    });
    expect(evt.event).toBe('case_start');
  });

  it('accepts action_taken from the mobile source without attemptId', () => {
    const { attemptId: _omit, ...noAttempt } = commonBase;
    const evt = parseAnalyticsEvent({
      ...noAttempt,
      source: 'mobile',
      event: 'action_taken',
      caseId: 'demo_sync_bradycardia_001',
      actionId: 'ecg_12lead',
      simTimeSec: 110,
      accepted: true,
    });
    expect(evt.event).toBe('action_taken');
  });

  it('rejects an unknown event name', () => {
    expect(() => parseAnalyticsEvent({ ...commonBase, event: 'nope' })).toThrow(
      AnalyticsValidationError,
    );
  });

  it('rejects a case_complete with an invalid terminal outcome', () => {
    expect(() =>
      parseAnalyticsEvent({
        ...commonBase,
        event: 'case_complete',
        caseId: 'x',
        caseVersion: 1,
        terminalOutcome: 'exploded',
        totalScore: 10,
        durationRealSec: 120,
      }),
    ).toThrow(AnalyticsValidationError);
  });

  it('exposes exactly the MVP event names', () => {
    expect([...ANALYTICS_EVENT_NAMES].sort()).toEqual(
      [
        'action_taken',
        'case_complete',
        'case_start',
        'critical_action_latency',
        'feedback_submit',
        'replay_start',
      ].sort(),
    );
  });
});
