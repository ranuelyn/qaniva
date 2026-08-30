import { describe, expect, it } from 'vitest';
import { parseAnalyticsEvent } from '@qaniva/analytics-schema';
import { Analytics, type AnalyticsSink } from './analytics';

describe('Analytics abstraction', () => {
  it('fills base fields and emits a contract-valid event', () => {
    const seen: unknown[] = [];
    const sink: AnalyticsSink = {
      name: 'capture',
      send: (e) => {
        seen.push(e);
      },
    };
    const analytics = new Analytics(sink, {
      sessionId: 'sess-1',
      source: 'mobile',
      appVersion: '0.1.0',
    });

    analytics.track({
      event: 'case_start',
      caseId: 'demo_sync_bradycardia_001',
      caseVersion: 1,
    });

    expect(seen).toHaveLength(1);
    // Round-trips through the canonical analytics schema without throwing.
    const parsed = parseAnalyticsEvent(seen[0]);
    expect(parsed.event).toBe('case_start');
    expect(parsed.source).toBe('mobile');
  });

  it('supports swapping the sink at runtime', () => {
    let count = 0;
    const a = new Analytics(
      { name: 'a', send: () => {} },
      { sessionId: 's', source: 'mobile', appVersion: '0.1.0' },
    );
    a.setSink({
      name: 'b',
      send: () => {
        count += 1;
      },
    });
    a.track({
      event: 'replay_start',
      caseId: 'x',
      previousAttemptId: '22222222-2222-4222-8222-222222222222',
    });
    expect(count).toBe(1);
  });
});
