import type { AnalyticsEvent } from '@qaniva/analytics-schema';

/**
 * Analytics abstraction. Screens/hooks call `analytics.track(...)`; the concrete
 * sink (PostHog / first-party endpoint / noop) is swapped at the boundary. Events
 * are the unified RN + Unity contract (@qaniva/analytics-schema).
 */
export interface AnalyticsSink {
  readonly name: string;
  send(event: AnalyticsEvent): void | Promise<void>;
}

export class NoopAnalyticsSink implements AnalyticsSink {
  readonly name = 'noop';
  send(): void {
    /* intentionally does nothing */
  }
}

export class ConsoleAnalyticsSink implements AnalyticsSink {
  readonly name = 'console';
  send(event: AnalyticsEvent): void {
    // eslint-disable-next-line no-console
    console.info(`[analytics] ${event.event}`, event);
  }
}

type BaseFields = 'occurredAt' | 'sessionId' | 'source' | 'appVersion';

// Omit that distributes over the discriminated union so per-event fields survive.
type DistributiveOmit<T, K extends keyof never> = T extends unknown ? Omit<T, K> : never;
export type TrackableEvent = DistributiveOmit<AnalyticsEvent, BaseFields>;

export class Analytics {
  constructor(
    private sink: AnalyticsSink,
    private readonly context: Pick<AnalyticsEvent, 'sessionId' | 'source' | 'appVersion'>,
  ) {}

  setSink(sink: AnalyticsSink): void {
    this.sink = sink;
  }

  /** Track an event; the base fields are filled from the constructor context. */
  track(event: TrackableEvent): void {
    const full = {
      ...event,
      ...this.context,
      occurredAt: new Date().toISOString(),
    } as AnalyticsEvent;
    void this.sink.send(full);
  }
}
