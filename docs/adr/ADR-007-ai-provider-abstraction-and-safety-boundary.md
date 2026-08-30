# ADR-007 — AI provider abstraction + safety boundary

## Status

Accepted (2026-08-30).

## Context

The MVP uses AI for exactly two things: a text **Patient AI** (anamnesis) and a
personalized **debrief narration**. An LLM must never become a source of clinical
truth, and the product must survive a provider outage. Vendors and models will
change.

## Decision

All LLM calls go through a single backend **AI gateway** (`apps/api/src/ai/`)
behind an `AiProvider` interface. Providers are selected by config (`AI_PROVIDER`);
only a deterministic `StubAiProvider` ships in the foundation (it is also the
fallback). Real providers are added behind the same interface, **backend-only** —
no key ever reaches a client or the Unity binary.

The gateway enforces the boundary:

- **Structured output only**, validated against a schema.
- **Patient AI**: the response must declare which fact ids it used; any id outside
  `allowedFactIds ∪ disclosedFacts` ⇒ reject ⇒ deterministic fallback. The model
  may rephrase given facts; it may not invent symptoms, vitals, drugs, history, or
  a diagnosis, and it cannot change state.
- **Debrief AI**: it narrates the deterministic timeline + rubric result. It must
  echo the score it was given; a mismatch ⇒ reject ⇒ fallback. It cannot recompute
  the score or invent "the correct path".
- **Timeout ⇒ deterministic fallback** so the simulation never stalls.
- Prompt version, model id, latency, schema-validation result, and a safety flag
  are logged (`ai_call` table in the target schema).
- Real-patient / real-advice questions are detected and answered with the "this is
  an educational simulation" boundary.

## Alternatives considered

- **Call the LLM directly from the client.** Rejected: leaks keys, no enforcement
  point, no fallback.
- **Trust the model's prose without structured validation.** Rejected: no way to
  detect a hallucinated fact or a changed score.
- **No fallback (fail the turn).** Rejected: a provider blip would break a case.

## Consequences

- Every AI feature has a deterministic template that must be maintained alongside
  the prompt.
- Adding a provider is an adapter + config, not a controller change.
- The gateway is the only place prompt/safety versioning lives.
