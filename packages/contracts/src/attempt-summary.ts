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
  /**
   * Authored causality texts of transition rules that fired on this step
   * (case data `transitionRules[].debriefText`, resolved by the engine adapter).
   * Empty for steps with no meaningful state change.
   */
  stateChanges: z.array(z.string()),
});
export type TimelineEntry = z.infer<typeof timelineEntrySchema>;

/**
 * One rubric criterion's final, deterministic outcome — the substance of the
 * debrief. Produced by the engine's ScoringEngine, never by an LLM.
 */
export const criterionResultSchema = z.object({
  id: z.string().min(1),
  /** Learner-facing criterion label from the case definition. */
  label: z.string().min(1),
  /** Rubric bucket: critical | timing | efficiency | treatment | disposition. */
  category: z.string().min(1),
  criticality: z.enum(['critical', 'major', 'minor']),
  harmful: z.boolean(),
  /**
   * Non-harmful criteria: correct | delayed | missed.
   * Harmful criteria: harmful (performed) | avoided (not performed).
   */
  classification: z.enum(['correct', 'delayed', 'missed', 'harmful', 'avoided']),
  /** Sim-clock seconds when credited; -1 when never credited. */
  creditedAtSec: z.number().int(),
  awardedPoints: z.number(),
  /** Positive max for scored criteria; negative magnitude for harmful penalties. */
  maxPoints: z.number(),
  /** Evidence-ledger ids from the case rubric (learner-visible traceability). */
  evidenceRefs: z.array(z.string()),
  /** Labels of every accepted action — >1 label means accepted alternatives exist. */
  acceptedActionLabels: z.array(z.string()),
});
export type CriterionResult = z.infer<typeof criterionResultSchema>;

/** A case-authored literature reference (concise; rendered in the debrief). */
export const caseReferenceSchema = z.object({
  label: z.string().min(1),
  citation: z.string().min(1),
});
export type CaseReference = z.infer<typeof caseReferenceSchema>;

/** Case-authored debrief narrative metadata (rephrased at most — never invented — by AI). */
export const debriefContentSchema = z.object({
  summary: z.string(),
  keyTeachingPoints: z.array(z.string()),
  commonErrors: z.array(z.string()),
});
export type DebriefContent = z.infer<typeof debriefContentSchema>;

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
  terminalState: z.enum([
    'complete',
    'partial',
    'deteriorated',
    'discharge',
    'admit',
    'death',
    'aborted',
  ]),
  totalScore: z.number(),
  scoreBreakdown: scoreBreakdownSchema,
  timeline: z.array(timelineEntrySchema),
  /** Per-criterion debrief outcomes, in case-definition order. */
  criteria: z.array(criterionResultSchema),
  debrief: debriefContentSchema,
  /** The case's authored references (guideline organization/year — concise). */
  references: z.array(caseReferenceSchema),
  /**
   * Hash of (caseVersion + ordered actionIds + seed + finalStateHash).
   * RN and backend use this to detect a determinism regression between runs.
   */
  replayHash: z.string().min(8),
});
export type AttemptSummary = z.infer<typeof attemptSummarySchema>;
