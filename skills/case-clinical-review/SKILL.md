# Skill: case-clinical-review

## Purpose

Guide the clinical review gate for a case: verify evidence, adjudicate design
questions, approve/reject per section, and keep approvals versioned. This skill
serves the human clinician (and any agent preparing/managing a review); **no AI
may issue the approval itself**.

## When to use

A case reaches CLINICAL_REVIEW or CHANGES_REQUESTED (`docs/clinical/case-lifecycle.md`);
a guideline update flags a published case; any clinical number changes.

## Required inputs

- The case's `REVIEW.md`, `BLUEPRINT.md`, `evidence.yaml` (+ asset specs)
- `docs/clinical/templates/CASE_REVIEW_TEMPLATE.md` (structure being followed)
- Primary sources listed in the ledger (the reviewer verifies against these,
  not against the AI's summaries)

## Non-negotiable rules

1. The approver is a qualified clinician identified by name/credentials/date in
   `REVIEW.md` and mirrored into `metadata.clinicalReview` at implementation.
2. Section-level verdicts are valid; **only approved sections may be
   implemented**; a REJECT on a safety item blocks the case.
3. Evidence verification means opening the primary source: check guideline
   version currency, spot-check every dose/threshold/timing the case displays,
   and every record whose `retrievalPath` notes a secondary summary.
4. Harmful classifications and penalties need explicit confirmation that the
   harm is real (not gamification); efficiency penalties stay labeled non-harm.
5. Timing windows/deterioration timings are reviewed as *labeled pedagogical
   compression* — the reviewer may accept, adjust, or reject the framing.
6. Alternative-pathway review: for each criterion, confirm the accepted-action
   set matches guideline-supported practice (neither forcing one button nor
   accepting substandard care).
7. Approval is versioned: it binds to the reviewed case `version` + guideline
   versions in the ledger. Any later clinical change = version bump +
   re-review of changed sections.

## Review workflow

1. Read synopsis + blueprint end-to-end once, as a clinician (realism pass).
2. Work the section checklist S1..S15 in `REVIEW.md`; comment every RC/R.
3. Verify the medication quick table row by row against primaries.
4. Answer every standing + case-specific question (Q*/OQ-*).
5. Walk the deterioration graph and terminal states for plausibility + safety.
6. Check the debrief teaching points: objective-aligned, evidence-cited only.
7. Sign: APPROVED / CHANGES REQUESTED / REJECTED (+ date, credentials).
8. On approval, the implementing engineer copies reviewer/date/status into
   `metadata.clinicalReview` — the review file stays in the case folder as
   provenance.

## Re-review (surveillance path)

New guideline version → ledger records citing the superseded source are listed
→ reviewer re-adjudicates only affected sections → version bump on any change
→ golden regression re-run and reviewed.

## Done criteria

Every section has a verdict; every question has an answer; sign-off block
complete; open items converted into CHANGES_REQUESTED tasks; nothing
implemented from unapproved sections.

## Common failure modes

- Reviewing the AI's summaries instead of the primary sources.
- Whole-case rubber-stamp instead of section verdicts (loses the ability to
  implement the safe subset while contested sections iterate).
- Accepting unlabeled pedagogical timings as clinical claims.
- Approving a dose "range" while the case displays a specific number nobody
  checked.
- Forgetting that approval binds to a version — silent in-place edits after
  sign-off.
