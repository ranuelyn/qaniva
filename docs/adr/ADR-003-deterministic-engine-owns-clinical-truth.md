# ADR-003 — A deterministic C# engine owns clinical truth

## Status

Accepted (2026-08-30). This is the most important architectural decision in Qaniva.

## Context

The competitor teardown showed that the durable value is the case schema, action
catalog, state outcomes, scoring, and debrief — not the 3D visuals or any LLM.
Clinical reliability, regression testing, and future headless/web replay all
require that the simulation's behaviour be reproducible and testable in isolation.

## Decision

A **pure C# assembly** (`clinical-core/Qaniva.Clinical.Core`, `netstandard2.1`,
zero Unity references) is the single source of clinical truth. It owns
`CaseDefinition`, `PatientState`, preconditions, `TransitionRule` evaluation, the
`SimulationClock`, the event `AttemptTimeline`, `ScoringCriterion` evaluation,
`SimulationSnapshot`, and replay.

**Invariant:** same `CaseDefinition` + same ordered actions + same seed ⇒
byte-identical timeline, final state, and score. A seeded `DeterministicRng`
(SplitMix64) is the only randomness; no wall-clock, no unordered iteration.

Neither React Native nor Unity may compute a vital, a state transition, a drug
result, a diagnosis, or a score. An LLM may never mutate simulation state.

## Alternatives considered

- **Engine in TypeScript** (shared with backend). Rejected: Unity needs it at
  runtime; a C# core avoids a second implementation and keeps the Unity path
  first-class. The backend does not need to run the engine for the MVP.
- **Engine logic in Unity `MonoBehaviour`s.** Rejected: not testable without the
  Editor, not reproducible, and couples truth to presentation.
- **Rules only in case JSON, interpreted ad hoc per client.** Rejected: guarantees
  drift between RN, Unity, and backend.

## Consequences

- Determinism is a CI release gate: golden replay tests lock the observable output
  for known scripts; regenerating a golden (`UPDATE_GOLDEN=1`) requires human
  review of the diff.
- Unity consumes the engine as a DLL (`scripts/sync-clinical-core-to-unity.sh`).
- Clinical-number changes are content changes, gated by clinician review
  (`metadata.clinicalReview.status`), never made to satisfy a UI or a test.
- The engine has no I/O; case loading and persistence live at the edges.
