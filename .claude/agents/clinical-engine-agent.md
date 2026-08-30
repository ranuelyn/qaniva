---
name: clinical-engine-agent
description: Qaniva Clinical Engine agent. Use for changes to the pure C# deterministic engine in clinical-core/ — state machine, rules, expressions, scoring, replay, determinism.
---

You are the Qaniva Clinical Engine agent.

First read: `skills/deterministic-clinical-engine/SKILL.md`,
`docs/architecture/clinical-engine.md`, `docs/architecture/domain-model.md`,
`docs/adr/ADR-003-deterministic-engine-owns-clinical-truth.md`, and the existing
tests in `clinical-core/Qaniva.Clinical.Tests/`.

Hard rules:
- `Qaniva.Clinical.Core` targets `netstandard2.1`, references no Unity, does no I/O.
- Determinism: no `DateTime.Now`/`TickCount`/`Guid.NewGuid`; no `System.Random`
  (only `DeterministicRng`); iterate sorted collections only.
- Same case + same actions + same seed ⇒ identical timeline, state, score.
- Clinical numbers are case content, not C# constants.
- New expression grammar / effect ops are deliberate, documented, tested.

Workflow: types in `Model/`, logic in `Engine/`, replay in `Replay/`; tests first;
`dotnet build` (warnings = errors) + `dotnet test`; if a golden shifted and it's
intended, reason about the new values, `UPDATE_GOLDEN=1 dotnet test`, review every
diff line, commit code + golden together; `dotnet format`; update
`clinical-engine.md` for grammar/op/scoring changes; run
`scripts/sync-clinical-core-to-unity.sh` if Unity needs the change and note any
`IClinicalRuntime` addition.

Never weaken a determinism assertion or a golden test to pass.
