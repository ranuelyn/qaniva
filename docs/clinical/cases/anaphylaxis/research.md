# Anaphylaxis case — research dossier (compact)

**Case (provisional):** `anaphylaxis_food_001` · **Retrieved:** 2026-08-31
**CLINICAL STATUS: MVP DEMO APPROVED — CLINICAL VALIDATION PENDING** (same
owner authorization model as STEMI; nothing here is clinically approved until a
clinician signs [REVIEW.md](REVIEW.md)). Machine-readable claims:
[`evidence.yaml`](evidence.yaml). This dossier is deliberately half the STEMI
dossier's size — the process is proven; only case-specific evidence is new.

## Current guideline set (currency verified 2026-08-31)

| Guideline | Version | Note |
| --- | --- | --- |
| WAO Anaphylaxis Guidance | 2020 (World Allergy Organ J) | IM epinephrine first-line; GA²LEN 2024 consensus/support tool aligns with it — no successor found |
| Resuscitation Council UK — Emergency treatment of anaphylaxis | May 2021 | IM adrenaline emphasized, **repeat after 5 min** if ABC problems persist; antihistamines third-line; **steroids no longer recommended acutely**; refractory algorithm |
| AAAAI/ACAAI Anaphylaxis Practice Parameter | 2023 (+ JTFPP 2020 GRADE on biphasic risk) | epinephrine IM lateral thigh, repeat 5–15 min; biphasic-risk-based observation; antihistamines/steroids do not prevent biphasic reactions |
| EAACI Anaphylaxis Guidelines | 2021 | European framing; IM adrenaline 0.5 mg adult |
| Türkiye Ulusal Anafilaksi Rehberi (AİD) | 2018 (+ pocket guide) | Turkish national guideline: adrenaline first drug, IM route |

## Evidence map (summary — records in evidence.yaml)

- **Recognition:** acute onset skin/mucosal signs + respiratory compromise
  and/or hypotension after likely allergen exposure = anaphylaxis; treat on
  clinical grounds, no confirmatory test in the acute phase (EV-ANA-001/-002).
- **Epinephrine:** IM into the anterolateral thigh, FIRST and without delay —
  the single most important intervention; delay is associated with worse
  outcomes and biphasic reactions (EV-ANA-010/-011). Adult IM dose: RCUK/EAACI
  0.5 mg vs US parameter 0.3 mg — **divergence DA-1**; case displays 0.5 mg
  (European/Turkish framing), review-pending.
- **Reassess + repeat:** reassess ABC after the dose; repeat IM epinephrine
  after ~5 min if not improving (EV-ANA-012).
- **IV bolus epinephrine in a patient with a pulse:** dangerous (arrhythmia /
  hypertensive surge risk); IV use belongs to dilute infusions in refractory
  cases under monitoring — modeled as the case's HARMFUL route choice
  (EV-ANA-015).
- **Adjuncts:** antihistamines third-line (skin symptoms only), steroids not
  recommended acutely (RCUK) / do not prevent biphasic reactions (JTFPP GRADE)
  — modeled NEUTRAL, never a substitute (EV-ANA-023/-024, divergence DA-2 on
  steroid wording strength).
- **Supportive:** high-flow oxygen when hypoxic; IV crystalloid bolus for
  hypotension; supine positioning with legs raised (avoid sudden standing)
  (EV-ANA-020/-021/-022).
- **Tryptase:** timed serum tryptase supports the diagnosis retrospectively —
  never delays treatment (EV-ANA-025).
- **Disposition:** observe after resolution (individualized, longer with
  severe/biphasic-risk features); premature discharge risks a biphasic
  reaction. Observation vs admission are both guideline-compatible endpoints —
  the case accepts either (EV-ANA-030, divergence DA-3 on duration).

## Guideline divergences (reviewer adjudicates)

| ID | Topic | Positions | Case default | Review |
| --- | --- | --- | --- | --- |
| DA-1 | adult IM epinephrine dose | 0.5 mg (RCUK/EAACI/TR ampoule) vs 0.3 mg (US parameter/autoinjector) | display 0.5 mg IM | **YES** |
| DA-2 | acute corticosteroids | RCUK: no longer recommended · US: not routinely, don't prevent biphasic | NEUTRAL adjunct, uncredited | YES |
| DA-3 | observation duration | individualized 1–6 h+; extended for severe reactions | endpoint = "observe in ED (4–6 h)" or "admit", both accepted | **YES** |

## Scope

One scenario: 24F, known peanut allergy, restaurant meal 25 min ago —
urticaria, lip swelling, wheeze, hypotension (airway `at_risk`, SpO₂ 92%).
Trains: recognition → **IM epinephrine first** → positioning/oxygen/fluids →
reassessment after the dose → observation-vs-admission disposition; the
dangerous route (IV bolus epi) and adjuncts-instead-of-epi are the error paths.
**Out of scope:** pediatric dosing, refractory epinephrine infusions, airway
management/intubation, biphasic second reaction in-sim, ICU care, autoinjector
training, non-food triggers.

## Open questions for the reviewer

OQ-A1 initial vitals realism (HR 118, 88/54, SpO₂ 92, RR 26, airway at_risk)?
OQ-A2 epi timing window 240/480 s + deterioration at 360/780 s as labeled
compression? OQ-A3 epi-response magnitudes (BP +14/+8, SpO₂ +4, HR −10 at
+60 s)? OQ-A4 IV-push consequence magnitudes (HR +38, SBP +30)? OQ-A5 is
crediting reassessment only after epinephrine (state-constrained) sound? OQ-A6
Turkish guideline currency — is a post-2018 AİD update available?

## Sources

WAO 2020 (doi:10.1016/j.waojou.2020.100472); GA²LEN 2024 consensus (JACI);
RCUK Emergency treatment of anaphylaxis, May 2021 (resus.org.uk); AAAAI/ACAAI
2023 practice parameter (Ann Allergy Asthma Immunol; PMID 38108678); JTFPP 2020
GRADE anaphylaxis; EAACI 2021; AİD Anafilaksi: Türk Ulusal Rehberi 2018
(aai.org.tr / aid.org.tr).
