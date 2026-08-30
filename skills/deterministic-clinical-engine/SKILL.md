# Skill: deterministic-clinical-engine

## Purpose

Work on `clinical-core/` without breaking determinism, the Unity-referenceability
of the Core, or the golden regression net.

## When to use

Any change under `clinical-core/`, or when a case needs an engine capability that
doesn't exist yet (a new effect op, an expression accessor, a scoring mode).

## Inputs (read first)

- `docs/architecture/clinical-engine.md`
- `docs/architecture/domain-model.md`
- `docs/adr/ADR-003-deterministic-engine-owns-clinical-truth.md`
- existing tests in `clinical-core/Qaniva.Clinical.Tests/`

## Non-negotiable rules

1. `Qaniva.Clinical.Core` targets `netstandard2.1` and references **no** Unity and
   **no** I/O. Keep it that way.
2. Determinism: no `DateTime.Now` / `TickCount` / `Guid.NewGuid` in engine logic;
   no `System.Random` (only `DeterministicRng`); iterate sorted collections only.
3. Same `CaseDefinition` + same ordered actions + same seed ⇒ identical timeline,
   state, score. If a change breaks a golden, that's a signal — review, don't
   paper over.
4. Clinical numbers are content, not engine constants. Don't bake case values into
   C#.
5. New expression grammar / effect ops are deliberate, documented, and tested.

## Workflow

1. Add/adjust types in `Model/`, logic in `Engine/`, replay in `Replay/`.
2. Add unit tests first (`ExpressionEvaluatorTests`, `EngineBehaviourTests`, …).
3. `dotnet build` (warnings are errors) and `dotnet test`.
4. If a golden shifted and the shift is intended and reviewed:
   `UPDATE_GOLDEN=1 dotnet test`, then read every line of
   `git diff clinical-core/Qaniva.Clinical.Tests/Golden`.
5. If Unity consumes the change: `scripts/sync-clinical-core-to-unity.sh` and note
   any `IClinicalRuntime` addition.
6. `dotnet format`.

## Validation

- `cd clinical-core && dotnet build && dotnet test && dotnet format --verify-no-changes`
- `DeterminismTests` still asserts run A == run B for every golden script.
- No new `using UnityEngine` / `System.IO` in `Qaniva.Clinical.Core`.

## Done criteria

Build clean (warnings-as-errors), all xUnit green, golden diffs reviewed and
intentional, `IClinicalRuntime` updated if Unity needs the new surface,
`clinical-engine.md` updated for grammar/op/scoring changes.

## Common failure modes

- Using a `Dictionary`/`HashSet` and depending on its iteration order.
- Formatting a double without the `Hashing` helper → hash noise → flaky golden.
- Adding a `resultTemplate` string with clinical content in C# instead of the case.
- Relaxing a golden assertion to `Contains` to dodge a real regression.
- Introducing `async`/time-based behaviour into the tick.
