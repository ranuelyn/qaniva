# Clinical engine

Decision: [ADR-003](../adr/ADR-003-deterministic-engine-owns-clinical-truth.md).
Types: [domain-model.md](domain-model.md). This page is how to work on it.

## Projects

| Project | TFM | Role |
| --- | --- | --- |
| `Qaniva.Clinical.Core` | `netstandard2.1` | the engine — Unity-referenceable, no Unity deps |
| `Qaniva.Clinical.Cli` | `net10.0` | headless `validate` / `replay` / `golden` tool |
| `Qaniva.Clinical.Tests` | `net10.0` | xUnit: expression, determinism, behaviour, loader, golden |

`netstandard2.1` keeps the Core consumable by Unity 6 as a DLL. Modern C# is
available via small polyfills in `Compat/Polyfills.cs` (guarded to the netstandard
target).

## The condition mini-language

Used by `visibleWhen`, `preconditions[]`, `TransitionRule.when`,
`ScoringCriterion.stateConstraints[]`, `TerminalState.when`. Read-only, no side
effects.

```
expr    := or
or      := and ( "||" and )*
and     := unary ( "&&" unary )*
unary   := "!" unary | comparison
comparison := primary ( ("=="|"!="|"<"|"<="|">"|">=") primary )?
primary := number | 'string' | true | false
         | flag('id') | disclosed('id') | actionCount('id')
         | simTimeSec | painScore | rhythm | airway | breathing | circulation | neuro
         | vitals.<name>
         | ( expr )
```

`ExpressionEvaluator.EvaluateBool(expr, state)`. Extending the grammar is a
deliberate change with tests in `ExpressionEvaluatorTests`.

## Effects

`setFlag` / `clearFlag` · `disclose <factId>` · `setEnum <target> <value>`
(`airway`/`breathing`/`circulation`/`neuro`) · `setRhythm <value>` ·
`set <target> <number>` / `adjust <target> <delta>` where `target` is
`vitals.<name>` or `painScore`. Effects may not write `simTimeSec`.

**Conditional effects** are deliberately NOT an effect feature — express them as
a transition-rule pair (data-only, deterministic). Worked example
(nitrate harmful only while hypotensive, `stemi_anterior_001`): the action sets
a transient flag; a high-priority `once:false` rule applies the consequence when
`flag && vitals.sbpMmHg < 100` and clears the flag; a low-priority `once:false`
cleanup rule clears the flag otherwise — the flag never survives the rule pass.

## Results & assets (QAN-022 minimal)

`resultTemplates[] {id, text, assetId?}` + `resultAssets[] {id, kind:"image",
label, provenance{source, license, clinicalStatus, …}}` are case data. On an
accepted action the engine resolves `resultTemplateId` into
`ActionResult.ResultText/ResultAssetId/ResultAssetLabel` and diffs disclosures
into `NewlyDisclosedFacts` — presentation layers render these verbatim (Unity's
result banner + full-screen viewer loads the image from
`Resources/Qaniva/CaseAssets/<assetId>`). Broken template/asset references fail
at load (CaseLoader) and in `@qaniva/case-schema` semantics. Legacy cases
without a `resultTemplates` array keep free-form ids (explicit compatibility
rule). A diagnostic asset with
`provenance.clinicalStatus: placeholder_replacement_required` is not
clinically valid — the honesty is machine-checkable, and the engine passes the
status through `ActionResult` so the Unity viewer shows a persistent
"NOT a verified diagnostic tracing" note for any non-verified asset.

Transition rules may carry an optional learner-facing `debriefText` — the
authored causality line surfaced when the rule fires (timeline `stateChanges`
in the AttemptSummary). Presentation metadata like `presentationCue`: it never
affects state, scoring, hashes or goldens.

## Scoring / debrief outputs

`ScoringEngine` also emits `CriterionResults()` — per-criterion
`correct | delayed | missed | harmful | avoided` with awarded/max points and
credit time. Harmful criteria respect `stateConstraints` (state-dependent harm,
e.g. only while hypotensive); constraint-free harmful criteria stay
unconditional. Terminal outcomes:
`complete | partial | deteriorated | discharge | admit | death | aborted`
(generic vocabulary — no disease semantics in the enum). The AttemptSummary
sent to RN carries `criteria[]` (now incl. per-criterion `evidenceRefs` and
`acceptedActionLabels` for alternatives), the timeline with per-step
`stateChanges` causality texts, the case's `debrief{}` metadata and its
`references[]` — the Results screen renders a timing-aware, evidence-traceable
debrief without recomputing anything.

## Determinism rules (enforced by review + tests)

- No `DateTime.Now`, `Environment.TickCount`, `Guid.NewGuid` in engine logic.
- No `System.Random`; only the seeded `DeterministicRng`.
- Iterate sorted collections only (`SortedSet`, `SortedDictionary`, or explicit
  `OrderBy`). Never rely on `Dictionary`/`HashSet` order.
- Number formatting via `Hashing` helpers (fixed culture + precision).

## Tests

| File | Locks down |
| --- | --- |
| `ExpressionEvaluatorTests` | grammar + rejections |
| `DeterminismTests` | run A == run B; hash chain; seed-independence for non-stochastic cases |
| `EngineBehaviourTests` | invalid action ⇒ no state change; time-transition fires deterioration; visibility guard; harmful scoring; terminal complete/death |
| `CaseLoaderTests` | loads the demo fixture; rejects `fictional:false` and unsupported `schemaVersion` |
| `GoldenReplayTests` | `ideal_path` and `harmful_path` scripts vs committed golden JSON |

Golden files: `clinical-core/Qaniva.Clinical.Tests/Golden/*.golden.json`.
Regenerate after a *reviewed* intentional change:

```bash
cd clinical-core && UPDATE_GOLDEN=1 dotnet test
git diff clinical-core/Qaniva.Clinical.Tests/Golden   # review every line
```

## Headless CLI

```bash
cd clinical-core
dotnet run --project Qaniva.Clinical.Cli -- validate <case.json>
dotnet run --project Qaniva.Clinical.Cli -- replay   <case.json> <script.json>
dotnet run --project Qaniva.Clinical.Cli -- golden   <case.json> <script.json> --write <out.json>
```

## Syncing to Unity

```bash
scripts/sync-clinical-core-to-unity.sh    # publish DLL -> Assets/Qaniva/Plugins/ClinicalCore (git-ignored)
```

Then set the `QANIVA_HAS_CLINICAL_CORE` scripting define in Unity to switch
`SimulationBridgeController` from `StubClinicalRuntime` to the real `ClinicalRuntime`.
