# Case blueprint — `<case_id>`

<!-- Generic template — works for anaphylaxis, DKA, septic shock, stroke,
tension pneumothorax, … without structural change. Delete guidance comments.
Worked example: docs/clinical/cases/stemi/BLUEPRINT.md -->

**CLINICAL STATUS: DRAFT — REVIEW REQUIRED** (see REVIEW.md). Evidence IDs
refer to `evidence.yaml`. Reviewer-decision markers: **[Q*n*]**.

## Case identity

| Field | Value |
| --- | --- |
| Provisional case ID | |
| Title (learner-facing — no diagnosis spoiler) | |
| Specialty | |
| Target learner / assumed prior knowledge | |
| Difficulty | |
| Estimated duration / sim-clock policy | |
| Environment (`roomKey` — reuse an existing room unless impossible) | |
| Patient visual profile (`patientVariant`, start cue) | |
| Fictional | `true` (always) |

**Case scope:** <one sentence: the decision arc being trained>
**Out of scope (deliberate):** <list — complexity excluded on purpose>

## Learning objectives (3–5, observable behavior, each maps to ≥1 criterion)

| ID | Objective (behavior + condition + standard) | Criteria |
| --- | --- | --- |

## Patient presentation

Demographics · chief complaint · context/onset · risk factors. (Synthetic only.)

## Triage

Initial note (learner-facing) · initial vitals **[Q]** · initial appearance
(→ visual state mapping).

## History (disclosure design)

| Bucket | Facts |
| --- | --- |
| Spontaneous | |
| On ask | <include the negatives later decisions depend on> |
| Intentionally irrelevant | |
| Hidden until relevant | |

## Physical exam

| System | Action | Result | State dependency |
| --- | --- | --- | --- |

## Initial patient state (engine terms)

`vitals {...}` · rhythm · airway/breathing/circulation/neuro · pain · flags.

## Investigations

| Action | Result | Delay | Prereq | Visibility | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- |

<!-- Delayed results: model with a transition rule (`when: flag(...)` +
`delaySec`) — see stemi IMPLEMENTATION_SPEC. Never reveal the answer for free. -->

## Key diagnostic asset(s)

Intended finding · what the learner must infer · assist-by-difficulty policy ·
pointer to `<ASSET>_SPEC.md` (license + clinician-verification requirements).
Never fabricate a diagnostic image from model memory.

## Actions (by UI category: Patient / Examine / Orders / Treat / More)

Per action: `actionId` · label (dose/route displayed if a medication) · class
(CRITICAL/RECOMMENDED/NEUTRAL/UNNECESSARY/HARMFUL) · visibility/precondition ·
timeCost · canonical effect · result · criteria · evidence IDs.

## Criteria (timing + causality — the Qaniva differentiator)

| criterionId | Clinical goal | Accepted actions (alternatives!) | Ideal | Acceptable | Late | Miss consequence | State-dependent consequence | Points | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |

<!-- All windows are sim-design numbers derived from guideline anchors — label
them so, and put them before the reviewer as their own question. Efficiency
penalties are labeled non-harm, separately from patient-harm penalties. -->

## Accepted alternative pathways

Per criterion: primary action · alternatives · equivalence conditions · timing
differences · state conditions. If multiple guideline-supported pathways exist,
represent the criterion, don't force one button.

## Differential diagnosis design

High-value plausible / reasonable-lower / clearly inappropriate. State whether
selection is scored or formative (beware click-everything gaming).

## Deterioration graph (modest, deterministic — only what the LOs need)

ASCII diagram + table: transitionId · condition · time · source→target · vital
changes · visual cue · action changes · terminal? · evidence · review flag.
Label pedagogical time compression explicitly.

## Terminal states

| id | Outcome (SUCCESS/PARTIAL_SUCCESS/DETERIORATED/FAILED/DEATH — use only what's justified) | Trigger | Notes |
| --- | --- | --- | --- |

## Scoring design

Per dimension: points · why it exists (→ LO) · what earns · what loses ·
safety-critical vs efficiency penalties labeled. Weights derive from the LOs —
never inherited from another product or case.

## Debrief design

Critical decisions · missed/delayed/unnecessary/harmful actions · timeline ·
alternative correct pathways · key teaching points (each cites an EV id) ·
evidence references · replay suggestions. Every statement traces to
timeline + criterion + evidence. LLMs rephrase; they never add claims.

## Prebrief / briefing (no spoilers)

Role · location · resources · handoff/triage note · expected task ·
assumptions/fiction contract.

## Post-acute elements

What was researched and deliberately excluded.
