# STEMI first production case — research dossier

**Case (provisional):** `stemi_anterior_001` · **Stage:** RESEARCHING → DRAFT
**Retrieved:** 2026-08-31 · **Author:** Qaniva clinical-content workflow (AI-assisted)
**CLINICAL STATUS: DRAFT — REVIEW REQUIRED.** Nothing in this dossier is canonical
Qaniva truth until a clinician approves it via `REVIEW.md`.

Machine-readable claims live in [`evidence.yaml`](evidence.yaml) (IDs `EV-…`
referenced throughout). This document is the human narrative.

---

## 1. Research question

What must Qaniva's first production case teach, and what does current Tier-A
evidence say about the recognition, timing, reperfusion strategy, pharmacology,
deterioration risks and disposition of an adult presenting to a PCI-capable ED
with an ST-elevation myocardial infarction — narrowed to a single teachable
scenario playable in 8–12 minutes on a phone?

## 2. Source hierarchy used

- **Tier A** — 2025 ACC/AHA/ACEP/NAEMSP/SCAI ACS guideline; 2023 ESC ACS
  guideline; INACSL Healthcare Simulation Standards of Best Practice (2021).
- **Tier B** — ACC/AHA official summaries ("Top Things to Know", ACC Ten
  Points); TKD publications / Turkish translations of ESC; Full Code and Body
  Interact official documentation; Wikimedia Commons licensing pages.
- **Tier C** — used only for cross-checking numbers already present in Tier A
  (a physician-maintained quick-reference; Turkish emergency-medicine
  translation sites acilci.net / acilcalisanlari.com). No Tier-C claim is
  canonical on its own.

Retrieval note: guideline full texts were accessed through official abstracts,
society summaries and the open-access ESC full text. **Where a secondary summary
was the retrieval path for an exact number, the evidence record says so and the
number carries `reviewRequired: true`** — the clinician reviewer verifies against
the primary PDF (both guidelines are freely accessible to clinicians).

## 3. Current guideline set (verified current as of 2026-08-31)

| Guideline | Version | Status check |
| --- | --- | --- |
| 2025 ACC/AHA/ACEP/NAEMSP/SCAI Guideline for the Management of Patients With Acute Coronary Syndromes | Published Feb 2025 (Circulation `CIR.0000000000001309` / JACC `10.1016/j.jacc.2024.11.009`) | Newest ACC/AHA ACS document; replaces 2013 STEMI + 2014 NSTEMI + 2015 PPCI update. No 2026 successor found. |
| 2023 ESC Guidelines for the management of acute coronary syndromes | Eur Heart J 2023;44(38):3720 | First combined STEMI+NSTE-ACS ESC document. No 2026 replacement found (ESC publication schedule checked). |
| INACSL Healthcare Simulation Standards of Best Practice | 2021 revision (renamed from "Standards of Best Practice: Simulation") | Current edition. |

## 4. STEMI clinical evidence map (summary)

Working definition (both guidelines): acute chest discomfort (or equivalent)
with **persistent ST-segment elevation in ≥2 contiguous leads** (or equivalents)
→ treat as STEMI and move to reperfusion; **do not wait for troponin**
(EV-STEMI-010, EV-STEMI-011).

- Typical presentation: retrosternal pressure/pain, radiation to arm/jaw,
  diaphoresis, nausea, dyspnea; onset at rest or exertion (EV-STEMI-001).
- Atypical presentations (older adults, women, diabetes): dyspnea-dominant,
  epigastric pain, weakness — relevant for future cases, **out of scope for
  case v1** which deliberately uses a typical presentation (EV-STEMI-002).
- Risk context for a credible fictional patient: smoking, hypertension,
  hyperlipidemia, family history, diabetes (EV-STEMI-001).

## 5. Timing evidence

| Anchor | ACC/AHA 2025 | ESC 2023 | Evidence ID |
| --- | --- | --- | --- |
| First ECG | within 10 min of FMC | within 10 min of FMC (interpreted) | EV-STEMI-010 |
| PPCI at a PCI-capable hospital | FMC-to-device ≤90 min | ≤60 min from STEMI diagnosis at a PCI hospital (≤90 for transfers); ≤120 min is the PCI-vs-lysis decision bound | EV-STEMI-020, EV-STEMI-021 |
| Fibrinolysis (when PPCI not available in time) | door-to-needle ≤30 min | bolus within 10 min of STEMI diagnosis | EV-STEMI-023 (divergence D2) |
| Reperfusion window | symptom onset <12 h | symptom onset <12 h | EV-STEMI-022 |
| Cost of delay | each 30 min of PPCI delay ≈ +7.5% relative 1-year mortality | delay-sensitive (same literature base) | EV-STEMI-024 |

These anchors justify the case's timing pressure and the timing-scored criteria.
Exact playable windows in the blueprint are **sim-design derivations**, not
guideline quotes — labeled as such and separately reviewable.

## 6. Diagnostic evidence

- 12-lead ECG is the pivotal diagnostic act; serial ECGs if the first is
  non-diagnostic but suspicion is high (out of scope for v1 — the case ECG is
  diagnostic) (EV-STEMI-010, EV-STEMI-012).
- Troponin: drawn as baseline, but reperfusion **must not wait** for the result
  (EV-STEMI-011). The case models this: troponin is orderable, its result
  returns after a realistic delay, and waiting for it is scored as delay.
- Continuous cardiac monitoring + defibrillator availability early — VF risk
  (EV-STEMI-013).
- Learner must recognize: anterior ST elevation (V1–V4 ± aVL/I) with reciprocal
  inferior depression → LAD territory (EV-STEMI-014). What is *not* revealed
  automatically: the machine-read banner does not say "STEMI — activate cath
  lab"; interpretation is the learner's job (difficulty-scalable assist is a
  design option, see BLUEPRINT §ECG).
- Diagnostic traps considered and **excluded from v1**: LBBB/Sgarbossa, posterior
  MI, early repolarization mimic, pericarditis ST elevation. First case must
  have an unambiguous ECG (INACSL: objectives drive complexity).

## 7. Reperfusion evidence

- Primary PCI is the default reperfusion strategy when achievable within 120
  min of diagnosis/FMC; the case is set **in a PCI-capable center**, so the
  correct pathway is immediate cath-lab activation, not fibrinolysis
  (EV-STEMI-020, EV-STEMI-021).
- Fibrinolysis is for when PPCI cannot be delivered in time (with a
  contraindication checklist); giving lytics when the cath lab is immediately
  available exposes bleeding risk without benefit — modeled as an
  **inappropriate action** in v1 (harm classification itself flagged for
  reviewer decision, Q7 in REVIEW.md) (EV-STEMI-023, EV-STEMI-025).
- Transfer decisions, pharmaco-invasive strategy (angiography 2–24 h post
  lysis), rescue PCI: researched (EV-STEMI-026) — **out of scope for v1**
  (no transfer geometry in a single-room case).

## 8. Medication evidence

Per-drug detail + doses: evidence ledger EV-STEMI-030…-039 and BLUEPRINT
medication table. Summary of *case classification* (Qaniva scoring intent — every
row needs clinician sign-off):

| Drug | Guideline position | Case classification |
| --- | --- | --- |
| Aspirin (chewed, non-enteric, 162–325 mg AHA / 150–300 mg ESC) | Class 1 both | **CRITICAL** |
| P2Y12 load (ticagrelor 180 mg or prasugrel 60 mg preferred; clopidogrel 600 mg if those unavailable/contraindicated) | Class 1 both | **RECOMMENDED**, alternatives-equivalent criterion (divergence D3 on pre-treatment timing) |
| Anticoagulation for PPCI (UFH bolus standard) | Class 1 both | **RECOMMENDED** (often cath-lab-administered — reviewer decides if ED action belongs in v1, Q5) |
| Oxygen | Only if SpO2 <90% (routine O2 Class 3: No Benefit) | **UNNECESSARY** at this case's SpO2 96% |
| Nitrates (SL) | Symptom relief; contraindicated in hypotension/RV infarct/recent PDE5 | **NEUTRAL** here (anterior, normotensive) — becomes state-dependent harmful only if hypotension develops |
| Morphine/opioid analgesia | For severe pain (ESC I-C for severe pain per retrieval; interactions with P2Y12 absorption noted in literature) | **NEUTRAL/context** |
| High-intensity statin | Start early/in-hospital, Class 1 | **RECOMMENDED** (low urgency; timing not scored) |
| Oral beta blocker | Within 24 h if no HF/shock/bradycardia (AHA Class 1); early IV use selective (ESC IIa/IIb) | **OUT OF SCOPE** for the acute 10-minute window (divergence D4) |
| NSAID analgesia (e.g., diclofenac) | Contraindicated in ACS (both) | **HARMFUL** |

Deliberately excluded from MVP case: GP IIb/IIIa inhibitors, cangrelor,
bivalirudin choice detail, colchicine, secondary-prevention titration.

## 9. Deterioration / complication evidence

Realistic candidates researched: ventricular arrhythmia (VF/VT — leading early
cause of death, why monitoring/defib proximity matters), bradyarrhythmia/AV
block (more typical of inferior MI), acute heart failure, cardiogenic shock
(CULPRIT-SHOCK: culprit-only PCI), mechanical complications (late, rare)
(EV-STEMI-040…-042).

**Selected for v1** (only what the objectives need):

1. Progressive ischemia when reperfusion is delayed → worsening pain, falling
   BP (visual: Distressed persists/worsens).
2. VF arrest as the endpoint of gross delay → terminal state; **resus team
   takes over and the case ends** (ACLS is explicitly out of scope — running a
   code is its own future case).

Cardiogenic shock management, RV infarction, AV block: excluded from v1.

## 10. Disposition evidence

Correct disposition for this case is **emergent cath-lab activation +
interventional cardiology involvement**, ending the playable case at handoff
(EV-STEMI-050). Post-PCI CCU admission is real but happens after the case's
final frame — out of scope. Discharge/observation of a diagnosed STEMI is
modeled as harmful-terminal (reviewer question Q8).

## 11. Guideline divergences (explicit — reviewer must adjudicate)

| ID | Topic | ACC/AHA 2025 | ESC 2023 | Turkey relevance | Proposed Qaniva default | Review req. |
| --- | --- | --- | --- | --- | --- | --- |
| D1 | Aspirin loading dose | 162–325 mg chewed | 150–300 mg oral (75–250 mg IV) | TKD follows ESC | Accept the overlap: display 300 mg chewed | **YES** |
| D2 | Lytic timing metric (not used in v1, recorded for future transfer case) | door-to-needle ≤30 min | ≤10 min from STEMI diagnosis to bolus | ESC framing used in TR | ESC framing | YES |
| D3 | P2Y12 pre-treatment timing in STEMI→PPCI | load without delaying angiography | at time of diagnosis for PPCI pathway (pre-treatment debate mainly NSTE-ACS) | ESC | Accept P2Y12 load any time before case end; timing not penalized in v1 | **YES** |
| D4 | Early beta-blockade | oral within 24 h Class 1 | early IV selective (IIa/IIb, stable, no contraindication) | ESC | Exclude from the 10-min acute case entirely | YES |
| D5 | PPCI timing anchor | FMC-to-first-device ≤90 min | diagnosis-to-wire ≤60 min (PCI center) | Turkish registries report door-to-balloon | Case scores **arrival-to-cath-activation** (a proxy the sim can measure honestly) | **YES** |
| D6 | UFH in the ED vs cath lab | dosing at PCI | dosing at PCI | local practice varies | Offer UFH as recommended-not-critical; no dose typed by learner in v1 | **YES** |

## 12. Turkish localization considerations

- TKD adopts/translates ESC guidelines (Turkish translation of ESC 2023 ACS
  exists; acilci.net hosts a section-by-section Turkish rendering) — where
  ACC/AHA and ESC diverge, **ESC is the closer fit for the Turkish learner**
  (proposed default, reviewer confirms) (EV-STEMI-060).
- MoH "Türkiye Kalp ve Damar Hastalıkları Önleme ve Kontrol Programı" (action
  plan 2015–2020) established the national CVD framework; Turkish centers
  report door-to-balloon metrics consistent with the ≤90 min target
  (EV-STEMI-061). No newer national STEMI-network pathway document was found
  in this pass — reviewer may know an unpublished/e-signed MoH circular
  (open question OQ-5).
- Practical fit: an urban Turkish PCI-capable ED (the chosen setting) is a
  realistic default; drug availability (ticagrelor, clopidogrel, prasugrel,
  UFH, tenecteplase) is consistent with Turkish formularies — **flagged for
  reviewer confirmation**, not asserted.
- Language: case text ships in English for MVP; Turkish localization is a
  product decision outside this dossier.

## 13. Competitor authoring lessons (patterns only, nothing copied)

**Full Code** (official site/FAQ): clinician-authored via a web "Creator" with
template content; primary author board-certified MD/DO; ~4–6 h authoring per
case after training; per-action scoring designations (critical / recommended /
unnecessary / harmful); local-practice customization of an existing case is a
first-class feature. → Lessons: template + reuse beats blank-page authoring;
scoring classification at the *action* level is legible to clinicians;
clinician review is a role, not a step. Qaniva addition: Full Code's flat
classes lack explicit **timing windows and causality** — Qaniva's criterion
model (timing window + state constraints) is the differentiator; we do **not**
copy Full Code's 35/20/45-style weight split.

**Body Interact** (official help docs): separates *general* vs *specific
measurable* learning objectives; attaches **scientific references** to each
scenario; time-stamped action report + untreated-conditions list feeds the
debrief; distinct training vs OSCE/assessment modes. → Lessons: objectives
first and measurable; references are part of the case artifact (Qaniva's
evidence ledger goes further: per-claim traceability); the debrief is generated
from the timeline, never invented.

## 14. Simulation-design standards applied (INACSL HSSOBP 2021)

| Standard | Qaniva authoring requirement derived |
| --- | --- |
| Outcomes & Objectives | Case authoring **starts** with 3–5 measurable, observable-behavior objectives; every scoring criterion maps to one. |
| Simulation Design | Evidence-based scenario, fidelity chosen to serve objectives (primitive 3D room is acceptable; ECG asset must be diagnostic-quality), pilot test before use (→ BLIND_PLAYTEST lifecycle stage). |
| Prebriefing | A structured briefing (role, setting, resources, expectations, fiction contract) ships with the case — BLUEPRINT §Prebrief. |
| Facilitation | Self-directed mobile play = facilitation is embedded in the product (hints policy is a difficulty setting, never silent auto-help). |
| Evaluation | Formative scoring in v1; summative/OSCE mode is a future product decision. Criteria are transparent post-hoc in the debrief. |
| Debriefing Process | Debrief is a designed artifact tied to objectives and the actual timeline (BLUEPRINT §Debrief), not free-form LLM prose. |

## 15. Case-scope recommendation

**One scenario:** middle-aged adult smoker with typical crushing chest pain,
90 min after onset, walks into a **PCI-capable urban ED**. Diagnostic anterior
STEMI on the first ECG. Correct pathway: recognize → aspirin + adjuncts →
activate cath lab fast → handoff. Full scope table and OUT-OF-SCOPE list:
BLUEPRINT §Case identity / §Out of scope. Session target 8–12 min real time,
1:1 sim clock (engine precedent from the demo case).

## 16. Open clinical questions (for the reviewer)

- OQ-1 Initial vitals set (BLUEPRINT) realistic for a compensated anterior STEMI?
- OQ-2 Is scoring **arrival-to-cath-lab-activation** an acceptable proxy for the
  FMC-to-device metric inside a 10-minute playable slice (D5)?
- OQ-3 Should clopidogrel (no contraindication present) earn full, partial, or
  no P2Y12 credit in a Turkish training context?
- OQ-4 Deterioration timings (BLUEPRINT state graph) — clinically defensible as
  *pedagogical compression*, or must they lengthen?
- OQ-5 Is there a current MoH/112 STEMI-network pathway document to cite?
- OQ-6 Fibrinolysis-in-PCI-center: score as HARMFUL or as UNNECESSARY-with-
  teaching-note (Q7)?
- OQ-7 Troponin result delay (proposed ~20 min sim → returns after case end on
  the ideal path): acceptable teaching device?

## 17. Source list

Primary/Tier A:
1. 2025 ACC/AHA/ACEP/NAEMSP/SCAI Guideline for the Management of Patients With
   Acute Coronary Syndromes. Circulation. 2025. doi:10.1161/CIR.0000000000001309
   (also JACC doi:10.1016/j.jacc.2024.11.009; PMID 40014670).
2. Byrne RA et al. 2023 ESC Guidelines for the management of acute coronary
   syndromes. Eur Heart J. 2023;44(38):3720–3826.
   https://academic.oup.com/eurheartj/article/44/38/3720/7243210
3. INACSL Standards Committee. Healthcare Simulation Standards of Best
   Practice™ (2021). https://www.inacsl.org/healthcare-simulation-standards-of-best-practice-

Tier B:
4. AHA Professional Heart Daily — 2025 ACS guideline hub & "Top Things to Know".
5. ACC "Ten Points to Remember" — 2023 ESC ACS guidelines (Aug 2023).
6. TKD (Türk Kardiyoloji Derneği) guideline program — Turkish adoption of ESC;
   acilci.net Turkish section-by-section translation of ESC 2023 ACS.
7. T.C. Sağlık Bakanlığı — Türkiye Kalp ve Damar Hastalıkları Önleme ve Kontrol
   Programı (action plan 2015–2020).
8. Full Code Medical Simulation — Educators / FAQ / Organizations pages
   (fullcodemedical.com).
9. Body Interact Help — Scenario Details, Learning Objectives, Clinical
   Scenario Features (help.bodyinteract.com); bodyinteract.com OSCE page.
10. Wikimedia Commons — 12-lead ECG file licensing pages (CC BY 4.0 examples).

Tier C (cross-check only):
11. sattimd.com 2025 ACC/AHA ACS quick reference (physician-maintained).
12. acilcalisanlari.com — ESC 2023 changes summary (Turkish EM community site).
