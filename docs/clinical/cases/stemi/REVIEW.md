# Clinical review package — `stemi_anterior_001`

**For the reviewing clinician.** Everything you need is in this file and
[BLUEPRINT.md](BLUEPRINT.md) — you never need to read JSON or code. Claims are
traceable via [evidence.yaml](evidence.yaml) (EV-… ids).

**CLINICAL STATUS: MVP DEMO APPROVED — CLINICAL VALIDATION PENDING.**
(The case is implemented as an internal/demo prototype per the project owner's
2026-08-31 decision; this review remains REQUIRED before any clinical claim.)
Reviewer: __________ · Credentials: __________ · Date: __________

How to review: work through the sections below; mark each **APPROVE /
REQUEST CHANGE / REJECT** with comments. Section-level approval is valid — the
case only implements sections you approved; a single REJECT on a safety item
blocks implementation. The AI that drafted this has **no authority** to declare
any of it clinically correct.

---

## 1. Case synopsis

54M smoker with hypertension, 90 min of typical crushing chest pain, walks into
an urban **PCI-capable** ED. First ECG (target ≤10 min) shows unambiguous
anterior STEMI. Correct pathway: aspirin + adjuncts, activate the cath lab
fast, handoff to interventional cardiology. Delay → worsening ischemia → VF
arrest (resus team takes over; case ends). 8–12 min playable, aimed at clinical
students / interns. Full detail: BLUEPRINT.

## 2. Section checklist

| # | Section (BLUEPRINT ref) | Verdict | Comments |
| --- | --- | --- | --- |
| S1 | Patient presentation + triage vitals | ☐A ☐RC ☐R | |
| S2 | History / exam content + disclosure design | ☐A ☐RC ☐R | |
| S3 | Investigations (incl. troponin-as-delayed-result) | ☐A ☐RC ☐R | |
| S4 | ECG intent + assist policy + asset spec | ☐A ☐RC ☐R | |
| S5 | Medication table (drugs, displayed doses, routes, gating) | ☐A ☐RC ☐R | |
| S6 | Harmful/unnecessary action set + penalty labels | ☐A ☐RC ☐R | |
| S7 | Reperfusion model (PPCI-only pathway, cath activation criterion) | ☐A ☐RC ☐R | |
| S8 | Timing windows (all sim-design numbers) | ☐A ☐RC ☐R | |
| S9 | Deterioration graph T1/T2 + magnitudes | ☐A ☐RC ☐R | |
| S10 | Terminal states (no DEATH in v1) | ☐A ☐RC ☐R | |
| S11 | Accepted alternative pathways (P2Y12 equivalence etc.) | ☐A ☐RC ☐R | |
| S12 | Scoring rubric + weights | ☐A ☐RC ☐R | |
| S13 | Debrief claims + teaching points | ☐A ☐RC ☐R | |
| S14 | Prebrief text | ☐A ☐RC ☐R | |
| S15 | Evidence ledger accuracy (spot-check EV ids against primaries) | ☐A ☐RC ☐R | |

## 3. Medication/action quick table (verify each row)

| Drug/action | Displayed dose/route | Case classification | Evidence | OK? |
| --- | --- | --- | --- | --- |
| Aspirin | 300 mg chewed | CRITICAL | EV-STEMI-030 (D1) | ☐ |
| Ticagrelor | 180 mg PO load | RECOMMENDED (≡ prasugrel) | EV-STEMI-031 | ☐ |
| Prasugrel | 60 mg PO load | RECOMMENDED (≡ ticagrelor) | EV-STEMI-031 | ☐ |
| Clopidogrel | 600 mg PO load | credit level = your call (OQ-3) | EV-STEMI-031 | ☐ |
| UFH | weight-based IV bolus (not typed) | RECOMMENDED (Q5: keep in ED scope?) | EV-STEMI-032 | ☐ |
| SL nitroglycerin | 0.4 mg SL | NEUTRAL; harmful iff SBP<100 | EV-STEMI-034 | ☐ |
| Morphine | titrated IV | NEUTRAL | EV-STEMI-035 | ☐ |
| Oxygen | mask, SpO2 96% | UNNECESSARY (−2, non-harm label) | EV-STEMI-033 | ☐ |
| NSAID | IV, for pain | HARMFUL (−10) | EV-STEMI-038 | ☐ |
| Tenecteplase | bolus | inappropriate-here (−10; Q7) | EV-STEMI-023/-025 | ☐ |
| High-intensity statin | PO | RECOMMENDED, untimed | EV-STEMI-036 | ☐ |

## 4. Explicit review questions

| # | Question | Answer / decision |
| --- | --- | --- |
| Q1 | Are the initial vitals (HR 96, 118/76, SpO2 96, RR 18, pain 8) and `circulation: poor_perfusion` realistic for a compensated anterior STEMI at 90 min? | |
| Q2 | ECG assist policy: raw trace only at core difficulty, machine-read line at easy — appropriate? | |
| Q3 | Is gating P2Y12/UFH/statin/lytic behind "ECG done" sound (treat what you diagnosed), and is aspirin correctly left un-gated? | |
| Q4 | Timing: ECG windows (300/600 s), cath activation (600/900 s), deterioration at 720 s, VF at 1080 s — defensible as labeled pedagogical compression? Adjust freely. | |
| Q5 | Should ED-administered UFH be in the v1 action set, or deferred to cath lab (D6)? | |
| Q6 | Nitrate state-dependence (harmful only if SBP<100 at administration, −8): plausible and fair? | |
| Q7 | Fibrinolysis at a PCI-capable center with the lab available: score HARMFUL (−10) or UNNECESSARY-with-teaching-note? | |
| Q8 | Discharging a diagnosed STEMI = harmful terminal FAILED: keep? | |
| Q9 | Differential selection formative-only in v1 (recorded, unscored): agree? | |
| Q10 | VF arrest ends as FAILED "resus team takes over" rather than DEATH: agree? | |
| Q11 | Are the fibrinolysis/prasugrel-relevant negatives in the history sufficient and realistic? | |
| Q12 | Is anything in the case unsafe, misleading, or likely to teach a wrong reflex? | |
| Q13 | Turkey fit: ESC-preferred defaults, drug availability, PCI-capable urban ED setting — realistic for the target learner (OQ-5: any national pathway doc to cite)? | |
| Q14 | Do the debrief teaching points teach LO-1..LO-5 and only evidence-backed claims? | |

## 5. Sign-off

| Decision | Meaning |
| --- | --- |
| ☐ APPROVED | all sections A → case may be implemented (lifecycle → CLINICALLY_APPROVED); record in `metadata.clinicalReview` |
| ☐ CHANGES REQUESTED | blueprint revised per comments, re-review changed sections only |
| ☐ REJECTED | case redesign required |

Signature: __________ Date: __________
