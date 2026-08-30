import { z } from 'zod';

/**
 * AI boundary contracts. See docs/architecture/ai-boundary.md.
 *
 * HARD RULE: the LLM may only rephrase facts it was given. It must not invent
 * symptoms, vitals, drugs, history, diagnosis, or scores, and it can never mutate
 * simulation state. The gateway enforces this by validating structured output and
 * rejecting any response that references a fact id outside `allowedFactIds`.
 */

export const patientTurnContextSchema = z.object({
  attemptId: z.string().uuid(),
  persona: z.string().min(1),
  /** Fact ids the patient is allowed to talk about IF asked (case `hiddenFacts` with disclosure "on_ask"). */
  allowedFactIds: z.array(z.string()).default([]),
  /** Fact ids already disclosed by the deterministic engine, with their text. */
  disclosedFacts: z.array(z.object({ id: z.string(), text: z.string() })).default([]),
  /** A short, non-authoritative summary of current state for tone only. */
  currentStateSummary: z.string().default(''),
  userMessage: z.string().min(1),
  safetyPolicyVersion: z.string().default('v1'),
});
export type PatientTurnContext = z.infer<typeof patientTurnContextSchema>;

export const patientReplySchema = z.object({
  reply: z.string().min(1),
  /** Fact ids the reply draws on. MUST be a subset of the disclosed/allowed set. */
  usedFactIds: z.array(z.string()).default([]),
  /** True if the model judged the question to be outside the simulation (real medical advice, etc.). */
  outOfScope: z.boolean().default(false),
});
export type PatientReply = z.infer<typeof patientReplySchema>;

export const debriefContextSchema = z.object({
  attemptId: z.string().uuid(),
  /** The deterministic timeline + rubric result. The model narrates, it does not recompute. */
  timeline: z.array(
    z.object({
      simTimeSec: z.number(),
      actionId: z.string(),
      classification: z.enum(['neutral', 'correct', 'delayed', 'missed', 'harmful']),
    }),
  ),
  totalScore: z.number(),
  missedCriterionIds: z.array(z.string()).default([]),
  approvedEvidenceNotes: z.array(z.string()).default([]),
});
export type DebriefContext = z.infer<typeof debriefContextSchema>;

export const debriefNarrativeSchema = z.object({
  narrative: z.string().min(1),
  /** Echo of the score the model was given — the gateway asserts it is unchanged. */
  reportedScore: z.number(),
});
export type DebriefNarrative = z.infer<typeof debriefNarrativeSchema>;

export interface AiProvider {
  readonly name: string;
  patientReply(ctx: PatientTurnContext): Promise<PatientReply>;
  debriefNarrative(ctx: DebriefContext): Promise<DebriefNarrative>;
}

export const AI_PROVIDER = Symbol('QANIVA_AI_PROVIDER');
