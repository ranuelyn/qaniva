# Second-case retrospective — `anaphylaxis_food_001` (case-factory validation)

Date: 2026-08-31. The point of this case was double: real content AND proof
that the STEMI-derived authoring system is reusable.

## Case-factory verdict: **NONE** (new infrastructure required)

Zero engine changes, zero schema changes, zero Unity presentation changes were
needed for the case itself. Everything the blueprint asked for mapped onto
existing primitives: un-gated treatments (clinical diagnosis) = empty
preconditions; route harm = two labeled actions + a consequence rule; visible
improvement = a delayed rule with vitals deltas; reassessment-after-treatment
= a non-harmful criterion with a `stateConstraints` flag check; two
alternatives-equivalence criteria = `acceptedActions` lists; peri-arrest
takeover = the generic `deteriorated` outcome. All six goldens matched the
blueprint's predicted scores **on first generation** (80 / 55.375 / 80 / 80 /
68 / 10).

The only "new" work outside case data: catalog + briefing entry in the RN
bundled catalog and one e2e-driver path row — both are data rows in existing
tables (and the catalog is now drift-guarded by a test against the fixture).

QAN-006b (parameter input) was **deliberately not built**: modeling IM vs IV
as two labeled actions puts the route choice in front of the learner more
visibly than a picker would. Deferral stands until a case needs numeric dose
titration. (Reviewer question Q-B.)

## Friction log (classified)

| Friction | Class | Note |
| --- | --- | --- |
| Catalog/briefing lives in RN code, not case data | PER-CASE (small) | mitigated with the drift-guard test; a real manifest pipeline is backlog hygiene (move when the backend serves briefings) |
| e2e driver ideal-path table is per-case C# data | PER-CASE (small) | acceptable while cases are few; make data-driven before case #4 |
| Designing expected golden scores by hand | HUMAN CLINICAL JUDGMENT + PER-CASE | it is also the QA superpower — keep (verifiable-by-hand goldens caught nothing this time because nothing was wrong) |
| Wait-step arithmetic for timed rules (delaySec fires on the NEXT step) | ONE-TIME (learning) | documented in clinical-engine.md; second case cost ~0 |
| No per-case Unity test needed | — (positive) | the generic presentation suite covered the new case with no additions |
| Evidence-per-criterion validator forces ledger discipline | positive friction | caught one missing record during STEMI; none here |
| Blueprint compactness | SKILL note | half-size dossier/blueprint was enough for case #2 — the templates over-specify once the process is proven |

## Time distribution (relative, this case)

Research+ledger ~35% · blueprint+review docs ~20% · case JSON ~15% · goldens+
tests ~15% · RN/e2e glue ~5% · docs ~10%. Unity/presentation: **0%**.
