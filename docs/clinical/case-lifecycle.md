# Case lifecycle & directory standard

How a Qaniva case travels from idea to production and stays trustworthy after.
Extracted from the first production-case run (`stemi_anterior_001`, 2026-08-31).

## Lifecycle

```
IDEA → RESEARCHING → DRAFT → CLINICAL_REVIEW ⇄ CHANGES_REQUESTED
     → CLINICALLY_APPROVED → IMPLEMENTED → TECHNICAL_QA → BLIND_PLAYTEST
     → PUBLISHED → SURVEILLANCE → (RETIRED)
```

| Stage | Canonical artifact | Gatekeeper |
| --- | --- | --- |
| RESEARCHING | `research.md` + `evidence.yaml` | authoring skill's source rules |
| DRAFT | `BLUEPRINT.md` (+ asset specs) | self-audit (skill §research-quality audit) |
| CLINICAL_REVIEW / CHANGES_REQUESTED | `REVIEW.md` (section verdicts) | **a real clinician** — AI has no approval authority |
| CLINICALLY_APPROVED | signed `REVIEW.md` + `metadata.clinicalReview` fields | clinician signature recorded in repo |
| IMPLEMENTED | versioned `case.json` (`fixtures/<id>/v<n>/`) | schema + CLI validation |
| TECHNICAL_QA | 6 golden-path scripts + golden files, green CI | testing-and-golden-replay skill |
| BLIND_PLAYTEST | playtest notes (INACSL: pilot before use) | someone who didn't author it |
| PUBLISHED | backend `published` + `clinicalReview.status == approved` (QAN-011) | release process |
| SURVEILLANCE | guideline-version fields + review-due date | see below |
| RETIRED | case withdrawn from `GET /cases`; artifacts kept | product + clinical lead |

Clinical approval and technical QA are **distinct gates**: a clinician approves
medicine; QA approves determinism/regression. Neither substitutes for the other.

## Directory standard

```
docs/clinical/cases/<case-id>/
  research.md            # stage RESEARCHING+ (canonical during research)
  evidence.yaml          # machine-readable claim ledger (lives forever)
  BLUEPRINT.md           # stage DRAFT+ (canonical during authoring)
  REVIEW.md              # stage CLINICAL_REVIEW+ (canonical during review)
  <ASSET>_SPEC.md        # only when the case needs an asset (e.g. ECG)
  IMPLEMENTATION_SPEC.md # engine mapping + gap analysis (pre-approval)
```

After approval, the canonical artifact becomes the versioned
`packages/case-schema/fixtures/<case-id>/v<n>/case.json`; the docs folder stays
as provenance. Do not create files a case doesn't need.

## Evidence & versioning rules

- Every canonical clinical claim in a case traces to an `evidence.yaml` record
  (id, claim, source, year, retrievedAt, reviewRequired).
- A production case's `metadata` carries: guideline references + versions,
  `clinicalReview {status, reviewer, date}`, case `version`, and a
  **reviewDueAt** target (default: 24 months or on guideline change,
  whichever first — product may tighten).
- Guideline surveillance (manual for now, by design — no automation service):
  1. New guideline version released → search `evidence.yaml` files for the
     superseded source id.
  2. Affected cases → lifecycle back to CLINICAL_REVIEW (flagged, not unpublished,
     unless the change is safety-relevant — clinician call).
  3. Clinical update → new case `version` (new `v<n>/` folder, never in-place)
     → golden regression regenerated and reviewed → republish.
- A content change of any clinical number = version bump + re-review of the
  changed sections (section-level re-review is valid; `REVIEW.md` supports it).
