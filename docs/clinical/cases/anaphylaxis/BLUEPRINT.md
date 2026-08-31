# Anaphylaxis case blueprint — `anaphylaxis_food_001` (compact)

**CLINICAL STATUS: MVP DEMO APPROVED — CLINICAL VALIDATION PENDING** (owner
authorization 2026-08-31; [REVIEW.md](REVIEW.md) unsigned). Evidence:
[`evidence.yaml`](evidence.yaml). Implemented as
`packages/case-schema/fixtures/anaphylaxis_food_001/v1/case.json` — the JSON is
the full detail; this blueprint records the design decisions and review flags.

## Identity & scope

24F ("Elif D.", fictional, 61 kg), known peanut allergy + mild asthma
(EV-ANA-031), restaurant meal ~25 min ago → urticaria, lip swelling, wheeze,
hypotension. ED resus, `ed_resus_v1` + `adult_rigged_v1` (full reuse, no new
art). ~8 min session, 1:1 clock. Out of scope: pediatric dosing, refractory
infusions, intubation, in-sim biphasic recurrence, ICU, autoinjector training.

## Learning objectives

LO-1 Recognize anaphylaxis clinically and give **IM epinephrine first, fast**
(EV-ANA-001/-010/-011). LO-2 Choose the safe route — IM thigh, never undiluted
IV bolus with a pulse (EV-ANA-015). LO-3 Support ABC: positioning, high-flow
oxygen (SpO₂ 92% — indicated here, unlike the STEMI case), IV fluids for
hypotension (EV-ANA-020/-021/-022). LO-4 Reassess after the dose; adjuncts are
adjuncts (EV-ANA-012/-023/-024). LO-5 Disposition with biphasic risk in mind —
observe or admit, never immediate discharge (EV-ANA-030).

## Initial state [OQ-A1]

HR 118 · BP 88/54 · SpO₂ 92% · RR 26 · T 36.9 · pain 2 · rhythm sinus_tachycardia ·
airway **at_risk** (lip swelling, no stridor yet) · breathing **labored**
(wheeze) · circulation poor_perfusion · neuro alert → Distressed visual.

## Design highlights (rest in the case JSON)

- **22 actions**; treatments deliberately NOT gated behind a diagnostic step
  (unlike STEMI): anaphylaxis is a clinical diagnosis — epinephrine is
  available from t=0 (EV-ANA-002).
- **Route as separate actions** (`give_epinephrine_im` 0.5 mg thigh, repeatable
  for a second dose, vs `give_epinephrine_iv_push` 1 mg — HARMFUL −12 with a
  canonical consequence rule: HR +38, SBP +30 surge). **QAN-006b deliberately
  NOT built**: two labeled actions teach the route choice at least as visibly
  as a parameter picker; the parameter UI stays deferred until a case needs
  numeric dose titration (decision documented, see review Q-B & friction log).
- **State-constrained reassessment**: `c_reassess_after_epi` accepts
  `exam_lungs` OR `exam_airway_skin` but only credits when
  `flag('epi_im_given')` — examining before treating is not "reassessment".
- **Two alternative-equivalence criteria**: reassessment (two accepted exams)
  and disposition (`disposition_observation` ≡ `disposition_admit`, DA-3).
- **Deterioration**: T1 ≥360 s without IM epi (SpO₂ −6, HR +14, BP −16/−8,
  RR +6, `deteriorated`) → T2 ≥780 s (airway obstructed, apneic, arrest,
  unresponsive) → terminal `peri_arrest_takeover`, outcome **deteriorated**
  (resus takeover, not death — same framing as STEMI). Timings are labeled
  pedagogical compression [OQ-A2].
- **Visible improvement**: `epi_response` rule (+60 s after IM dose): BP
  +14/+8, SpO₂ +4, HR −10, breathing→spontaneous, airway→patent, cue
  `recovery` — the monitor and the patient visibly recover [OQ-A3];
  `fluid_response` (+120 s): BP +8/+4. All rules carry authored `debriefText`
  causality lines.
- **Rubric**: 80 positive pts (epi 30 w/ 240/480 window · hx 5 · monitor 5 ·
  IV 5 · O₂ 8 · fluids 8 · positioning 4 · reassess 5 · disposition 10) + one
  safety penalty (IV-push epi −12). No efficiency trap in this case (design
  choice: the error modes that matter are delay, route and substitution).
  Antihistamine/steroid/salbutamol are NEUTRAL adjuncts (EV-ANA-023/-024).
- **Terminals (5)**: complete (observe), admit (admit), partial (dispo after
  deterioration), discharge (premature discharge — biphasic risk), deteriorated
  (peri-arrest takeover).
- Tryptase = delayed result (900 s) that must not delay care (EV-ANA-025);
  **no result assets** (proves `resultAssets` is optional per case).

## Golden paths (expected scores designed up front)

G1 optimal 80 · G2 adjuncts-first/delayed epi (T1 fires; epi decayed; ends
`partial`) · G3 alternative 80 ≡ G1 (epi-before-history order + airway-exam
reassessment + ADMIT disposition) · G4 thorough-but-timely (all adjuncts +
labs + second epi dose; still 80 — extra actions cost time, not points) ·
G5 harmful route (IV push −12 then proper IM; 68, surge visible) ·
G6 untreated → T1 → T2 → deteriorated (hx+monitor only = 10).
