# Skill: testing-and-golden-replay

## Purpose

Keep the test net honest — real coverage, no green-washing — and manage golden
replay files correctly.

## When to use

Any change with behaviour; any time a test fails; any time a golden file changes.

## Inputs (read first)

- `docs/development/testing.md`
- `clinical-core/Qaniva.Clinical.Tests/` (all)
- `clinical-core/Qaniva.Clinical.Tests/Golden/`

## Non-negotiable rules

1. Never `.skip`, delete, or weaken a failing test to get green. Fix the code or
   the design.
2. Every behavioural change ships a test in the matching layer; the command output
   goes in the PR.
3. A golden diff is reviewed line by line by a human before it's committed.
   `UPDATE_GOLDEN=1` is a deliberate, reviewed act — never run blindly in CI.
4. Determinism assertions stay strict equality (`replayHash`, `StateHash`, score,
   serialized result). Don't downgrade to `Contains`/`approximately`.
5. Invalid-input tests assert **state unchanged** (hash equal), not just "no crash".

## Workflow

1. Pick the layer (testing.md table). Write the test first where practical.
2. TS: `pnpm run ci`. Cases: `pnpm run validate:cases`. Engine:
   `cd clinical-core && dotnet test`. API: `pnpm --filter @qaniva/api test`.
3. If an engine/case change moved a golden:
   - confirm the new output is *correct* (reason about it);
   - `UPDATE_GOLDEN=1 dotnet test`;
   - `git diff clinical-core/Qaniva.Clinical.Tests/Golden` — read every line;
   - commit the golden change in the same commit as the code change, explained.
4. New case ⇒ 6 golden-path scripts + golden files (see `case-authoring`).

## Validation

- All layers for the change are green; output pasted in the PR.
- `DeterminismTests` still asserts run A == run B for every golden script.
- Golden diff is intentional and explained in the commit message.

## Done criteria

New behaviour is covered; no test skipped/deleted to pass; golden changes reviewed
and explained; determinism assertions unchanged in strictness.

## Common failure modes

- "The golden is just noise, I'll regenerate it" — without checking the new values
  are right.
- Asserting `result.Accepted == false` but not that the state hash is unchanged.
- Adding a test that imports `react-native` into the `vitest` scope.
- Letting a flaky test through because "it usually passes" — determinism means it
  must always pass; find the nondeterminism.
