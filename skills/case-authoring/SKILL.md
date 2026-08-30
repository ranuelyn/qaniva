# Skill: case-authoring

## Purpose

Author a `case.json` that validates, replays deterministically, and keeps clinical
truth reviewable.

## When to use

Creating a new case, bumping a case version, or changing any action/rule/rubric.

## Inputs (read first)

- `docs/clinical/case-authoring-guide.md`
- `packages/case-schema/schema/case.schema.json`
- `packages/case-schema/fixtures/demo_sync_bradycardia_001/v1/case.json` (worked example)
- `docs/architecture/clinical-engine.md` (the condition mini-language)

## Non-negotiable rules

1. `metadata.fictional: true`. No real-patient data.
2. Every clinical number/decision is provisional until
   `metadata.clinicalReview.status == "approved"` with a reviewer + date.
3. A new content `version` = a new `v<n>/` folder. Never edit a published version
   in place.
4. No clinical logic in `presentationProfile` — that block is Unity asset keys only.
5. Ids must resolve: `criterionIds` → `scoringCriteria`, `acceptedActions` →
   `availableActions`, rule `terminalState` → `terminalStates`.
6. `visibility: "when"` requires a non-null `visibleWhen`.

## Workflow

1. Copy the demo fixture into `fixtures/<id>/v1/case.json`; rewrite section by
   section per the guide.
2. `pnpm run validate:cases` until clean (schema + cross-references).
3. `dotnet run --project clinical-core/Qaniva.Clinical.Cli -- validate <path>`.
4. Write the 6 golden-path replay scripts under
   `clinical-core/Qaniva.Clinical.Tests/Golden/` (ideal, delayed-critical,
   wrong-harmless, harmful, early-disposition, AI-unavailable).
5. `UPDATE_GOLDEN=1 dotnet test` to create the golden files; review them.
6. Open a PR; request clinician review; record sign-off in `metadata.clinicalReview`.

## Validation

- `pnpm run validate:cases` → 0 failures.
- `cd clinical-core && dotnet test` → new golden tests green.
- Each golden script reaches a `terminalState` with no unexpected `rejections`.

## Done criteria

Schema + semantic valid; 6 golden paths committed with golden files; clinician
sign-off recorded; no id dangling; `presentationProfile` carries no logic.

## Common failure modes

- Rubric points that can't be earned because `acceptedActions` are never visible.
- A deterioration rule with `once:false` that re-fires every tick.
- Using `=` instead of `==` in a `when` expression (the evaluator rejects it —
  fix, don't work around).
- Putting the "right answer" narrative in `debriefMetadata` in a way that leaks
  before the debrief.
- Forgetting to bump `version` after changing a number.
