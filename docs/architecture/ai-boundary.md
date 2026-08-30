# AI boundary

Decision: [ADR-007](../adr/ADR-007-ai-provider-abstraction-and-safety-boundary.md).

## The rule, stated plainly

> An LLM response may **rephrase facts it was given**. It may not invent symptoms,
> vitals, drugs, history, or a diagnosis, and it can **never** mutate simulation
> state or a score. All clinical truth comes from the deterministic engine.

## Where AI is allowed (MVP)

| Feature | Input it gets | It may | It may not |
| --- | --- | --- | --- |
| Patient AI | user message, persona, `allowedFactIds`, `disclosedFacts[]` (id+text), a tone-only state summary | answer naturally from the given facts | reveal an undisclosed/absent fact; state a vital/drug/diagnosis; change state |
| Debrief AI | the deterministic timeline, `totalScore`, `missedCriterionIds`, approved evidence notes | turn it into teaching prose | recompute or change the score; invent "the correct path" |

Tutor, voice, authoring assistant, adaptive next-case: later phases, not in the MVP.

## Enforcement (`apps/api/src/ai/`)

1. **One entry point.** All calls go through `AiGatewayService`. Keys are
   backend-only; nothing reaches a client or the Unity binary.
2. **Structured output.** Provider responses are parsed with a Zod schema
   (`patientReplySchema`, `debriefNarrativeSchema`); a parse failure ⇒ fallback.
3. **Fact-id containment.** A patient reply must list `usedFactIds`. Any id outside
   `allowedFactIds ∪ disclosedFacts` ⇒ reject ⇒ fallback.
4. **Score immutability.** A debrief must echo `reportedScore === totalScore` it
   was given; mismatch ⇒ reject ⇒ fallback.
5. **Timeout ⇒ fallback.** `AI_REQUEST_TIMEOUT_MS`; the simulation never stalls.
6. **Deterministic fallback.** `StubAiProvider` builds the patient reply only from
   disclosed facts and the debrief only from the timeline. It is also the default
   provider in the foundation.
7. **Audit.** prompt version, model id, latency, schema-valid, used-fallback,
   safety flag are logged (`ai_call` in `apps/api/db/schema.sql`).
8. **Real-advice guard.** Questions that read as real medical advice get the
   "this is an educational simulation" response, not an answer.

Tests: `apps/api/src/ai/ai-gateway.service.spec.ts` (fact leak ⇒ fallback, score
tamper ⇒ fallback, timeout ⇒ fallback, clean provider passes through).

## Adding a real provider

Implement `AiProvider` in `apps/api/src/ai/providers/`, register it in
`ai.module.ts` behind the `AI_PROVIDER` switch. Do not touch the controller or the
gateway. Keep the deterministic fallback working.
