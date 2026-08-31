# First-case retrospective — `stemi_anterior_001` implementation (2026-08-31)

What the first blueprint→implementation run taught the case factory. Status at
write time: case implemented as data, MVP DEMO APPROVED, clinical validation
PENDING (QAN-012C remains the gate for any "clinically approved" claim).

## What the blueprint got right

- **The action/criterion/transition tables mapped almost 1:1 onto case.json** —
  authoring the JSON took under an hour because every number, gate and window
  was already decided and review-flagged. The blueprint tables are the right
  level of detail; keep them.
- **The IMPLEMENTATION_SPEC gap analysis was accurate**: `delaySec` covered
  delayed results/consult callbacks exactly as predicted; the
  transition-rule pattern for the nitrate conditional effect worked unchanged
  (`ntg_bp_response` priority 220 + `ntg_processed` cleanup rule, both
  `once:false`, flag cleared within one rule pass).
- Declaring OUT-OF-SCOPE explicitly prevented every "while we're here"
  temptation during implementation.

## What was ambiguous / decided during implementation

- Blueprint said treatments gate "after ecg_done" without choosing
  visible-hidden vs visible-disabled. Chose **visible+disabled with the engine's
  reason** (teachier). Template now asks for the explicit choice.
- The rhythm string: blueprint draft said `sinus_tachycardia_st_elevation`,
  which would leak the diagnosis through the vitals bar — implemented as
  `sinus_rhythm`. Lesson: audit every displayable string for spoilers.
- Post-deterioration exam text (state-dependent results) was dropped per
  GAP-3's "static acceptable v1" — recorded as an implementation deviation, no
  clinical impact (reviewer question unaffected).

## Engine/schema gaps that materialized (all closed generically)

- Terminal outcome vocabulary: added `partial` + `deteriorated` (schema, zod
  contract, RN labels). No engine code change — outcomes were already strings.
- Result content: actions had `resultTemplateId` but nothing consumed it.
  Added generic `resultTemplates` (+ optional `assetId`) and `resultAssets`
  (with license/clinical-status provenance) to the schema, engine resolution
  into `ActionResult`, and the Unity result banner/viewer. ECG-agnostic by
  construction (X-ray/CT reuse it as-is).
- Harmful criteria ignored `stateConstraints` (nitrate-only-when-hypotensive
  needed them). One-line generic engine fix + tests; demo case unaffected.
- Per-criterion debrief outcomes existed internally but never left the engine:
  added `CriterionResults()` → AttemptSummary `criteria[]` + `debrief{}` so RN
  renders timing-aware debrief (correct/delayed/missed/harmful/avoided).

## What consumed the most effort

1. **The rigged patient (QAN-020)** — asset research, Blender generator
   iterations (Skin-modifier island roots!, joint overlaps, supine orientation
   trial-and-error against PlayMode captures, blanket-vs-feet composition).
   Roughly half the sprint's iteration cycles. The generator script is now
   reproducible, but budget real time for any art replacement.
2. Full Unity batch cycles (CreateAll + PlayMode) at ~2–4 min each make visual
   iteration slow; captures via PlayMode remain the only honest preview.
3. Contract ripples: one summary change touches zod + C# DTO mirror + parity
   tests + API test fixture + RN fake bridge. The structural parity test
   caught every miss — keep it.

## Asset pain points

- LITFL (CC BY-NC) being unusable was known from research — the committed
  placeholder route (deterministic generator + on-image watermark +
  `placeholder_replacement_required` provenance in case data) worked well and
  keeps the honesty machine-checkable.
- Poly Pizza integration in the installed blender-mcp addon was missing;
  first-party Blender modeling was faster than fighting downloads and keeps
  zero licensing surface. Production-quality art remains a purchase decision
  (see asset manifest).

## Scoring/golden lessons

- Designing the six paths **in the blueprint** (with expected sim-times) made
  the goldens verifiable by hand: ideal=alternative=88 proved pathway
  equivalence arithmetically, delayed lost exactly the two timing decays.
- Review generated goldens against *predicted* numbers, not just eyeball —
  the 79.5 delayed-path total was confirmable on paper (11.5 + 20 partials).
- `waitSec` steps in QA scripts are fine for timing paths; the learner-facing
  UI never needs a wait button (actions advance time).

## For the next case (anaphylaxis) do differently

- Write the six golden scripts (with expected scores) into the BLUEPRINT
  itself, not just "pathways" prose.
- Specify visible-vs-disabled gating per action in the blueprint.
- Name every learner-visible string field (rhythm, labels, templates) in a
  "spoiler audit" checklist before implementation.
- Reuse `adult_rigged_v1` + `ed_resus_v1` — a new case should need zero art.
- Skill decision recorded: implementation stays a **stage of case-authoring**
  (no separate case-implementation skill) — the staged skill remained legible
  during real use; revisit only if the implementation stage outgrows one
  screen of instructions.
