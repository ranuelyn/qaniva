# STEMI case blueprint — `stemi_anterior_001`

**CLINICAL STATUS: MVP DEMO APPROVED — CLINICAL VALIDATION PENDING.** The
project owner authorized implementing this draft as an INTERNAL/DEMO
educational prototype (2026-08-31); it is NOT clinically approved until a
clinician signs [REVIEW.md](REVIEW.md).
Every clinical value below is provisional. Evidence IDs refer to
[`evidence.yaml`](evidence.yaml); design questions marked **[Q*n*]** map to the
review checklist. Implemented as versioned case data
(`packages/case-schema/fixtures/stemi_anterior_001/v1/`) under the MVP-demo
authorization; implementation deviations are listed at the end of this file.

---

## Case identity

| Field | Value |
| --- | --- |
| Provisional case ID | `stemi_anterior_001` (v1) |
| Title (learner-facing) | "Crushing chest pain in a 54-year-old" (no diagnosis spoiler) |
| Specialty | emergency_medicine |
| Target learner | clinical-year medical student / intern / early EM resident |
| Assumed prior knowledge | basic ECG reading, ACS pharmacology names, ED workflow |
| Difficulty | core (introductory production case) |
| Estimated duration | 8–12 min real time; sim clock 1:1 (engine precedent) |
| Environment | `ed_resus_v1` (existing room; **no new scene required**) |
| Patient visual profile | `adult_neutral_v1`, start `distress_mild` |
| Fictional | `true` — synthetic patient, no real data |

**Case scope:** recognition → first ECG → guideline initial therapy → primary-PCI
decision + cath-lab activation → handoff. Single room, single patient, PCI-capable
center, diagnostic first ECG.

**Out of scope (deliberate):** fibrinolysis pathway/transfer geometry, ACLS/code
management, cardiogenic shock management, RV/posterior/LBBB patterns, serial-ECG
diagnostics, multivessel/complete-revascularization decisions, cath-lab procedure
itself, post-PCI/CCU care, secondary prevention, beta-blocker initiation (D4),
morphine–P2Y12 interaction effects (EV-STEMI-039), dose-entry UI (doses are
displayed, not typed — QAN-006b lands later).

## Learning objectives (observable behavior; each maps to ≥1 criterion)

| ID | Objective | Criteria |
| --- | --- | --- |
| LO-1 | Obtain a 12-lead ECG within 10 minutes of arrival for acute chest pain and identify anterior ST-elevation MI. (EV-STEMI-010, -014) | C-ECG, C-DDX |
| LO-2 | Initiate guideline-based initial medical therapy — aspirin loading plus appropriate adjuncts — without delaying reperfusion. (EV-STEMI-030, -031) | C-ASA, C-P2Y12 |
| LO-3 | Select primary PCI and activate the cath lab within the target window at a PCI-capable center. (EV-STEMI-020, -021) | C-CATH, C-DISPO |
| LO-4 | Avoid non-indicated and harmful interventions (routine O2 at normal SpO2, NSAID analgesia, fibrinolysis when PPCI is immediately available). (EV-STEMI-033, -038, -023) | C-NoNSAID, C-NoLytic, C-NoO2 |
| LO-5 | Maintain monitoring/safety netting appropriate for early VF risk (monitor + defib proximity, IV access). (EV-STEMI-013) | C-MON, C-IV |

## Patient presentation

- **Demographics:** 54-year-old man ("Kemal A." — fictional), 86 kg.
- **Chief complaint:** "Crushing chest pain for the last hour and a half."
- **Context:** walked into triage of an urban PCI-capable ED at ~90 min after
  symptom onset (inside the <12 h window with strong time pressure; EV-STEMI-022).
- **Risk factors:** smoker (20 pack-years), hypertension on amlodipine,
  father had an MI at 60. (EV-STEMI-001)

## Triage

- **Note (learner-facing):** "54M, severe central chest pain ×90 min, sweaty,
  triaged category 2 to resus."
- **Initial vitals [Q1]:** HR 96 · BP 118/76 · SpO2 96% (room air) · RR 18 ·
  T 36.8 °C · pain 8/10.
- **Appearance:** pale, diaphoretic, clutching chest, anxious — maps to
  `distress_mild`/Distressed visual state.

## History (disclosure design)

| Bucket | Facts |
| --- | --- |
| Spontaneous (in briefing/first look) | chest pain 90 min, "pressure like an elephant", sweating |
| On ask (focused history) | radiation to left arm and jaw; nausea; started at rest; smoker; HTN on amlodipine; father's MI; **no prior stroke/TIA** (prasugrel-relevant); no recent surgery/bleeding (future-lysis-relevant); no PDE5 inhibitor use (nitrate-relevant; EV-STEMI-034) |
| On ask (allergies) | no known drug allergies |
| Intentionally irrelevant | mild seasonal rhinitis; appendectomy age 20 |
| Hidden until relevant | none in v1 (no trap facts in the first production case) |

## Physical exam

| System | Action | Result | State dependency |
| --- | --- | --- | --- |
| Cardiac | `exam_cardiac` | S4 gallop, no murmur, regular tachycardic rhythm | after deterioration T1: adds "cool peripheries, thready pulse" |
| Lungs | `exam_lungs` | clear bilaterally (no failure at start) | after T1: fine basal crackles |
| General | (appearance, automatic) | pale, diaphoretic, distressed | tracks visual state |

## Initial patient state (engine terms)

`vitals {hr 96, sbp 118, dbp 76, spo2 96, rr 18, temp 36.8}` ·
`rhythm: sinus_tachycardia_st_elevation` [Q1] · airway patent · breathing
spontaneous · circulation `poor_perfusion` (pale/diaphoretic → Distressed
visual) [Q1: or `normal` with pain 8?] · neuro alert · pain 8 · flags [].

## Investigations

| Action | Result | Delay | Prereq | Visibility | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `ecg_12lead` | anterior STEMI ECG asset + text result | 60 s (acquisition) | — | always | EV-STEMI-010, -014 | Pivotal. See ECG section. |
| `order_troponin` | "hs-troponin: result pending — lab turnaround ~20 min" → value returns at +1200 s sim | result delayed **[ENGINE_GAP-1]** | IV or phlebotomy | always | EV-STEMI-011 | Teaching point: don't wait for it (OQ-7). Ideal path ends before the result. |
| `order_baseline_labs` | "CBC, electrolytes, creatinine, coags sent" (values return post-case) | delayed | — | always | — | NEUTRAL, cath-lab prep realism. |
| `cxr_portable` | "Portable CXR ordered — no acute findings" text at +300 s | delayed | — | always | — | NEUTRAL; must not delay cath activation (debrief note if ordered before ECG). |

Interpretation behavior: results are presented as raw findings; **no automatic
"this is a STEMI" banner** (see ECG section for the difficulty-scaled assist).

## ECG (asset-driven; see [ECG_ASSET_SPEC.md](ECG_ASSET_SPEC.md))

- **Intended pattern:** sinus rhythm ~96/min, ST elevation V1–V4 with reciprocal
  inferior depression — unambiguous anterior STEMI (EV-STEMI-014).
- **Learner must infer:** STEMI → reperfusion pathway, LAD territory.
- **Assist by difficulty [Q2]:** core difficulty shows the raw trace + the
  machine-style header (rate/intervals only, no diagnosis). An optional
  easier mode may add "computer read: ***ST elevation, consider acute
  infarction***" — mirrors real machine reads; never names the vessel.
- **No fabricated diagnostic image**: the asset is acquired/produced per the
  spec and clinician-verified before implementation.

## Actions (by UI category)

Legend: class = CRITICAL / RECOMMENDED / NEUTRAL / UNNECESSARY / HARMFUL.
All timeCost values are sim-design numbers [Q4]. "Effect" = canonical effect.

### Patient

| actionId | Label | Class | Visibility / precond | timeCost | Effect / result | Criteria | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `focused_history` | Take a focused history | RECOMMENDED | always | 45 s | discloses history bucket 2 | C-HX | EV-STEMI-001 |
| `ask_allergies` | Ask about allergies | NEUTRAL | always | 15 s | discloses allergy fact | — | — |
| `reassure_patient` | Explain and reassure | NEUTRAL | always | 20 s | flag only (communication) | — | INACSL comms |

### Examine

| actionId | Label | Class | Visibility / precond | timeCost | Effect / result | Criteria | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `attach_monitor` | Attach monitor + defib pads | CRITICAL-adjacent (major) | always | 20 s | flag `monitor_on` | C-MON | EV-STEMI-013 |
| `exam_cardiac` | Cardiac examination | NEUTRAL | always | 30 s | exam result (state-dependent) | — | — |
| `exam_lungs` | Lung examination | NEUTRAL | always | 30 s | exam result (state-dependent) | — | — |

### Orders

`ecg_12lead` (CRITICAL, 60 s, → C-ECG) · `iv_access` (RECOMMENDED, 60 s, flag
`iv_access`, → C-IV) · `order_troponin` (RECOMMENDED, 20 s) ·
`order_baseline_labs` (NEUTRAL, 20 s) · `cxr_portable` (NEUTRAL, 30 s).

### Treat

| actionId | Label | Class | Visibility / precond | timeCost | Effect / result | Criteria | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `give_aspirin` | Aspirin 300 mg chewed | CRITICAL | always | 20 s | flag `asa_given` | C-ASA | EV-STEMI-030 [D1] |
| `give_ticagrelor` | Ticagrelor 180 mg load | RECOMMENDED | after `ecg_done` [Q3a] | 20 s | flag `p2y12_given` | C-P2Y12 | EV-STEMI-031 |
| `give_prasugrel` | Prasugrel 60 mg load | RECOMMENDED (alternative) | after `ecg_done` | 20 s | flag `p2y12_given` | C-P2Y12 | EV-STEMI-031 (no stroke/TIA — history says so) |
| `give_clopidogrel` | Clopidogrel 600 mg load | see [Q3/OQ-3] | after `ecg_done` | 20 s | flag `p2y12_given_clopi` | C-P2Y12 (partial?) | EV-STEMI-031 |
| `give_heparin_ufh` | Unfractionated heparin bolus (weight-based) | RECOMMENDED [Q5/D6] | after `ecg_done` && `iv_access` | 20 s | flag `ufh_given` | C-UFH | EV-STEMI-032 |
| `give_nitroglycerin_sl` | Sublingual nitroglycerin | NEUTRAL → state-dep. HARMFUL | always | 15 s | pain −2; **if `sbp < 100`: sbp −15 and scored harmful** [Q6] | C-NitrateSafety | EV-STEMI-034 |
| `give_morphine` | IV morphine (titrated) | NEUTRAL/context | `iv_access` | 20 s | pain −4 | — | EV-STEMI-035, -039 (debrief note only) |
| `give_oxygen` | Oxygen via mask | UNNECESSARY (SpO2 96%) | always | 15 s | flag `o2_given` | C-NoO2 | EV-STEMI-033 |
| `give_nsaid_analgesia` | IV NSAID for pain | **HARMFUL** | always | 15 s | pain −2, flag `nsaid_given` | C-NoNSAID | EV-STEMI-038 |
| `give_fibrinolytic` | Give fibrinolytic (tenecteplase) | **INAPPROPRIATE here** [Q7] | after `ecg_done` | 30 s | flag `lytic_given` | C-NoLytic | EV-STEMI-023, -025 |
| `start_statin` | High-intensity statin | RECOMMENDED (no timing pressure) | after `ecg_done` | 15 s | flag `statin_given` | C-STATIN | EV-STEMI-036 |

### More (Differential / Consult / Disposition)

| actionId | Label | Class | Visibility / precond | timeCost | Effect | Criteria | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `select_differential` | Record working differential | formative [Q9] | always | 20 s | records selection | C-DDX (formative v1) | — |
| `activate_cath_lab` | Activate cath lab / interventional cardiology | **CRITICAL** | after `ecg_done` | 30 s | flag `cath_activated`; consult response after 60 s delay **[ENGINE_GAP-2]**: "Interventional cardiology accepts — lab ready in 15 min" | C-CATH | EV-STEMI-020, -050 |
| `disposition_cath_lab` | Transfer to cath lab (handoff) | CRITICAL (terminal) | `cath_activated` | 30 s | flag `dispo_cath` → terminal | C-DISPO | EV-STEMI-050 |
| `disposition_discharge` | Discharge home | **HARMFUL (terminal)** [Q8] | always | 20 s | terminal `discharged_stemi` | C-DISPO (miss) | EV-STEMI-050 |

Double-submit: engine availability projection already disables performed
one-shot actions ("already performed") — no new mechanism needed.

## Action classification & criteria (timing + causality)

Category weights: see Scoring. All windows are **sim-design numbers derived from
guideline anchors, not guideline quotes** [Q4].

| criterionId | Clinical goal | Accepted actions | Ideal (full credit) | Acceptable (partial) | Late (zero) | Miss consequence | State-dependent consequence | Points | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| C-ECG | Diagnostic ECG fast | `ecg_12lead` | ≤300 s | ≤600 s (linear decay) | >600 s | everything downstream locked/late | — | 15 | EV-STEMI-010 |
| C-ASA | Aspirin loading | `give_aspirin` | ≤480 s | ≤720 s | >720 s | debrief: missed critical med | — | 10 | EV-STEMI-030 |
| C-P2Y12 | P2Y12 loading | `give_ticagrelor` **or** `give_prasugrel` (equivalence: both full credit) [+ clopidogrel per OQ-3] | before case end | — | — | debrief note | — | 5 | EV-STEMI-031, D3 |
| C-UFH | PPCI anticoagulation [Q5] | `give_heparin_ufh` | before handoff | — | — | debrief note | — | 5 | EV-STEMI-032 |
| C-CATH | Cath-lab activation | `activate_cath_lab` | ≤600 s | ≤900 s | >900 s | deterioration T1 fires | after T1: response text notes patient unstable | 25 | EV-STEMI-020/-021 |
| C-DISPO | Correct disposition | `disposition_cath_lab` | before deterioration T2 | — | — | terminal FAILED paths | — | 10 | EV-STEMI-050 |
| C-MON | Monitor + defib pads early | `attach_monitor` | ≤240 s | ≤480 s | >480 s | VF (T2) becomes unwitnessed in debrief | — | 5 | EV-STEMI-013 |
| C-IV | IV access | `iv_access` | ≤600 s | — | — | blocks UFH/morphine | — | 5 | — |
| C-HX | Focused history (incl. lysis/prasugrel-relevant negatives) | `focused_history` | any time | — | — | debrief note | — | 5 | EV-STEMI-001 |
| C-NoNSAID | Avoid NSAID in ACS | `give_nsaid_analgesia` (harmful) | never do it | — | — | −10 (safety-critical penalty) | — | −10 | EV-STEMI-038 |
| C-NoLytic | No lytic when PPCI available | `give_fibrinolytic` (inappropriate) | never do it | — | — | −10 [Q7: harm class] | — | −10 | EV-STEMI-023 |
| C-NoO2 | No routine O2 at SpO2 96% | `give_oxygen` (unnecessary) | avoid | — | — | −2 (**efficiency penalty, labeled non-harm**) | — | −2 | EV-STEMI-033 |
| C-NitrateSafety | Nitrate only while normotensive | `give_nitroglycerin_sl` | — | — | — | — | harmful **only if** `sbp < 100` at administration: −8 | −8 cond. | EV-STEMI-034 |
| C-STATIN | Start statin | `start_statin` | before end | — | — | debrief note | — | 3 | EV-STEMI-036 |

Order/causality rules built in: treatments gate on `ecg_done` (you treat what
you diagnosed [Q3a — reviewer may prefer aspirin-before-ECG allowed: aspirin is
deliberately **not** gated]); UFH/morphine gate on IV; disposition gates on
activation; nitrate harm gates on live BP; late cath activation *causes* T1
rather than merely losing points.

## Accepted alternative pathways

- **P2Y12 choice:** ticagrelor ≡ prasugrel (full equivalence; history
  establishes no contraindication). Clopidogrel: reviewer decision OQ-3.
- **Sequence flexibility:** aspirin before or after ECG both full-credit
  (aspirin un-gated); monitor/IV/history in any order within windows.
- **Analgesia:** morphine optional — pain relief is not scored, only safety is.
- **Reperfusion:** no accepted alternative in v1 by design (PCI-capable center;
  the *criterion* is "timely reperfusion decision", and in this setting only
  cath activation satisfies it — per §15 guidance this is a deliberate
  single-pathway criterion, justified by setting) [Q7 confirms].

## Differential diagnosis design [Q9]

- High-value plausible: **STEMI/ACS**, aortic dissection, pulmonary embolism.
- Reasonable, lower probability: pericarditis, esophageal spasm/GERD.
- Clearly inappropriate: panic attack, musculoskeletal pain (as final answer).
- v1 proposal: differential selection is **formative only** (recorded, shown in
  debrief, not scored) to avoid click-everything gaming; scored DDX deferred.

## Deterioration graph (deterministic, modest)

```
INITIAL (compensated anterior STEMI, Distressed)
  │ T1: no cath activation by 720 s
  ▼
WORSENING (pain 9, HR 108, SBP 96, crackles; Distressed/severe cue)
  │ T2: still no cath activation by 1080 s          │ R1: cath activated
  ▼                                                  ▼
VF_ARREST (terminal FAILED_DETERIORATED)        STABILIZING-ENOUGH (no vitals
  "Resus team takes over — case ends"            improvement — reperfusion, not
                                                 drugs, fixes STEMI; text cue only)
```

| transitionId | Condition | Time | Source→target | Vital changes | Visual | Action changes | Terminal? | Evidence | Review |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| T1 `ischemia_worsens` | `!cath_activated` | ≥720 s | initial→worsening | HR +12, SBP −22, pain +1 | distress_severe | exam results change; nitrate now state-harmful | no | EV-STEMI-041 | **[Q4]** |
| T2 `vf_arrest` | `!cath_activated` | ≥1080 s | worsening→arrest | circulation `arrest` | arrest (Unresponsive) | all clinical actions moot; terminal | **yes** → `FAILED_DETERIORATED` | EV-STEMI-040 | **[Q4]** |
| R1 `cath_accepted` | `cath_activated` (+60 s consult delay) | — | any→(text cue) | none (deliberate: only reperfusion fixes STEMI — no fake drug rescue) | unchanged | disposition enabled | no | EV-STEMI-020 | [Q4] |

Timings are **pedagogical compression** (real VF risk is front-loaded but not
this fast); the debrief says so explicitly. Reviewer adjudicates OQ-4.

## Terminal states

| id | Outcome | Trigger | Notes |
| --- | --- | --- | --- |
| `handoff_cath_lab` | SUCCESS | `dispo_cath` before T1 | primary intended ending |
| `handoff_after_deterioration` | PARTIAL_SUCCESS | `dispo_cath` after T1 fired | right decision, late |
| `vf_arrest_takeover` | FAILED (DETERIORATED) | T2 | resus team takes over; **not modeled as death** — ACLS out of scope, and death-certainty from a sim timer is not clinically claimable [Q10] |
| `discharged_stemi` | FAILED | `disposition_discharge` | harmful terminal [Q8] |

DEATH is deliberately unused in v1. Unity never decides any of these.

## Scoring design (proposed rubric — derived from LOs, not Full Code's weights)

Total 88 positive + capped penalties; normalize to 100 for display.

| Dimension | Points | Why it exists | Earns credit | Loses credit |
| --- | --- | --- | --- | --- |
| Critical actions (C-ECG, C-ASA, C-CATH) | 50 | LO-1/2/3 — the case *is* these three acts | doing them | missing them |
| Timing | (embedded in the 3 criticals' windows) | LO-1/3, the Qaniva differentiator | inside windows | linear decay to zero |
| Treatment adjuncts (C-P2Y12, C-UFH, C-STATIN) | 13 | LO-2 completeness | any accepted alternative | omission (debrief-noted) |
| Safety (C-NoNSAID, C-NoLytic, C-NitrateSafety) | 0 (penalty −8..−28) | LO-4 — **safety-critical penalties, labeled** | abstaining | committing |
| Monitoring & access (C-MON, C-IV) | 10 | LO-5 | early monitor/IV | late/omitted |
| Communication/history (C-HX) | 5 | prasugrel/lysis-relevant negatives have consequences | focused history | omission |
| Disposition (C-DISPO) | 10 | LO-3 endpoint | cath-lab handoff | discharge/none |
| Efficiency (C-NoO2) | penalty −2, **labeled non-harm** | keep "do everything" from winning | abstaining | shotgunning |

Breakdown is always shown per-dimension; the single number is a summary, never
the pedagogy. No penalty exists without an evidence ID + reviewer sign-off.

## Debrief design (all statements trace to timeline + criterion + evidence)

Sections: Critical decisions (ECG→diagnosis→activation with learner's actual
timestamps) · Missed critical actions · Delayed actions (window vs actual) ·
Recommended actions taken/omitted · Unnecessary actions (O2 with EV-STEMI-033
explanation) · Harmful actions (NSAID/lytic/nitrate-in-hypotension with
evidence) · Clinical timeline (engine timeline verbatim) · Alternative correct
pathways (ticagrelor/prasugrel equivalence; aspirin-order flexibility) · Key
teaching points (≤5, each cites an EV id: ECG ≤10 min; don't wait for troponin;
PPCI at a PCI center; delay costs mortality (EV-STEMI-024 as color); aspirin
early) · Evidence references (the ledger subset actually used) · Replay
suggestions (rule-based: e.g., "replay and activate the cath lab before the
10-minute mark"). A future LLM may **rephrase** these; it cannot add claims
(clinical-safety skill).

## Prebrief / briefing (learner-facing, INACSL-aligned, no spoilers)

- **Role:** you are the ED doctor receiving the patient in resus.
- **Location:** urban PCI-capable hospital ED, weekday daytime; cath lab
  operational.
- **Resources:** monitor/defib, ED drug stock, lab, portable X-ray, cardiology
  + interventional cardiology on call, resus nurse (implicit).
- **Handoff:** triage note as written above (chest pain — not "rule out MI"
  phrasing beyond what triage would realistically write).
- **Expected task:** assess and manage until a disposition decision.
- **Assumptions/fiction contract:** simulation for learning; act as you would
  clinically; time matters and advances only with your actions.

## Post-acute elements

Researched (statin continuation, DAPT duration, cardiac rehab, secondary
prevention — EV-STEMI-036/-037) and **excluded** from the playable case; one
debrief line may point forward ("post-PCI care continues in CCU…") without
scoring anything.


---

## Implementation deviations (2026-08-31, QAN-012D)

| # | Blueprint behavior | Implemented behavior | Why | Clinical impact | Status |
| --- | --- | --- | --- | --- | --- |
| D-IMPL-1 | `rhythm: sinus_tachycardia_st_elevation` | `sinus_rhythm` | the rhythm string is learner-visible; the draft value leaked the diagnosis | none (display string only) | review-pending (Q1 covers vitals/rhythm) |
| D-IMPL-2 | post-T1 state-dependent exam findings | static exam text | IMPLEMENTATION_SPEC GAP-3 ("static acceptable v1") | cosmetic; deterioration still shows via vitals/visuals | review-pending |
| D-IMPL-3 | patient visual `adult_neutral_v1` | `adult_rigged_v1` (QAN-020 rigged model) | presentation-only asset upgrade; same prefab contract | none (presentation) | n/a |
| D-IMPL-4 | ECG asset "acquired per spec, clinician-verified before implementation" | committed code-generated placeholder, watermarked NOT-DIAGNOSTIC, provenance `placeholder_replacement_required` in case data | MVP-demo authorization; no legal external asset integrated yet | learner sees a schematic tracing — replacement REQUIRED before clinical validation | open (S4/Q2) |
