# Clinical review package — `<case_id>`

<!-- Generic template. Worked example: docs/clinical/cases/stemi/REVIEW.md.
Optimized for a clinician: everything reviewable from this file + BLUEPRINT.md,
never from JSON. -->

**CLINICAL STATUS: DRAFT — REVIEW REQUIRED.**
Reviewer: ______ · Credentials: ______ · Date: ______

Section-level verdicts are valid (**APPROVE / REQUEST CHANGE / REJECT** +
comment); only approved sections may be implemented; a REJECT on any safety
item blocks implementation. The drafting AI has no approval authority.

## 1. Case synopsis

<5–8 lines: who, where, the correct pathway, what goes wrong on delay, session
length, target learner.>

## 2. Section checklist

| # | Section | Verdict | Comments |
| --- | --- | --- | --- |
| S1 | Presentation + initial vitals | ☐A ☐RC ☐R | |
| S2 | History/exam + disclosure design | ☐A ☐RC ☐R | |
| S3 | Investigations (delays, prerequisites) | ☐A ☐RC ☐R | |
| S4 | Diagnostic asset intent + assist policy | ☐A ☐RC ☐R | |
| S5 | Medication table (drugs/doses/routes/gating) | ☐A ☐RC ☐R | |
| S6 | Harmful/unnecessary actions + penalty labels | ☐A ☐RC ☐R | |
| S7 | Core management pathway | ☐A ☐RC ☐R | |
| S8 | Timing windows (sim-design numbers) | ☐A ☐RC ☐R | |
| S9 | Deterioration graph + magnitudes | ☐A ☐RC ☐R | |
| S10 | Terminal states | ☐A ☐RC ☐R | |
| S11 | Accepted alternative pathways | ☐A ☐RC ☐R | |
| S12 | Scoring rubric + weights | ☐A ☐RC ☐R | |
| S13 | Debrief claims + teaching points | ☐A ☐RC ☐R | |
| S14 | Prebrief text | ☐A ☐RC ☐R | |
| S15 | Evidence-ledger spot check vs primary sources | ☐A ☐RC ☐R | |

## 3. Medication/action quick table

| Drug/action | Displayed dose/route | Case classification | Evidence | OK? |
| --- | --- | --- | --- | --- |

## 4. Standing review questions (keep all; add case-specific ones)

- Are the initial vitals realistic for this state?
- Is the diagnostic timeline appropriate?
- Are the core management decisions correct for the setting?
- Are medication choices/doses/routes appropriate?
- Are the contraindications complete *for this case*?
- Are deterioration transitions clinically plausible (as labeled compression)?
- Are alternative pathways accepted where guidelines support them?
- Are the harmful actions truly harmful (and the unnecessary ones merely
  unnecessary)?
- Are timing penalties clinically justified vs purely pedagogical (and labeled)?
- Does the debrief teach the stated objectives with only evidence-backed claims?
- Is anything unsafe, misleading, or likely to train a wrong reflex?
- Local fit (Turkey/ESC defaults, drug availability, setting realism)?

## 5. Sign-off

☐ APPROVED (→ CLINICALLY_APPROVED; record reviewer/date in
`metadata.clinicalReview`) · ☐ CHANGES REQUESTED · ☐ REJECTED

Signature: ______ Date: ______
