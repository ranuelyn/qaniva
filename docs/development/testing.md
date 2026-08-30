# Testing

Testing is set up from day one (blueprint §14). "It looks like it works" is not a
pass.

## Layers

| Layer | Where | Runner | Runs in CI |
| --- | --- | --- | --- |
| Contract unit | `packages/contracts` | vitest | yes |
| C#↔TS bridge parity | `packages/contracts/src/__tests__/csharp-parity.test.ts` | vitest | yes |
| Case schema (structure + cross-ref) | `packages/case-schema` | vitest + `validate:cases` CLI | yes |
| Analytics contract | `packages/analytics-schema` | vitest | yes |
| Engine unit | `clinical-core/Qaniva.Clinical.Tests` | xUnit | yes (dotnet job) |
| Determinism (run A == run B) | `DeterminismTests` | xUnit | yes |
| Golden replay (run == committed) | `GoldenReplayTests` | xUnit | yes |
| Backend integration | `apps/api/src/*.spec.ts` | jest + supertest | yes |
| AI safety boundary | `apps/api/src/ai/*.spec.ts` | jest | yes |
| Mobile RN-free unit | `apps/mobile/src/**/*.test.ts` | vitest | yes |
| Mobile typecheck | `apps/mobile` | tsc | yes |
| Unity EditMode | `unity/.../Scripts/Tests` | Unity Test Runner | **no** (needs the Editor — run locally) |
| Device perf, e2e beta | — | manual | release |

## Commands

```bash
pnpm run ci                 # format:check + lint + typecheck + test across the TS workspace
pnpm run validate:cases     # every fixture: JSON Schema + semantic cross-references
cd clinical-core && dotnet test
```

## The determinism guarantees

1. **Same script twice ⇒ identical everything.** `DeterminismTests` runs each
   golden script twice and asserts equal `replayHash`, `StateHash`, score, and
   serialized result.
2. **Hash chain.** Each `AttemptEvent.beforeHash` equals the previous
   `afterHash`; `seq` is contiguous from 0.
3. **Seed-independence for non-stochastic cases.** Different seeds ⇒ same outcome
   for the demo case (it has no randomness).
4. **Golden lock.** `ideal_path` / `harmful_path` output is frozen in
   `Golden/*.golden.json`. A shift fails the build and forces a human to review
   the diff. Regenerate only after reviewing:
   `UPDATE_GOLDEN=1 dotnet test` then `git diff` the golden files.

## Invalid-input & time tests

- Precondition not met ⇒ action rejected, **state hash unchanged**, timeline empty.
- `AdvanceTime` past a deterioration threshold fires the rule via the
  `SimulationClock` with no player action.

## Adding a test with a change

Every behavioural change ships a test in the matching layer, and the command
output goes in the PR. Never `.skip` or delete a failing test to get green — fix
the code or the design.
