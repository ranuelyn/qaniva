# Case authoring guide

A case is **data** (`case.json`), validated by
`packages/case-schema/schema/case.schema.json`, run by the C# engine. This guide is
how to write one. It does **not** teach medicine — every clinical number must be
verified by a clinician (see "Review").

## Location & versioning

```
packages/case-schema/fixtures/<case_id>/v<version>/case.json
```

- `id` — snake_case, stable forever (e.g. `stemi_001`).
- `schemaVersion` — the schema contract version (currently `1`). The engine
  refuses versions it doesn't support.
- `version` — the content version. **Bump it on any change** to clinical numbers,
  actions, rules, or the rubric. A new version is a new folder.
- `metadata.fictional` must be `true`. MVP uses synthetic cases only.
- `metadata.clinicalReview.status`: `not_reviewed` → `in_review` → `approved`.
  A case is not shippable until `approved` with a reviewer and date.

## Anatomy

| Section | Write this |
| --- | --- |
| `metadata` | title (mark demos "FICTIONAL DEMO"), chief complaint, specialty, estimate, authors, review block |
| `learningObjectives` | what the learner should take away (drives the debrief) |
| `presentationProfile` | Unity asset keys — room, patient variant, start animation, monitor layout, required props, camera, audio. **No clinical logic here.** |
| `patient` | display name, age, sex, weight, persona (tone for Patient AI), background facts (narrative, not auto-disclosed) |
| `initialState` | vitals, rhythm, ABC/neuro enums, pain, starting flags |
| `hiddenFacts` | `{ id, disclosure: on_ask|on_exam|on_order_result, text }` — Patient AI may only surface `on_ask` facts, and only once the engine has disclosed them |
| `availableActions` | see below |
| `transitionRules` | time/state-driven changes — deterioration, stabilisation, arrest |
| `scoringCriteria` | the rubric — see below |
| `terminalStates` | `{ id, when, outcome, label }`; a rule or a `when` ends the attempt |
| `debriefMetadata` | summary, key teaching points, common errors |
| `references` | citations; for demos, an explicit "not a clinical source" note |

### Actions

```jsonc
{
  "id": "give_atropine",
  "type": "medication",                       // examine|order|medication|procedure|consult|disposition|communication
  "label": "Give first-line drug",
  "timeCostSec": 30,                          // advances the sim clock when performed
  "visibility": "when",                       // "always" or "when"
  "visibleWhen": "flag('atropine_given') || simTimeSec >= 180",  // required if visibility=="when"
  "preconditions": ["flag('iv_access')"],     // all must be true or the action is rejected
  "params": [{ "name": "dose_mg", "kind": "number", "min": 0.5, "max": 3, "unit": "mg" }],
  "effects": [
    { "op": "adjust", "target": "vitals.hr", "value": 30 },
    { "op": "setEnum", "target": "circulation", "value": "normal" },
    { "op": "setFlag", "flag": "atropine_given" }
  ],
  "criterionIds": ["give_first_line"],        // must exist in scoringCriteria
  "repeatable": false
}
```

### Rules

```jsonc
{
  "id": "deterioration_untreated",
  "when": "simTimeSec >= 300 && !flag('atropine_given') && !flag('pacing_started')",
  "priority": 100,        // higher runs first; ties broken by id
  "once": true,           // fire at most once per attempt
  "delaySec": 0,          // apply this many seconds after the condition first holds
  "effects": [ { "op": "adjust", "target": "vitals.hr", "value": -15 } ],
  "presentationCue": "distress_severe",   // string for the Unity presentation layer
  "terminalState": null   // or a terminalStates[].id to end the attempt
}
```

### Rubric

```jsonc
{
  "id": "give_first_line",
  "label": "Gave the first-line drug",
  "criticality": "critical",              // critical|major|minor
  "acceptedActions": ["give_atropine"],   // any of these satisfies it
  "timingWindow": { "fullCreditBeforeSec": 120, "zeroCreditAfterSec": 360 },  // optional; linear between
  "stateConstraints": ["flag('iv_access')"],  // optional; must hold at credit time
  "points": 40,
  "category": "critical",                 // critical|timing|efficiency|treatment|disposition
  "harmful": false,                       // if true, doing an acceptedAction SUBTRACTS points
  "rationale": "why this matters",
  "evidenceRefs": []
}
```

## The condition mini-language

Read-only. See [clinical-engine.md](../architecture/clinical-engine.md#the-condition-mini-language).
Accessors: `simTimeSec`, `painScore`, `rhythm`, `airway`, `breathing`,
`circulation`, `neuro`, `vitals.<name>`, `flag('id')`, `disclosed('id')`,
`actionCount('id')`.

## Validate

```bash
pnpm run validate:cases                                  # schema + cross-references
cd clinical-core && dotnet run --project Qaniva.Clinical.Cli -- validate <path>/case.json
```

## Golden paths (each case needs these as replay scripts)

ideal · delayed critical action · wrong-but-harmless · harmful/contraindicated ·
early disposition · AI-unavailable. Commit them under
`clinical-core/Qaniva.Clinical.Tests/Golden/` and lock the output with a golden file.

## Review

Open a PR with the case JSON + its golden scripts. A clinician reviews every
number and decision and records sign-off in `metadata.clinicalReview`. Software
review checks: schema valid, ids resolve, golden tests pass, no clinical logic
leaked into `presentationProfile` or client code.
