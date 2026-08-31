# MVP physician-demo readiness — post-sprint report

**Date:** 2026-08-31 · **Baseline:** [STEMI_MVP_AUDIT.md](STEMI_MVP_AUDIT.md)
(tree `2f3b662`) · **This report:** after the two-track sprint (Track A
physician-demo readiness, Track B anaphylaxis case-factory validation).
Evidence images: [`media/07…14`](media/) — all from live simulator runs of this
tree (iPhone 16 Pro simulator, real Unity runtime, real engine, E2E driver
pressing real UI controls; the E2E route now passes through the real Briefing
screen).

## What shipped

**Track A — demo readiness**

- **Results/debrief rebuilt** to render the rubric's own semantics
  (`media/10`): Critical decisions (done/missed with time + points) · Harmful
  (safety, `category != efficiency`) · **Unnecessary (efficiency) as its own
  section** (the audit's conflation fixed) · Correct-but-delayed with credit
  lost · Missed · **Accepted alternatives** (from `acceptedActionLabels`) ·
  Done well · teaching points · common errors · **References** + per-criterion
  **Evidence: EV-…** ids (new contract fields, parity-tested) · replay hash ·
  **Replay button** (fresh attemptId+seed; history preserved).
- **Causality**: new generic `transitionRules[].debriefText` (authored,
  deterministic; never affects state/hashes) → timeline `stateChanges` → a
  "What happened to the patient" section + inline ⚠ timeline rows. Live proof:
  `media/13` ("Intramuscular epinephrine took effect — the breathing eased and
  the blood pressure recovered", 02:55).
- **Attempt persistence + progress**: `AttemptStore` over AsyncStorage
  (injected-KV abstraction, 5 unit tests incl. failure reporting and
  no-overwrite-on-replay); Results auto-saves; Cases shows
  attempted/completed/best/attempts; Briefing shows recent attempts +
  "Play again" (`media/14` — survives full app relaunch).
- **Blind-playtest flow**: Home → Cases → Briefing → Simulation → Results →
  Replay works with **no env vars** (E2E autostart only activates when
  `EXPO_PUBLIC_E2E_AUTOSTART` is set at bundle time, and now routes through
  the real Briefing screen). Friendly failure states (case-load/bridge codes
  mapped to plain language; persistence failure = small note, never blocking);
  the fake-bridge fallback stays loudly labeled.
- **Analytics**: existing typed schema extended
  (app_open/case_viewed/case_abort/debrief_viewed + outcome vocabulary) and
  wired through the app (console sink in dev, noop in prod — no provider yet).
  **Crash reporting: deliberately NOT added** this sprint (new native dep +
  DSN secret handling would destabilize the demo build; recorded as QAN-018
  next-step).
- **Patient visual pass** (`media/08` vs the baseline `media/02`): draped
  blanket mesh (modeled in Blender, leg contours + fabric edges — replaces the
  box), warm saturated skin tints (washout fixed), ears added / brow-fin
  artifacts removed, softened key light. Same prefab contract; ~8.9k tris.
- **ECG placeholder policy preserved**: watermark intact; the asset's
  `provenance.clinicalStatus` now flows engine→viewer, which shows a
  persistent "NOT a verified diagnostic tracing" note for any non-verified
  asset (a placeholder can no longer be presented as verified content).

**Track B — anaphylaxis (`anaphylaxis_food_001` v1, `mvp_demo_approved`,
clinical validation PENDING)**

Full lifecycle run: research + ledger (WAO 2020/GA²LEN 2024, RCUK 2021,
AAAAI/ACAAI 2023, EAACI 2021, Turkish AİD 2018; divergences DA-1..3 explicit)
→ compact blueprint + review package → case data (22 actions, 10
evidence-referenced criteria, IM-vs-IV route harm with canonical surge,
state-constrained reassessment, two alternatives-equivalence criteria, visible
epinephrine response, deterioration → peri-arrest `deteriorated`) → 6 goldens
**green on first generation** at the blueprint's predicted scores
(80 / 55.375 / 80 / 80 / 68 / 10) → 11 engine tests → live simulator run
(`media/11–13`).

**Case-factory verdict: NONE — zero new engine/schema/Unity capability was
required** (see `docs/clinical/cases/anaphylaxis/RETROSPECTIVE.md` incl. the
friction log). QAN-006b (parameter input) deliberately deferred: the route
choice is two labeled actions, which is at least as teachable as a picker.

## Readiness re-score (same dimensions as the baseline audit)

| Area | Before | After | What moved |
| --- | --- | --- | --- |
| Technical architecture | 90 | 91 | briefing/manifest content now drift-guarded data; evidence/causality flow end-to-end |
| Deterministic simulation | 95 | 95 | unchanged (nothing needed) |
| Case-authoring system | 85 | **93** | validated by a real second case with zero new infrastructure |
| Clinical content maturity | 40 | **45** | second evidence-backed case; still no clinician review (the gate) |
| 3D presentation | 70 | 72 | draped blanket, lighting |
| Patient visual quality | 45 | **60** | washout fixed, blanket, face cleanup — improved EARLY MVP (production art still a purchase decision) |
| Simulation interaction UX | 70 | 72 | provenance note; result readability |
| Results/debrief UX | 60 | **85** | taxonomy, causality, evidence, alternatives, replay, persistence |
| Testing | 90 | 91 | +26 tests (engine 84, mobile 21) at same rigor |
| Release readiness | 30 | 38 | persistence + analytics events; still no device run/crash reporting/TestFlight |

**TECHNICAL MVP READINESS: ~85%** (was ~80; limiter unchanged: device
validation + backend upload). **PHYSICIAN DEMO READINESS: ~80%** (was ~65;
moved by the Results rebuild + causality + visuals + a second case proving
"product, not prototype"; limiter: clinician review is still pending and the
demo must say so, plus final patient-art decision). **CLOSED STUDENT BETA
READINESS: ~45%** (was ~35; limiters unchanged in kind: mandatory clinician
approval, verified ECG, device+distribution, upload/backup of attempts).

## Honest capture gaps

The Cases-list screen (both cases + progress rows) and Home-after-completion
are reachable only by tap; no headless tap tooling exists on this host, so
those two screenshots are not included. Their logic is covered by the catalog
drift-guard test, the AttemptStore tests, and `media/14` (persisted history on
the Briefing screen, which uses the same store).

## Top remaining MVP items (re-derived)

| # | P | Item | Blocks physician demo? | Blocks student beta? |
| --- | --- | --- | --- | --- |
| 1 | P0 | Clinician review of BOTH cases (QAN-012C-class) + post-review v2s | no (the demo collects it) | **yes** |
| 2 | P0 | Clinician-verified ECG asset (placeholder + provenance note stay until then) | with spoken disclaimer, no | **yes** |
| 3 | P0 | Production patient-art purchase decision (asset manifest RECOMMENDED PURCHASE) | partially | yes |
| 4 | P1 | QAN-008 physical-device run (still DEFERRED; schedule before TestFlight) | no | **yes** |
| 5 | P1 | Attempt upload (QAN-007) — local store is the queue source | no | yes |
| 6 | P1 | Crash reporting (QAN-018) + real analytics sink | no | yes |
| 7 | P1 | Blind playtest by a non-author (flow is now shortcut-free) | helpful | yes |
| 8 | P2 | QAN-006b parameter input (first case that needs numeric titration) | no | no |
| 9 | P2 | Unity CI (QAN-024) | no | yes (at scale) |
| 10 | P2 | Data-driven e2e path table + backend-served briefings (content-in-code hygiene) | no | no |
