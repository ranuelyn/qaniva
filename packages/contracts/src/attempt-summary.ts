import { z } from 'zod';

/**
 * The compact result Unity hands back to React Native on SIMULATION_COMPLETED.
 *
 * This is intentionally small: enough for the RN Results/Debrief screens to render
 * without a network call. The authoritative, full event log is uploaded separately
 * to the backend keyed by `attemptId` (see docs/architecture/clinical-engine.md).
 */

export const timelineEntrySchema = z.object({
  /** Monotonic sequence number within the attempt. */
  seq: z.number().int().nonnegative(),
  /** Simulated clock time in seconds when the action was applied. */
  simTimeSec: z.number().nonnegative(),
  /** Action identifier from the case definition, or a synthetic engine event id. */
  actionId: z.string().min(1),
  /** Human-facing short label, already localized by the engine/presentation layer. */
  label: z.string().min(1),
  /** Classification produced by the deterministic engine — never by an LLM. */
  classification: z.enum(['neutral', 'correct', 'delayed', 'missed', 'harmful']),
});
export type TimelineEntry = z.infer<typeof timelineEntrySchema>;

export const scoreBreakdownSchema = z.object({
  critical: z.number(),
  timing: z.number(),
  efficiency: z.number(),
  treatment: z.number(),
  disposition: z.number(),
});
export type ScoreBreakdown = z.infer<typeof scoreBreakdownSchema>;

export const attemptSummarySchema = z.object({
  attemptId: z.string().uuid(),
  caseId: z.string().min(1),
  caseVersion: z.number().int().positive(),
  /** Seed used for the deterministic RNG, echoed for replay. */
  seed: z.number().int().nonnegative(),
  startedAt: z.string().datetime(),
  completedAt: z.string().datetime(),
  terminalState: z.enum(['complete', 'discharge', 'admit', 'death', 'aborted']),
  totalScore: z.number(),
  scoreBreakdown: scoreBreakdownSchema,
  timeline: z.array(timelineEntrySchema),
  /**
   * Hash of (caseVersion + ordered actionIds + seed + finalStateHash).
   * RN and backend use this to detect a determinism regression between runs.
   */
  replayHash: z.string().min(8),
});
export type AttemptSummary = z.infer<typeof attemptSummarySchema>;
