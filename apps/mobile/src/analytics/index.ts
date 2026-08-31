import Constants from 'expo-constants';
import { Analytics, ConsoleAnalyticsSink, NoopAnalyticsSink } from './analytics';
import { cryptoRandomId } from '@/lib/ids';

/**
 * App-wide analytics singleton. Sink policy: console in dev (visible in Metro),
 * noop otherwise until a real provider lands (QAN-017 backend ingest /
 * QAN-032). Events are the typed @qaniva/analytics-schema union — no free-form
 * payloads, no PII, no clinical content beyond case/action ids.
 */
export const analytics = new Analytics(
  __DEV__ ? new ConsoleAnalyticsSink() : new NoopAnalyticsSink(),
  {
    sessionId: cryptoRandomId(),
    source: 'mobile',
    appVersion: Constants.expoConfig?.version ?? '0.0.0',
  },
);
