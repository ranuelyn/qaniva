import { z } from 'zod';

/**
 * Unified analytics event contract for React Native, Unity, and the backend.
 * Every event carries `attemptId` where one exists so the RN product funnel and
 * the Unity in-simulation action timeline can be correlated (blueprint §17, §21).
 *
 * Keep this list SMALL. The MVP "Done" definition names exactly these events.
 */

export const analyticsSourceSchema = z.enum(['mobile', 'unity', 'backend']);
export type AnalyticsSource = z.infer<typeof analyticsSourceSchema>;

const base = {
  /** ISO-8601 timestamp from the emitter. */
  occurredAt: z.string().datetime(),
  /** Anonymous install/session id. Never a real user identifier in the MVP. */
  sessionId: z.string().min(1),
  /** Present for every event tied to a case attempt. */
  attemptId: z.string().uuid().optional(),
  source: analyticsSourceSchema,
  appVersion: z.string().min(1),
  /** Optional outreach cohort / referral code (blueprint §17). */
  refCode: z.string().optional(),
};

export const caseStartEvent = z.object({
  ...base,
  event: z.literal('case_start'),
  caseId: z.string().min(1),
  caseVersion: z.number().int().positive(),
});

export const actionTakenEvent = z.object({
  ...base,
  event: z.literal('action_taken'),
  caseId: z.string().min(1),
  actionId: z.string().min(1),
  simTimeSec: z.number().nonnegative(),
  accepted: z.boolean(),
});

export const criticalActionLatencyEvent = z.object({
  ...base,
  event: z.literal('critical_action_latency'),
  caseId: z.string().min(1),
  criterionId: z.string().min(1),
  /** Simulated seconds from case start to the satisfying action. */
  latencySimSec: z.number().nonnegative(),
});

export const caseCompleteEvent = z.object({
  ...base,
  event: z.literal('case_complete'),
  caseId: z.string().min(1),
  caseVersion: z.number().int().positive(),
  terminalOutcome: z.enum([
    'complete',
    'partial',
    'deteriorated',
    'discharge',
    'admit',
    'death',
    'aborted',
  ]),
  totalScore: z.number(),
  durationRealSec: z.number().nonnegative(),
});

export const appOpenEvent = z.object({
  ...base,
  event: z.literal('app_open'),
});

export const onboardingViewedEvent = z.object({
  ...base,
  event: z.literal('onboarding_viewed'),
});

export const onboardingCompletedEvent = z.object({
  ...base,
  event: z.literal('onboarding_completed'),
});

export const surfaceViewedEvent = z.object({
  ...base,
  event: z.literal('surface_viewed'),
  /** Product shell surface: home | cases | progress | settings | about | disclaimer. */
  surface: z.enum(['home', 'cases', 'progress', 'settings', 'about', 'disclaimer']),
});

export const caseViewedEvent = z.object({
  ...base,
  event: z.literal('case_viewed'),
  caseId: z.string().min(1),
});

export const caseAbortEvent = z.object({
  ...base,
  event: z.literal('case_abort'),
  caseId: z.string().min(1),
});

export const debriefViewedEvent = z.object({
  ...base,
  event: z.literal('debrief_viewed'),
  caseId: z.string().min(1),
});

export const replayStartEvent = z.object({
  ...base,
  event: z.literal('replay_start'),
  caseId: z.string().min(1),
  previousAttemptId: z.string().uuid(),
});

export const feedbackSubmitEvent = z.object({
  ...base,
  event: z.literal('feedback_submit'),
  caseId: z.string().min(1).optional(),
  rating: z.number().int().min(1).max(5).optional(),
  wouldReuse: z.boolean().optional(),
});

export const analyticsEventSchema = z.discriminatedUnion('event', [
  appOpenEvent,
  onboardingViewedEvent,
  onboardingCompletedEvent,
  surfaceViewedEvent,
  caseViewedEvent,
  caseAbortEvent,
  debriefViewedEvent,
  caseStartEvent,
  actionTakenEvent,
  criticalActionLatencyEvent,
  caseCompleteEvent,
  replayStartEvent,
  feedbackSubmitEvent,
]);

export type AnalyticsEvent = z.infer<typeof analyticsEventSchema>;
export type AnalyticsEventName = AnalyticsEvent['event'];

export const ANALYTICS_EVENT_NAMES = [
  'case_start',
  'action_taken',
  'critical_action_latency',
  'case_complete',
  'replay_start',
  'feedback_submit',
] as const satisfies readonly AnalyticsEventName[];

export class AnalyticsValidationError extends Error {
  constructor(
    message: string,
    readonly issues: unknown,
  ) {
    super(message);
    this.name = 'AnalyticsValidationError';
  }
}

export function parseAnalyticsEvent(input: unknown): AnalyticsEvent {
  const result = analyticsEventSchema.safeParse(input);
  if (!result.success) {
    throw new AnalyticsValidationError('Invalid analytics event', result.error.issues);
  }
  return result.data;
}
