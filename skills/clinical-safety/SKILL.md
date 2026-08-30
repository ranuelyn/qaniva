# Skill: clinical-safety

## Purpose

Keep clinical truth deterministic and keep the LLM boxed in.

## When to use

Any change to `apps/api/src/ai/`, any prompt, any code that consumes an AI
response, any case's `hiddenFacts`/`debriefMetadata`, or any feature that surfaces
clinical information to the user.

## Inputs (read first)

- `docs/architecture/ai-boundary.md`
- `docs/adr/ADR-007-ai-provider-abstraction-and-safety-boundary.md`
- `apps/api/src/ai/ai.types.ts`, `ai-gateway.service.ts`, `providers/stub-provider.ts`

## Non-negotiable rules

1. An LLM response may only **rephrase given facts**. It may not invent symptoms,
   vitals, drugs, history, or a diagnosis, and it can **never** change simulation
   state or a score.
2. All LLM calls go through `AiGatewayService`. Keys are backend-only; never in a
   client bundle or the Unity binary. Use `.env` / `.env.example`.
3. Structured output only, schema-validated. Parse failure ⇒ deterministic fallback.
4. Patient AI must declare `usedFactIds`; any id outside
   `allowedFactIds ∪ disclosedFacts` ⇒ reject ⇒ fallback.
5. Debrief must echo the score it was given; mismatch ⇒ reject ⇒ fallback.
6. Timeout ⇒ fallback. The simulation never stalls on AI.
7. Real-advice / real-patient questions get the "educational simulation" boundary.
8. Log prompt version, model id, latency, schema-valid, used-fallback, safety flag.

## Workflow

1. New provider ⇒ implement `AiProvider` in `providers/`, register behind the
   `AI_PROVIDER` switch in `ai.module.ts`. Don't touch the controller or gateway.
2. Prompt change ⇒ bump the prompt version; keep the deterministic fallback
   equivalent in spirit.
3. Add/extend eval cases (allowed-facts, role break, invalid schema, prompt
   injection) — see backlog QAN-033.
4. `pnpm --filter @qaniva/api test`.

## Validation

- `apps/api/src/ai/ai-gateway.service.spec.ts` green: fact leak ⇒ fallback, score
  tamper ⇒ fallback, timeout ⇒ fallback, clean provider passes through.
- Grep: no API key literal in the repo; `.env` git-ignored; `.env.example` has
  placeholders only.

## Done criteria

Gateway enforces containment + score immutability + timeout fallback; new provider
is config-only; deterministic fallback still works; evals cover the change; audit
fields logged.

## Common failure modes

- Trusting model prose without `usedFactIds` / schema validation.
- A "helpful" prompt that invites the model to add clinical detail.
- Removing the fallback to "keep the UX pure".
- Passing raw case internals to the model instead of the allowed/disclosed subset.
- Putting a key in an Expo `EXPO_PUBLIC_*` var.
