# STEMI MVP implementation audit

**Audited tree:** `2c15652` (clean) · **Date:** 2026-08-31 · **Scope:** read-only
post-implementation audit of `stemi_anterior_001` v1 + the generic capabilities
added in commits `193ba09..2c15652`. Every number below was recomputed from the
repository (case JSON, golden files, engine source, live engine execution) —
not copied from the sprint handoff. Where the handoff was wrong, this document
says so.

**Handoff corrections found:** the sprint report claimed **24 actions**; the
implemented case has **26** (it under-counted `ask_allergies` and
`record_differential`). All other spot-checked numbers (scores, hashes, counts)
verified correct.

Visual evidence: [`media/`](media/) (six small JPEGs, simulator/PlayMode
captures from the committed build).

---

## 1. Case complexity inventory (from `packages/case-schema/fixtures/stemi_anterior_001/v1/case.json`)

| Metric | Count |
| --- | --- |
| File | 872 lines · 31.2 KB (+ 50 KB ECG placeholder PNG in `assets/`) |
| Actions (total) | **26** |
| — Patient (communication) | 4 (`focused_history`, `ask_allergies`, `reassure_patient`, `record_differential`) |
| — Examine | 3 (`attach_monitor`, `exam_cardiac`↻, `exam_lungs`↻) |
| — Orders | 4 (`ecg_12lead`, `order_troponin`, `order_baseline_labs`, `cxr_portable`) |
| — Treat (medication 11 + procedure 1) | 12 (aspirin, ticagrelor, prasugrel, clopidogrel, UFH, SL nitroglycerin↻, morphine, oxygen, NSAID, tenecteplase, statin; `iv_access`) |
| — More (consult 1 + disposition 2) | 3 (`activate_cath_lab`, `disposition_cath_lab`, `disposition_discharge`) |
| Hidden facts | 9 (4 `on_ask`, 5 `on_order_result`) |
| Scoring criteria | 14 (10 positive = **88 pts** · 4 harmful, penalty magnitude 30 · 5 with timing windows · 1 state-constrained) |
| Multi-accepted criteria | 1 (`c_p2y12_loading`: ticagrelor **or** prasugrel) |
| Transition rules | 8 (4 delayed `delaySec>0` · 2 `once:false` — the nitrate conditional-effect pair · 1 terminal-triggering) |
| Terminal states | 4 (`complete`, `partial`, `deteriorated`, `discharge`) |
| Result templates / assets | 21 / 1 (ECG placeholder) |
| Learning objectives | 5 |
| Unique evidence IDs referenced | 18 (all resolve in `evidence.yaml`; ledger holds 36 records, 34 `reviewRequired`) |
| Flags written | 25 · vitals/pain targets 5 · enum targets 3 (circulation, neuro, rhythm) |
| Scoring-relevant actions | 15 |
| Non-credit/distractor actions | 11 (allergies, reassure, ddx, 2 exams, 3 non-ECG orders, clopidogrel, morphine, discharge) |

**Complexity classification: MODERATE.** 872 formatted JSON lines for a
10-minute case with real timing/causality is tractable — the blueprint tables
mapped ~1:1 and authoring the JSON took well under a day. Not LOW: three real
sources of authoring complexity dominate:

1. **The scoring/criteria design** (14 criteria × timing windows × categories ×
   evidence refs × harmful/efficiency labeling) — the part that must be *right*,
   not just valid.
2. **Result templates + hidden-fact choreography** (30 learner-facing strings,
   each needing a spoiler audit).
3. **The transition-rule patterns** (delayed results, the 3-rule nitrate
   conditional) — powerful but idiomatic; authors must learn the patterns
   (now documented in clinical-engine.md).

## 2. Case structure (actual)

```
stemi_anterior_001 v1  (clinicalReview: mvp_demo_approved, clinicalVersion 0.1-draft)
├── metadata + 5 learning objectives (EV-referenced)
├── presentationProfile → ed_resus_v1 / adult_rigged_v1 (data only)
├── patient (fictional 54M, 86 kg) + initialState (HR96 118/76 96% RR18 pain8, poor_perfusion)
├── 9 hiddenFacts (history buckets + delayed result texts)
├── 26 availableActions (gating: treatments ⊣ ecg_done; UFH ⊣ +iv; dispo ⊣ cath_activated; aspirin un-gated)
├── 8 transitionRules
│   ├── ntg_bp_response + ntg_processed   (conditional-effect pair, once:false)
│   ├── t1_ischemia_worsens (≥720 s, no cath) / t2_vf_arrest (≥1080 s → terminal)
│   └── troponin(1200 s) / labs(600 s) / cxr(300 s) / cath_team(60 s) delayed results
├── 14 scoringCriteria (88 pts + 4 penalties; 1 alternatives-equivalence; 1 state-constrained)
├── 4 terminalStates (evaluation order: vf → discharge → partial-handoff → complete)
├── debriefMetadata (summary, 5 EV-cited teaching points, 5 common errors)
├── 21 resultTemplates + 1 resultAsset (ECG, provenance placeholder_replacement_required)
└── references (2 guidelines + evidence-ledger/status pointer)
```

## 3. Golden paths — exact sequences and scoring reconstruction

All from the committed `clinical-core/Qaniva.Clinical.Tests/Golden/stemi_*`
(seed 20260831). **Every total reconstructs exactly from per-action deltas**;
arithmetic shown was verified against the committed goldens and re-derived from
the criteria definitions.

| Golden | Sequence (t=sim seconds) | Outcome / terminal | Score | Hash (prefix) |
| --- | --- | --- | --- | --- |
| **ideal** | history 45 → monitor 65 → **ECG 125** → ASA 145 → IV 205 → ticagrelor 225 → UFH 245 → **cath 275** → statin 290 → handoff 320 | complete / handoff_cath_lab | **88** | `6cd7ec02` |
| **alternative** | monitor 20 → **ASA 40 (before ECG)** → history 85 → ECG 145 → IV 205 → **prasugrel** 225 → UFH 245 → cath 275 → statin 290 → handoff 320 | complete / handoff_cath_lab | **88** | `7bde240b` |
| **delayed** | history 45, allergies 60, cardiac 90, lungs 120, reassure 140, ddx 160, labs 180, CXR 210, monitor 230, IV 290, troponin 310, **ECG 370**, ASA 390, ticagrelor 410, UFH 430, *wait 200* (cxr_returns fires 630), **cath 660**, statin 675, handoff 705 | complete / handoff_cath_lab | **79.5** | `a44c5187` |
| **inefficient** | history 45, monitor 65, **O₂ 80 (−2)**, ECG 140, ASA 160, IV 220, morphine 240, **NTG 255 (SBP 118 → neutral, `ntg_processed`)**, **clopidogrel 275 (uncredited)**, troponin 295, labs 315, CXR 345, UFH 365, cath 395, handoff 425 | complete / handoff_cath_lab | **78** | `38823858` |
| **harmful** | monitor 20, ECG 80, **NSAID 95 (−10)**, **tenecteplase 125 (−10)**, ASA 145, IV 205, cath 235, handoff 265 | complete / handoff_cath_lab | **50** | `ca47ff88` |
| **deterioration** | history 45, monitor 65, ECG 125, *wait 600* → **T1 fires @725** (HR+12, SBP−22, DBP−10, pain+1, `deteriorated`), **NTG 740 @SBP 96 → `ntg_bp_response` −15 SBP, harmful −8**, *wait 400* → **T2 @1140** (arrest, unresponsive, VF rhythm) | **deteriorated** / vf_arrest_takeover | **17** | `3fd25264` |

Per-path arithmetic (criterion → awarded):

- **ideal 88** = hx 5 + monitor 5 + ECG 15 + ASA 10 + IV 5 + P2Y12 5 + UFH 5 +
  cath 25 + statin 3 + dispo 10. Breakdown: critical 50 (ECG+ASA+cath) /
  timing 10 (monitor+IV) / treatment 18 / disposition 10 / efficiency 0. Missed: none.
- **alternative 88** — identical per-criterion awards (see §4).
- **delayed 79.5** = ideal − ECG decay − cath decay:
  ECG @370 s → multiplier 1−(370−300)/300 = 0.7667 → **11.5**/15;
  cath @660 s → 1−(660−600)/300 = 0.8 → **20**/25. Everything else full
  (monitor @230 < 240; ASA @390 < 480; IV @290 < 600). 88 − 3.5 − 5 = 79.5.
  **State unchanged by the delay** (cath at 660 s beat T1's 720 s threshold):
  final vitals identical to ideal — this path demonstrates *score-only* delay;
  the deterioration path demonstrates *state* consequences.
- **inefficient 78** = 88 − 2 (O₂, efficiency) − 5 (`c_p2y12_loading` **missed** —
  clopidogrel deliberately earns nothing under the MVP default, OQ-3) − 3
  (statin missed). SpO₂ ends 98 (O₂ effect). NTG at SBP 118: cleanup rule only,
  criterion `avoided`.
- **harmful 50** = 88 − 20 (NSAID −10 + lytic −10) − 18 (missed hx 5, P2Y12 5,
  UFH 5, statin 3). Patient still reaches the lab: the penalties are score-level;
  neither drug has a modeled vital effect (harm classification review-pending, Q7).
- **deterioration 17** = hx 5 + monitor 5 + ECG 15 − nitrate 8; 7 positive
  criteria missed. Final state: **HR 108, BP 81/66** (118−22−15), SpO₂ 96,
  RR 18, circulation `arrest`, neuro `unresponsive`, rhythm
  `ventricular_fibrillation`.

## 4. Ideal vs alternative equivalence — proven

- Shared criterion: `c_p2y12_loading`, `acceptedActions:
  ["give_ticagrelor","give_prasugrel"]` (case JSON) — the *criterion* is
  scored, not the button; both drugs set the same `p2y12_given` flag.
  Clopidogrel intentionally sets a different flag and no criterion (OQ-3).
- Aspirin ordering: `give_aspirin` has `preconditions: []` (un-gated by
  design), and its window (480/720 s) is wide enough that t=40 vs t=145 both
  earn full credit. Timing treatment is identical (both inside every window).
- Resulting patient state: **flags, disclosed facts, vitals, enums and terminal
  state are identical** (verified against both goldens). `finalStateHash`
  differs (`9f2d7c30…` vs `26cdd6b2…`) because `Hashing.StateHash` includes
  `actionCounts` — the hash records *which* actions were performed;
  `replayHash` additionally includes the ordered action list. So: identical
  clinical result, distinct provenance — by design.
- Intentional & documented: BLUEPRINT "Accepted alternative pathways",
  criterion rationale, and `StemiCaseTests.AlternativePathwayScoresIdenticallyToIdeal`.

## 5. Harmful path — harm vs efficiency, precisely

| Action | Criterion | Penalty | State constraint | Canonical state consequence | Evidence | Class |
| --- | --- | --- | --- | --- | --- | --- |
| `give_nsaid_analgesia` | `c_nsaid_in_acs` (category **critical**) | −10 | none (unconditional) | pain −2 only; **no modeled vital harm** | EV-STEMI-038 | HARMFUL TO PATIENT (guideline Class 3: Harm), review-pending |
| `give_fibrinolytic` | `c_lytic_with_pci_available` (**critical**) | −10 | none | flag only; no modeled effect | EV-STEMI-023/-025 | HARMFUL-classed pending Q7 (harm vs unnecessary is an open reviewer decision) |
| `give_nitroglycerin_sl` | `c_nitrate_in_hypotension` (**critical**) | −8 | `vitals.sbpMmHg < 100` at (post-)administration | **SBP −15 via `ntg_bp_response`** — the only harmful action with a canonical state consequence | EV-STEMI-034 | HARMFUL TO PATIENT, state-dependent |
| `give_oxygen` | `c_unnecessary_oxygen` (category **efficiency**) | −2 | none | SpO₂ +2 | EV-STEMI-033 | **EFFICIENCY / not patient harm** — labeled by category + rationale |

The engine data keeps the distinction (category + rationale). **The RN Results
screen does not**: it groups by classification, so the −2 oxygen row renders
under the "Harmful actions" header (see §7 improvements).

## 6. Deterioration causality (exact)

```
t=0      HR 96  BP 118/76  poor_perfusion, alert            (initial)
t=125    ECG done (only diagnostic act; no reperfusion decision)
t=725    T1 t1_ischemia_worsens: condition = simTimeSec ≥ 720 AND !flag(cath_activated)
         → HR 108, BP 96/66, pain 9, flag deteriorated, cue distress_severe
t=740    NTG at SBP 96 → rule ntg_bp_response (flag ntg_active AND SBP<100)
         → BP 81/66; criterion c_nitrate_in_hypotension fires (−8)
t=1140   T2 t2_vf_arrest: simTimeSec ≥ 1080 AND !flag(cath_activated)
         → circulation arrest, neuro unresponsive, rhythm ventricular_fibrillation,
           flag vf_arrest → terminal vf_arrest_takeover
```

Cause: **elapsed time + one missing flag** (`cath_activated`), nothing else —
no action-order trap, no hidden constraint. Outcome is `deteriorated` (not
`death`) because the terminal state's `outcome` field says so: the blueprint
deliberately ends with "the resus team takes over" (ACLS out of scope; Q10).
`handoff_after_deterioration` (`partial`) would have applied had the learner
activated + dispositioned between T1 and T2 (covered by
`StemiCaseTests.HandoffAfterDeteriorationIsPartialNotComplete`).

## 7. Results / debrief — real data shape and quality

**Contract actually shipped to RN** (`SIMULATION_COMPLETED.summary`):
ids/versions/seed/timestamps · `terminalState` (7-value vocabulary) ·
`totalScore` · `scoreBreakdown{critical,timing,efficiency,treatment,disposition}` ·
`timeline[]{seq,simTimeSec,actionId,label,classification}` ·
`criteria[]{id,label,category,criticality,harmful,classification,creditedAtSec,awardedPoints,maxPoints}` ·
`debrief{summary,keyTeachingPoints,commonErrors}` · `replayHash`.

**Ideal-path debrief lines the engine produces today** (verbatim, generated by
executing the committed engine against the ideal script; the RN screen renders
these under "Done well"):

```
Obtained the 12-lead ECG promptly — on time (02:05), 15/15 pts
Gave the aspirin loading dose early — on time (02:25), 10/10 pts
Activated the cath lab within the target window — on time (04:35), 25/25 pts
Loaded a preferred P2Y12 inhibitor (ticagrelor or prasugrel) — on time (03:45), 5/5 pts
Started anticoagulation for primary PCI — on time (04:05), 5/5 pts
Started a high-intensity statin — on time (04:50), 3/3 pts
Attached monitoring + defib pads early — on time (01:05), 5/5 pts
Obtained IV access — on time (03:25), 5/5 pts
Took a focused history (including safety-relevant negatives) — on time (00:45), 5/5 pts
Handed the patient off to the cath lab — on time (05:20), 10/10 pts
```

Delayed path (the two lines that change):

```
Obtained the 12-lead ECG promptly — correct but delayed (06:10), 11.5/15 pts
Activated the cath lab within the target window — correct but delayed (11:00), 20/25 pts
```

Section-by-section honesty check against the target debrief model:

| Section | Status |
| --- | --- |
| Case result / outcome label / score / domain breakdown | IMPLEMENTED |
| Clinical timeline (every action, classification) | IMPLEMENTED |
| Delayed decisions ("correct but delayed" + time + partial points) | IMPLEMENTED |
| Missed actions | IMPLEMENTED |
| Harmful actions | IMPLEMENTED (but efficiency penalties render under the same header — labeling bug) |
| Critical vs recommended distinction in UI | **NOT IMPLEMENTED** (`criticality` shipped but unused by the screen) |
| Unnecessary/efficiency as its own section | **NOT IMPLEMENTED** |
| Alternative correct pathways section | **NOT IMPLEMENTED** (only implicit in the criterion label "(ticagrelor or prasugrel)") |
| Evidence/reference links | **NOT IMPLEMENTED in the contract** — `evidenceRefs` never leave the case data; RN cannot display them |
| Common errors (case-authored) | shipped in contract, **NOT rendered** |
| Replay CTA | **NOT IMPLEMENTED** ("Back to home" only) |

**Quality scores (1–5):** clarity **4** · educational usefulness **3** ·
timing explanation **4** · causality explanation **2** (T1/T2 consequences are
visible in vitals/timeline but never *narrated* — nothing says "because
activation was late, the patient deteriorated at 12:05") · critical/harmful/
unnecessary distinction **2** (data yes, UI no) · alternative-pathway
explanation **2** · evidence traceability **2** (repo-level yes, learner-level
no) · replay usefulness **2** (hash only, no CTA).

**Top 5 improvements before physician demos** (not implemented in this audit):
1. Separate "Unnecessary/efficiency" from "Harmful" in the Results UI (data
   already distinguishes via `category`).
2. Narrate causality for deterioration outcomes ("no cath-lab activation by
   12:00 → the patient arrested at 18:00") — derivable from timeline rule ids.
3. Render `commonErrors` + an explicit "accepted alternatives" line; add a
   Replay CTA.
4. Ship `evidenceRefs` (or resolved citations) per criterion so the debrief is
   learner-traceable, not just repo-traceable.
5. Surface `criticality` (critical vs recommended) in section ordering/badges.

## 8. Rigged patient — technical + candid visual audit

**Technical** (from the committed asset + generator):
`Assets/Qaniva/Art/Patients/adult_rigged_v1.fbx` — **237 KB**; ~**7,532
triangles**; **17 bones** (Hips/Spine/Chest/Neck/Head + arm/leg chains, no
fingers/toes/face bones); **2 skinned meshes** (GownBody; SkinParts with the
joined face/hair details); **3 materials** remapped at import to the shared URP
set (Skin/Gown/Hair); **zero textures**; Generic (non-Humanoid-avatar) rig; no
animation clips — all four visual states (Normal/Distressed/Unconscious/
Unresponsive) are **procedural** in `PatientVisualController` (chest-bone
translation breathing at canonical RR clamped ≤60/min; material tinting).
Prefab: root (`PatientVisualController`) → FBX instance + primitive Pillow +
primitive Blanket + 4 procedure anchors. Deterministically reproducible via
`scripts/generate-patient-blender.py`.

**Classification: B — a procedural technical placeholder with a real rig.**
It is genuinely rigged and genuinely reusable (contract-stable, any case can
use it), but it is not "a reusable rigged humanoid" in the art sense a reviewer
would assume, and it is far from production art.

**Candid visual audit** (see `media/01…03`): at the composed room camera the
scene reads as a clean stylized hospital — acceptable. Up close
(`media/02-patient-closeup.jpg`) the weaknesses are plain: **skin renders
near-white** (pale Distressed tint × bright key light washes out hands/
forearms/face), the face is minimal (closed-eye strips + brow blocks + hair cap
— readable but toy-like), the blanket is a **hard rectangular box** (no drape),
**foot tips still graze the blanket's front edge** (minor clipping), arms are
sausage-like with no elbows visible, no neck definition. Proportions are
plausible; pose and bed contact are correct; nothing floats; the bedside
monitor is crisp and legible (`media/03`). **Visual quality: EARLY MVP** —
above engineering placeholder because the composition, monitor and state
changes are coherent and demonstrable, but a physician's first comment will be
about the patient. Skin washout is the cheapest high-impact fix (tint/lighting
balance), followed by a real (purchased) character.

## 9. Generic-engine audit + delete-STEMI experiment

Disease-term grep (`stemi|anterior|aspirin|prasugrel|troponin|cath`) across
`clinical-core/Qaniva.Clinical.Core`, `packages/case-schema/{src,schema}`,
`packages/contracts/src`, Unity runtime scripts, `apps/mobile/src`,
`apps/api/src` — excluding case data, tests, docs:

| Location | Finding | Assessment |
| --- | --- | --- |
| engine / schema src / contracts / api | **zero hits** | clean |
| `IntegrationAutoPlayer.cs` (`StemiIdealPath`, `IdealPathFor`) | per-case e2e action table | test-harness data mirroring the committed golden script; double-gated (`QANIVA_INTEGRATION_AUTOPLAY` define + e2e mode) — acceptable, but should become data-driven before case #3 |
| `CaseDetailScreen.tsx` `BRIEFINGS` / `CasesScreen.tsx` `FALLBACK` | STEMI prebrief + manifest entry hardcoded in RN | **content-in-code smell** (not clinical logic): briefing text belongs in case data/manifest; flagged for the next case sprint |

**Delete-STEMI experiment** (performed in a temporary git worktree; deleted the
fixture dir, the six golden pairs, `StemiCaseTests.cs`, plus the two pieces of
STEMI test glue — the csproj fixture-copy item and `TestData.StemiCase`):

| Area | Verdict | Evidence / dependency |
| --- | --- | --- |
| Engine compiles | **YES** | `dotnet test` built clean |
| Demo case validates | **YES** | validator: 1 file, 0 failures |
| Demo golden replay | **YES** | **46/46 tests green** — exactly the pre-STEMI count |
| Result assets generic | **YES** | resolution lives in `Simulation`/`CaseLoader` keyed by case data; viewer keyed by assetId |
| Criterion debrief generic | **YES** | `ScoringEngine.BuildCriterionResults` reads only the rubric |
| Terminal outcomes generic | **YES** | plain string vocabulary; demo uses old values untouched |
| Unity compiles/runs | **YES** (with the STEMI PlayMode test deleted alongside, as the premise states; the driver's dead table entry is inert data) |
| RN Results | **YES** | fully summary-driven; the RN fallback/briefing entries would reference a non-loadable case id — cosmetic, not structural |

## 10. Anaphylaxis reusability (thought exercise, nothing built)

history facts **YES** · exam findings **YES** (static text; state-dependent
text remains GAP-3, optional) · medications **YES** (epinephrine = medication
action + effects; **dose/route choice UI is the real gap — QAN-006b** if IM-vs-IV
epinephrine is a learning point) · timing windows **YES** · harmful actions
**YES** (incl. state-constrained) · delayed deterioration **YES** (biphasic
reaction = a second delayed rule) · alternative criteria **YES** · terminal
states **YES** · result assets **YES** · presentation profile **YES** (same
room/patient keys) · rigged patient **YES** (airway/skin visuals like urticaria
or stridor audio would be presentation wishes, not engine gaps) · monitor
**YES** · Results/debrief **YES**. → **One expected engine-adjacent gap:
parameterized action input (QAN-006b)**; everything else is case data.

## 11. Case-factory effort distribution (relative, from the real STEMI run)

| Work | Share of total effort | Reusable now? | Automatable by agent? | Needs clinician? |
| --- | --- | --- | --- | --- |
| Research + evidence ledger | ~20% | process reusable; content per-case | mostly (with source flags) | verification |
| Blueprint | ~15% | templates reusable | mostly | judgment on scope/timing |
| Clinician review | (external) | process reusable | **no** | **entirely** |
| Case JSON authoring | ~10% | schema/patterns reusable → cheap now | yes (blueprint→JSON is mechanical) | no |
| Golden scripts + QA tests | ~10% | harness reusable | yes (design them in the blueprint) | no |
| Image/asset work (ECG-class) | ~10% | viewer/pipeline reusable; **asset per case** | placeholder yes; real asset **no** | verification |
| Unity/presentation | ~25% this time | **now ~0 for a same-room case** (one-time cost paid) | — | no |
| Debrief design | ~5% | structure reusable | mostly | claim verification |
| RN glue (briefing/fallback/e2e entry) | ~5% | should move to data | yes | no |

Linear-per-case: research content, clinician review, real diagnostic assets.
Near-zero-marginal now: engine, schema, Unity presentation, debrief plumbing.

## 12. Review-pending clinical items (extract; resolution NOT attempted)

34 of 36 evidence records carry `reviewRequired: true`. The decision-level
items:

| ID | Topic | Current MVP default | Why review needed |
| --- | --- | --- | --- |
| D1 | aspirin loading dose | 300 mg chewed displayed | AHA 162–325 vs ESC 150–300 divergence |
| D2 | lytic timing metric | ESC framing recorded (unused in v1) | divergence, future transfer case |
| D3 | P2Y12 pre-treatment timing | any time before case end, unpenalized | guideline nuance |
| D4 | early beta-blockade | excluded from case | class divergence |
| D5 | PPCI timing anchor | arrival-to-cath-activation proxy, 600/900 s | proxy + compression need sign-off |
| D6 / Q5 | ED-administered UFH | present, RECOMMENDED +5 | ED-vs-cath-lab practice varies |
| Q1 | initial vitals + `poor_perfusion` | HR96 118/76 96% RR18 pain8 | realism |
| Q2 / S4 | ECG assist policy + asset | raw tracing only; **placeholder image** | asset must be clinician-verified |
| Q3 | ECG-gating of treatments | visible+disabled until `ecg_done`; aspirin un-gated | pedagogy check |
| Q4 | all timing windows, T1=720 s, T2=1080 s | labeled pedagogical compression | clinical defensibility |
| Q6 | nitrate-in-hypotension −8 / SBP −15 | implemented | magnitude plausibility |
| Q7 | lytic-with-PCI-available | HARMFUL −10 | harm vs unnecessary |
| Q8 | discharge terminal | `discharge` outcome, heavy implicit loss | keep/refine |
| Q9 | differential formative-only | unscored `record_differential` | agree/score |
| Q10 | VF ends as `deteriorated`, not death | implemented | framing |
| OQ-3 | clopidogrel credit | zero credit | Turkish-context call |
| OQ-5 | national MoH pathway citation | none found | reviewer may supply |
| OQ-7 | troponin 20-min delay | implemented | teaching-device acceptability |
| D-IMPL-1..4 | rhythm string, static exam text, rigged patient, placeholder ECG | see BLUEPRINT deviations table | recorded, review-pending |

## 13. ECG placeholder status

File: `Resources/Qaniva/CaseAssets/ecg_stemi_anterior_v1.png` (= fixture
`assets/` copy, 50 KB, 2170×1778), generated deterministically by
`scripts/generate-ecg-placeholder.py` (first-party; no external content).
Visually it **does resemble** a 12-lead ECG (3×4 + rhythm strip, calibration
pulses, 25 mm/s grid, schematic anterior ST-elevation morphology) — which is
exactly why the safeguards matter: the image itself carries the red header
**"TRAINING PLACEHOLDER - SCHEMATIC TRACING - NOT A DIAGNOSTIC ECG -
REPLACEMENT REQUIRED"**, and the case data carries
`provenance.clinicalStatus: "placeholder_replacement_required"`. Replacement =
drop a verified PNG under the same assetId + update provenance + asset
manifest (mechanically trivial; the viewer/pipeline is asset-agnostic).
**Recommendation: SAFE FOR PHYSICIAN DEMO WITH DISCLAIMER** (the watermark is
one line at the top and could be scrolled past — say it out loud in the demo);
**MUST REPLACE before any student-facing beta**, since students are the
population that could absorb a schematic tracing as ground truth.

## 14. MVP readiness (engineering/product judgment, 0–100)

| Area | Score | Biggest limiter |
| --- | --- | --- |
| Technical architecture | 90 | codegen'd bridge mirror still manual (QAN-035); briefing content-in-code |
| Deterministic simulation | 95 | nothing material; param actions untested (unused) |
| Case-authoring system | 85 | e2e driver + RN glue not yet data-driven; blueprint→JSON still manual |
| Clinical content maturity | 40 | **no clinician has reviewed anything** (the gate is the point) |
| 3D presentation | 70 | room fine at demo camera; no animation clips |
| Patient visual quality | 45 | washed skin, boxy blanket, toy face (EARLY MVP) |
| Simulation interaction UX | 70 | functional dev-styled UI; no param input; result banner utilitarian |
| Results/debrief UX | 60 | rich data, thin rendering (see §7 gaps) |
| Testing | 90 | no Unity CI (QAN-024); blind playtest missing |
| Release readiness | 30 | no device run (QAN-008 deferred), no persistence, no crash reporting, no TestFlight |

**TECHNICAL MVP READINESS: ~80%** (limiter: device validation + persistence).
**PHYSICIAN DEMO READINESS: ~65%** (limiter: patient visual quality + debrief
rendering gaps; content is demoable *as a draft* with the pending-validation
framing). **CLOSED STUDENT BETA READINESS: ~35%** (limiters: clinician
approval is mandatory before students, real ECG asset, device+distribution
pipeline, attempt persistence).

## 15. Top 10 remaining MVP items

| # | P | Item | Why | Depends on | Blocks physician demo? | Blocks student beta? |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | P0 | Clinician review of REVIEW.md (QAN-012C) + post-review case v2 | the entire clinical claim | a clinician | no (demo *collects* it) | **yes** |
| 2 | P0 | Clinician-verified ECG asset replacing the placeholder | students must not learn from a schematic | #1 (verifier) | with disclaimer, no | **yes** |
| 3 | P0 | Patient visual pass: fix skin washout + blanket shape now; decide the RECOMMENDED PURCHASE | first thing physicians will see | — | partially | yes (quality bar) |
| 4 | P1 | Results/debrief rendering: efficiency≠harmful split, causality narration, alternatives line, common errors, replay CTA | data exists; the payoff screen undersells it | — | ideally not | yes |
| 5 | P1 | QAN-008 physical-device run (still deferred — schedule before any distribution) | simulator-only evidence so far | device access | no | **yes** |
| 6 | P1 | Attempt persistence + event upload (QAN-007/QAN-010 minimal) | testers' results must survive the session | backend choice | no | yes |
| 7 | P1 | Blind playtest by a non-author (+ fix findings) | INACSL pilot requirement; UX blind spots | — | helpful | yes |
| 8 | P1 | Move briefing/manifest/e2e path data out of code into case data | case factory hygiene before case #2 | — | no | no |
| 9 | P2 | QAN-006b parameterized action input | needed by anaphylaxis-class cases | — | no | no (for STEMI-only beta) |
| 10 | P2 | Unity CI (QAN-024) + crash reporting (QAN-018) | regression + field visibility at beta scale | mac runner | no | yes (at scale) |

## 16. Validation performed for this audit

`pnpm run format:check` ✅ · `pnpm run lint` ✅ · `pnpm run validate:cases` 2/2 ✅ ·
`dotnet test` **67/67** ✅ · Unity EditMode **18/18** ✅ · Unity PlayMode
**13/13** ✅ (all re-run at `2c15652`) · delete-STEMI worktree: engine **46/46**
✅ · debrief lines generated by executing the committed engine against the
committed golden scripts (throwaway harness, not committed). No simulator
rebuild was needed — the media files are captures from this tree's simulator
run and PlayMode captures. No device watchers started; QAN-008 remains deferred.
