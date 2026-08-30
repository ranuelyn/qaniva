# ADR-004 — Data-driven, versioned case format

## Status

Accepted (2026-08-30).

## Context

Cases must be authored and reviewed like content, not code. Every case needs
author/reviewer/version/evidence metadata (for academic trust and for store
review). RN, Unity, and the backend must all speak the same case identity.

## Decision

Cases are **schema-validated JSON**, not hard-coded C# classes. The canonical
schema is `packages/case-schema/schema/case.schema.json` (JSON Schema
draft 2020-12), with:

- `schemaVersion` (integer) — bumped on any breaking schema change; the engine
  refuses versions it does not support.
- `version` (integer) — the content version, bumped on any change to clinical
  numbers, actions, rules, or the rubric.
- Required sections: `metadata` (incl. `clinicalReview`, `fictional: true`),
  `learningObjectives`, `presentationProfile`, `patient`, `initialState`,
  `hiddenFacts`, `availableActions`, `transitionRules`, `scoringCriteria`,
  `terminalStates`, `debriefMetadata`, `references`.
- A small, read-only **condition expression** mini-language for rule/precondition
  logic (documented in the schema and in the engine).

Structural validation (JSON Schema) plus semantic validation (cross-references
resolve, no dangling ids, no duplicates) run in CI. The same JSON is deserialized
by the C# engine.

## Alternatives considered

- **C# case classes.** Rejected: every case becomes a code change and a deploy.
- **YAML / a DSL.** Rejected: JSON is trivially consumable by C#, TS, and tooling
  with mature schema validation; a DSL is more power than the MVP needs.
- **Embedding a scripting language (Lua/JS) for rules.** Rejected for the MVP:
  larger attack/complexity surface; the mini-expression language covers the cases.

## Consequences

- Case JSON PRs are reviewed like code; golden replay tests run on case changes.
- `presentationProfile` maps a case to Unity asset keys, keeping clinical logic out
  of scene files (blueprint §9).
- The mini-expression language is a maintained artifact; extending it is a
  deliberate change with tests.
