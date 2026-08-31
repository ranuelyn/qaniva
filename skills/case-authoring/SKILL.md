# Skill: case-authoring

## Purpose

Take a clinical case from idea to clinician-approved, implemented,
golden-tested content — evidence-first. Formalized from the first production
case run (`stemi_anterior_001`, 2026-08-31).

## When to use

Creating a new case, bumping a case version, or changing any clinical
action/rule/number. ("Add anaphylaxis case" starts here, at stage 1 — not at
`case.json`.)

## Required inputs (read first)

- `docs/clinical/case-lifecycle.md` (stages, directory standard, versioning)
- `docs/clinical/templates/CASE_BLUEPRINT_TEMPLATE.md` + `CASE_REVIEW_TEMPLATE.md`
- Worked example: `docs/clinical/cases/stemi/` (all files)
- For implementation stage: `docs/clinical/case-authoring-guide.md`,
  `packages/case-schema/schema/case.schema.json`, demo fixture,
  `docs/architecture/clinical-engine.md`

## Non-negotiable rules

1. **Never author production clinical truth from model memory alone.** Every
   canonical clinical claim traces to an `evidence.yaml` record with source,
   year and retrieval date.
2. Source hierarchy: Tier A (current official society/government guidelines,
   simulation standards) > Tier B (official society summaries, national
   adoptions, official competitor docs) > Tier C (cross-check only, never
   canonical alone). **Verify guideline currency at execution time** — never
   assume a version named in an older prompt/doc is still current.
3. Guideline divergence is **explicit**: a divergence table row (both
   positions, jurisdiction relevance, proposed default, review flag) — never a
   silent choice.
4. **Clinical review is required before production publication.** The case
   stays `CLINICAL STATUS: DRAFT — REVIEW REQUIRED` until a real clinician
   signs `REVIEW.md`. The AI never claims approval.
5. Alternative correct pathways must be considered for every criterion; when
   guidelines support several, the criterion accepts them (`acceptedActions`
   list) instead of forcing one button.
6. Clinical harm is never invented for gamification. Efficiency penalties are
   allowed but labeled separately from patient-harm penalties. Every
   consequence carries evidence + reviewer approval.
7. The LLM never controls canonical simulation state (ADR-003/007); debrief
   text may rephrase, never add claims.
8. Never modify a clinical rule to satisfy a test, UI, or schema convenience —
   document the gap instead (IMPLEMENTATION_SPEC pattern).
9. Objectives first (INACSL): start from observable learner behaviors, not
   from "what patient should we simulate".
10. Diagnostic assets (ECG, imaging) are never fabricated from memory: write an
    `<ASSET>_SPEC.md` (pattern, layout, license policy, provenance record) and
    require clinician verification of the actual asset. Watch licenses:
    education-famous sources are often **CC BY-NC** (unusable commercially).
11. `metadata.fictional: true`; synthetic patients only.
12. Timing windows and deterioration timings in a blueprint are **sim-design
    derivations** from guideline anchors — label them so and surface them to
    the reviewer as their own question.

## Workflow by stage (see case-lifecycle.md)

### 1 RESEARCHING → `research.md` + `evidence.yaml`

Verify current guideline versions (search; check for successors) → answer the
domain question set (presentation, assessment, diagnostics, core management,
pharmacology, deterioration, disposition, post-acute) with sources → record
divergences → local (Turkey) fit → competitor/authoring patterns if useful →
one evidence record per canonical-candidate claim (`reviewRequired: true` for
anything clinical; note the retrieval path when a number came via a summary).

### 2 DRAFT → `BLUEPRINT.md` (+ asset specs)

Narrow to ONE scenario (unambiguous educational value, clear timing pressure,
several meaningful decisions, ≥1 accepted alternative, ≥1 plausible
harmful/delayed path, 8–12 min mobile session). Define scope + out-of-scope
explicitly. 3–5 measurable objectives → presentation/triage/history buckets/
exam → investigations (delays!) → actions with class
(CRITICAL/RECOMMENDED/NEUTRAL/UNNECESSARY/HARMFUL) → criteria with timing +
causality + alternatives → modest deterministic deterioration graph → terminal
states (only justified ones) → scoring derived from the objectives → debrief +
prebrief. Fill the blueprint template; mark every reviewer decision **[Qn]**.

Then **self-audit as a skeptic**: outdated guidelines? secondary overriding
primary? unsupported timing/dose claims? divergence hidden as certainty?
US-only assumptions? scope creep? punitive scoring without justification?
missing alternatives? debrief claims without evidence? memory-only facts?

### 3 Gap analysis → `IMPLEMENTATION_SPEC.md`

Map every blueprint element to current engine constructs; mark READY /
REVIEW_REQUIRED / ENGINE_GAP / OUT_OF_SCOPE. Verify engine semantics against
source, not from memory (e.g., `TransitionRule.delaySec` already covers delayed
results/callbacks; timing windows already decay linearly). Do not expand the
engine during authoring.

### 4 CLINICAL_REVIEW → `REVIEW.md`

Fill the review template (synopsis, section checklist, medication quick table,
questions). Hand off to a clinician. Iterate CHANGES_REQUESTED on changed
sections. **STOP here — implementation is a separate, post-approval task.**

### 5 IMPLEMENTED (only after approval)

Copy the demo fixture → `fixtures/<id>/v1/case.json`; encode the approved
blueprint; `pnpm run validate:cases`; CLI `validate`; record
`metadata.clinicalReview` from the signed review.

### 6 TECHNICAL_QA

6 golden-path scripts (ideal, delayed-critical, wrong-harmless, harmful,
early-disposition, AI-unavailable) + `UPDATE_GOLDEN=1 dotnet test` + line-by-line
golden review (testing-and-golden-replay skill). Then BLIND_PLAYTEST before
PUBLISHED.

## Validation

- Research/draft stages: prettier/lint on the docs; evidence ids referenced by
  the blueprint all exist in the ledger; every [Qn] appears in REVIEW.md.
- Implementation stages: `pnpm run validate:cases` 0 failures; engine CLI
  validate clean; goldens green; no dangling ids.

## Definition of done (per stage)

Stage artifacts exist per the directory standard; status headers correct;
review gate respected; for implementation — schema+semantic valid, 6 goldens,
sign-off recorded in metadata.

## Common failure modes (observed + inherited)

- Starting at `case.json` and back-filling evidence afterwards.
- Trusting a fetched summary's exact numbers without flagging the retrieval
  path (summaries garble doses).
- Converting every guideline recommendation into an action (maximal-complexity
  case) instead of serving the objectives.
- A single-script "guess the author" pathway with no accepted alternatives.
- Punishing with death/harm where the evidence only supports "delay is worse".
- Rubric points that can't be earned (`acceptedActions` never visible).
- A deterioration rule with `once:false` re-firing every tick.
- `=` instead of `==` in `when` expressions.
- Debrief narrative leaking the answer pre-terminal.
- Forgetting the version bump on any clinical-number change.
