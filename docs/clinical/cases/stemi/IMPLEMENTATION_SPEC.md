# Implementation spec (draft mapping) — `stemi_anterior_001`

**Not an implementation.** This maps [BLUEPRINT.md](BLUEPRINT.md) onto the
*current* engine/schema so engineering sees every gap **before** clinical
approval. Nothing lands in `clinical-core/` or `packages/case-schema/` from this
sprint. Row status: **READY** (current engine supports as data) ·
**REVIEW_REQUIRED** (clinical decision pending) · **ENGINE_GAP** (needs schema
and/or engine work) · **OUT_OF_SCOPE**.

Engine facts verified against source (2026-08-31): timing windows apply linear
decay between `fullCreditBeforeSec` and `zeroCreditAfterSec`
(`ScoringEngine.TimingMultiplier`); `TransitionRule.DelaySec` schedules a rule's
effects at `simTime + delay` after its condition first holds
(`Simulation.cs:355`); `stateConstraints` are expression-checked at scoring time;
harmful criteria subtract points (demo-proven); `acceptedActions` is a list
(alternative-equivalence works today); terminal outcomes enum:
`complete | discharge | admit | death | aborted`.

## Mapping table

| Blueprint element | Engine construct | Status | Notes |
| --- | --- | --- | --- |
| Case identity / metadata / prebrief | `metadata`, `learningObjectives`, briefing via RN screen | READY | prebrief text lives with the case; RN Briefing screen already renders case metadata |
| `presentationProfile` | existing block → `ed_resus_v1` / `adult_neutral_v1` | READY | **no new Unity scene or room needed** |
| Initial state + vitals | `initialState` | REVIEW_REQUIRED | values Q1; `rhythm` string is free-form (display-only today) |
| History disclosure buckets | `hiddenFacts` (`on_ask` / `on_order_result`) + `disclose` effects | READY | |
| State-dependent exam results | `resultTemplateId` per action | ENGINE_GAP-3 (minor) | result templates are static ids today; state-dependent result *text* needs either two actions gated by state or template selection by expression. Smallest fix: allow `resultTemplateId` variants keyed by a `when` expression. Alternative: accept static text v1 (drop the post-T1 exam flavor) — decision at implementation, not clinical |
| Delayed troponin result | rule `when: flag('troponin_sent')`, `delaySec: 1200`, effects `disclose troponin_result` | **READY (data-only)** | nuance: sim time advances only on actions, so the result "arrives" at the first action after the threshold — acceptable and deterministic; document in the case |
| Consult callback (+60 s) | same `delaySec` mechanism | READY | |
| Action gating (`ecg_done`, `iv_access`, `cath_activated`) | `preconditions` / `visibleWhen` + availability projection | READY | disabled-reason strings surface in the existing UI |
| P2Y12 equivalence (ticagrelor ≡ prasugrel) | one criterion, `acceptedActions: [both]` | READY | clopidogrel credit = OQ-3 (REVIEW_REQUIRED) |
| Timing criteria with decay (C-ECG, C-ASA, C-CATH, C-MON) | `timingWindow` | READY | window values REVIEW_REQUIRED (Q4) |
| Harmful penalties (NSAID, lytic) | `harmful: true` criteria | READY | classification Q7 REVIEW_REQUIRED |
| Efficiency penalty labeled non-harm (C-NoO2) | `harmful:true` + `category:"efficiency"` | READY | the *label* separation is a debrief-rendering concern; category field exists |
| Nitrate harmful-iff-SBP<100 **scoring** | criterion `stateConstraints: ["vitals.sbpMmHg < 100"]` | READY-with-care | constraints evaluate on **post-action** state — if the nitrate effect itself lowers SBP the constraint must be authored against the right side of the effect; verify in golden tests |
| Nitrate **conditional effect** (drop SBP only when already <100) | unconditional `effects[]` today | **ENGINE_GAP-1** | needs conditional effects (`effects[].when`) or a paired transition rule (`when: flag('ntg_given') && vitals.sbpMmHg < 100`, once, delay 0 → adjust) — the transition-rule workaround is data-only and deterministic; prefer it for v1 |
| Deterioration T1/T2 | `transitionRules` (time + flag conditions, priorities, `presentationCue`) | READY | magnitudes/timings Q4 REVIEW_REQUIRED |
| Terminal `handoff_cath_lab` (SUCCESS) | outcome `complete` | READY | |
| Terminal `handoff_after_deterioration` (PARTIAL_SUCCESS) | nearest: `complete` + distinct label + score | **ENGINE_GAP-2 (schema)** | outcome enum lacks a partial/deteriorated value. Options: extend enum (`deteriorated`, `failed`) — small schema+RN change; or ship v1 with `complete` + label + rubric differentiating. Clinical meaning is preserved either way; product decides |
| Terminal `vf_arrest_takeover` (FAILED, not death) | nearest: `death` (wrong) or enum extension | **ENGINE_GAP-2** | same enum extension covers it (Q10 approved the non-death framing) |
| Terminal `discharged_stemi` | outcome `discharge` + harmful scoring | READY | Q8 |
| ECG image asset in result | `resultTemplateId` text only today | **ENGINE_GAP-4** = existing **QAN-022** (case media pipeline) | v1 fallback: text-described ECG is NOT acceptable for LO-1 — QAN-022 (at least "bundled static image referenced by template id") becomes a hard dependency of implementation |
| Difficulty-scaled ECG assist | no difficulty concept in schema | ENGINE_GAP-5 / OUT_OF_SCOPE v1 | ship core difficulty only; revisit with product |
| Formative differential recording | action with no criterion; timeline records it | READY | debrief reads from timeline |
| Dose entry by learner | `params` exist; picker UI = QAN-006b | OUT_OF_SCOPE v1 | doses are displayed in labels only |
| Debrief structure | `debriefMetadata` + timeline + criteria | READY (data) | LLM rephrase-only rule already enforced by AI gateway design |

## Gap summary (prioritized for the approved case only)

| Gap | What | Type | Needed for v1? |
| --- | --- | --- | --- |
| GAP-4 / QAN-022 | media (ECG image) referenced from a result | engine+pipeline | **YES** — minimal form: bundled image keyed by `resultTemplateId` |
| GAP-2 | terminal outcome vocabulary (`deteriorated`/`failed` or similar) | schema (+RN result screen mapping) | YES (small), unless product accepts label-only differentiation |
| GAP-1 | conditional effects | engine — **avoidable** via transition-rule pattern | NO (workaround is clean, deterministic, data-only) |
| GAP-3 | state-dependent result text | engine (minor) | NO (static text acceptable v1) |
| GAP-5 | difficulty modes | schema+product | NO (deferred) |

Everything else the blueprint needs is **pure case data** against the current
engine. Do not implement any gap before the clinical review lands — the review
may change what is needed (e.g., Q5 dropping UFH removes nothing engine-wise;
Q7 changing lytic classification is data-only).
