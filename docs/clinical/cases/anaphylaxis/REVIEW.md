# Clinical review package — `anaphylaxis_food_001`

**CLINICAL STATUS: MVP DEMO APPROVED — CLINICAL VALIDATION PENDING.** This
review remains REQUIRED before any clinical claim; the case is implemented as
an internal/demo prototype per the project owner's 2026-08-31 authorization.
Reviewer: __________ · Credentials: __________ · Date: __________
Sections may be approved individually (APPROVE / REQUEST CHANGE / REJECT); a
REJECT on a safety item blocks the case. The drafting AI has no approval
authority.

## 1. Synopsis

24F, known peanut allergy + mild asthma, 25 min after a restaurant meal:
urticaria, lip swelling, wheeze, BP 88/54, SpO₂ 92%. Correct pathway: IM
epinephrine 0.5 mg thigh FIRST → positioning, high-flow O₂, IV fluids →
reassess (repeat dose available) → observe 4–6 h or admit. Errors modeled:
adjuncts-instead-of-epi (delay → deterioration), undiluted IV bolus
epinephrine (surge), premature discharge (biphasic risk). Untreated → airway
obstruction/peri-arrest takeover (`deteriorated`, not death).

## 2. Section checklist

| # | Section | Verdict | Comments |
| --- | --- | --- | --- |
| S1 | Presentation + initial vitals (incl. airway `at_risk`) | ☐A ☐RC ☐R | |
| S2 | History/exam + disclosure design | ☐A ☐RC ☐R | |
| S3 | Epinephrine: dose (0.5 mg IM, DA-1), un-gated availability, repeatability | ☐A ☐RC ☐R | |
| S4 | IV-push epinephrine harm model (−12; HR +38, SBP +30) | ☐A ☐RC ☐R | |
| S5 | Supportive care set (O₂ at SpO₂ 92, fluids, positioning) | ☐A ☐RC ☐R | |
| S6 | Adjuncts NEUTRAL (antihistamine/steroid/salbutamol; DA-2) | ☐A ☐RC ☐R | |
| S7 | Timing windows (epi 240/480 s) + T1 360 s / T2 780 s compression | ☐A ☐RC ☐R | |
| S8 | Response magnitudes (epi +60 s: BP +14/+8, SpO₂ +4, HR −10; fluids +8/+4) | ☐A ☐RC ☐R | |
| S9 | Reassessment credited only post-epinephrine (state constraint) | ☐A ☐RC ☐R | |
| S10 | Disposition equivalence (observe 4–6 h ≡ admit; DA-3) + discharge terminal | ☐A ☐RC ☐R | |
| S11 | Terminal states (deteriorated, not death) | ☐A ☐RC ☐R | |
| S12 | Rubric weights (80 + −12) | ☐A ☐RC ☐R | |
| S13 | Debrief texts / causality lines / teaching points | ☐A ☐RC ☐R | |
| S14 | Prebrief (no spoiler) | ☐A ☐RC ☐R | |
| S15 | Evidence ledger spot-check vs primaries | ☐A ☐RC ☐R | |

## 3. Key questions

Q-A OQ-A1..A6 (research.md) — vitals realism, timing compression, response and
surge magnitudes, state-constrained reassessment, Turkish guideline currency.
Q-B Is modeling the IM-vs-IV route as two labeled actions (no dose/route
picker) pedagogically sufficient for this case? (QAN-006b deferral decision.)
Q-C Tryptase result text ("elevated — drawn during the acute phase") —
acceptable without a number?
Q-D Should salbutamol earn credit for wheeze (currently NEUTRAL adjunct)?
Q-E Anything unsafe, misleading, or likely to train a wrong reflex?

## 4. Sign-off

☐ APPROVED → CLINICALLY_APPROVED (record in `metadata.clinicalReview`, bump
case version for any changes) · ☐ CHANGES REQUESTED · ☐ REJECTED

Signature: __________ Date: __________
