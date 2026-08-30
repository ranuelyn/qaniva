# Qaniva MVP Blueprint (Canonical)

> **Source of truth.** This document is the AI-readable restructuring of
> `Qaniva_ReactNative_Unity_3D_MVP_Blueprint_Agustos_2026.docx` (dated 30 August 2026).
> Product decisions in the DOCX are preserved. Where this document adds an interpretation
> or a proposal that is not in the DOCX, it is tagged **[Recommendation]** or
> **[Open Question]**. If this file and the DOCX disagree on a product decision, the DOCX wins
> and this file must be corrected.

---

## 1. Decision summary — what we build now

Qaniva is a **mobile-first, 3D clinical decision simulation platform**. The MVP goal is a
working, convincing, testable beta that can be mailed to first users/institutions
(students, educators) who can install it and complete one case in ~5–10 minutes without
developer help.

Core decisions:

| Area | Decision |
| --- | --- |
| Dimensionality | 3D, but **not** an open world. One reusable clinical room + modular patient/prop system + deterministic simulation engine. 3D is a **presentation layer**. |
| Clinical truth | Lives in test-driven case/state data, **never** in 3D objects or LLM prompts. |
| Case count for first outreach | **2 production-quality cases + 1 short third variant.** Not 6–10. STEMI + Anaphylaxis are the ideal start; third can be Hyperkalemia, SVT, or Tension pneumothorax. |
| Product shell | React Native + TypeScript owns Home, Cases, Profile, Progress, Debrief. |
| 3D runtime | Unity 6 + URP, full-screen simulation runtime only. |
| Clinical engine | Pure C# assembly, independent of Unity. RN produces no clinical rules; Unity is not the source of clinical truth. |
| Backend | Thin: auth, case version distribution, attempt/event upload, AI gateway, analytics. **No microservices in MVP.** |
| AI scope | Anamnesis/patient dialogue + debrief narration only. LLM does **not** produce vitals, labs, drug results, scores, or state transitions. |
| First distribution | iOS TestFlight + Android internal/closed test links via email outreach. **No public App Store launch required.** |
| Success metric | Case completion, critical-action latency, replay, and the "I want to solve this again" signal — **not** "do the graphics look good?" |

---

## 2. Product backbone (from the source reports)

The competitor teardown's key finding: most of the competitor's value comes **not** from 3D
visuals but from the combination of case schema, action catalog, state outcomes, scoring,
debrief, and content library. Principle for Qaniva: **medical truth in a deterministic
engine; LLM in the presentation/tutor/debrief layer.**

| Source finding | MVP impact |
| --- | --- |
| Competitor: 3D patient + dynamic deterioration + scoring + Patient/Tutor AI | 3D scene is useful but is a visual layer bound to the state engine. |
| Competitor screen inventory: 105 screens/states | No 1:1 copy. Outreach MVP is solvable with ~18–24 user-visible states. |
| Qaniva report: action order and timing scoring opportunity | Engine must emit an event timeline from day one. |
| Qaniva report: 5 main mobile areas | Keep Patient / Examine / Orders / Treat / More structure. |
| Qaniva report: clinician review and version metadata | Every case is a versioned artifact, reviewed like code. |
| Qaniva report: not a real-patient CDS | MVP limited to fictional/synthetic cases. |

### 2.1 Reducing 105 states to the MVP

Most dense areas in the competitor inventory: Results (10), Educator Dashboard (10),
Investigate (8), Simulation (8), Case Library (7), Profile (7). The outreach MVP **cuts**
dashboard, creator, subscription, challenges, and offline.

| MVP flow step | Example states needed |
| --- | --- |
| Launch/Home | Splash, Home, Case card, Case briefing |
| Simulation room | Main room, vitals monitor, timer, action feedback |
| Patient | Background + text Patient AI chat |
| Examine | System list + selected finding result |
| Orders | Lab/ECG/imaging list + result viewer |
| Treat | Medication/procedure picker + result |
| More | Differential, consult, disposition, case log |
| Results | Overall, clinical timeline, missed/harmful, debrief, replay |

---

## 3. The 3D decision — yes, but bounded

2D/2.5D was previously preferred for smaller app size, lower GPU/battery, and fast
iteration; those reasons are still valid. 3D becomes worthwhile because of the new priority:
making patient variation, scene variation, and hospital props reusable. The critical framing
is to design 3D as a **modular content system**, not a graphics project.

| 3D gain | How we use it |
| --- | --- |
| Patient variation | One humanoid rig; skin/hair/clothing/body variants; same animation controller. |
| Clinical state appearance | A small number of state-driven animations/blendshapes (dyspnea, consciousness, pain, arrest). |
| Room variation | Resus room prefab; slot-based props (monitor, trolley, oxygen, IV pole). |
| Different settings | ED bay / ward / ambulance prefab combinations from the same asset set; **MVP has one main room only.** |
| Camera | Fixed/semi-fixed cinematic camera; no free roam. |
| Content updates | Case-specific asset references packaged with Addressables, distributed remotely when needed. |

**MVP 3D budget:** 1 room, 1 main bed, 1 monitor prefab, 1 trolley, 1 oxygen/IV set,
2 patient looks, 6–10 core animations. Data-driven variations of one scene rather than
"a new scene per case."

### 3.1 Mobile 3D performance rules

- Unity 6 URP. No realtime global illumination; prefer baked lighting/lightmaps.
- 1 main shadow-casting light; other lights baked or shadowless.
- LOD Groups; low-poly background props; no unnecessary rigged objects.
- Texture atlas + ASTC/ETC2 platform profiles; no 4K textures. 1K/2K is enough except patient face/hands.
- Keep material and draw-call counts low; GPU instancing on repeated props.
- Particles only for necessary clinical feedback; minimal post-processing.
- Target: stable 30 FPS on mid-tier devices; 60 FPS optional on good devices. Frame budget must be reported.
- Profiler must run on real iOS/Android devices; Editor FPS is not an acceptance criterion.

---

## 4. Tech stack

| Layer | Recommendation | Why |
| --- | --- | --- |
| Mobile app shell | React Native + TypeScript (Expo development build or RN CLI) | Preserves team experience; fast iteration of Home, Cases, Profile, Progress, Debrief, auth, analytics, native product UI. |
| 3D simulation runtime | Unity 6 LTS/6000.x + C# + URP, Unity as a Library | Opens as a full-screen simulation module from RN; strong 3D/animation tooling, mobile profiler, Addressables. |
| RN state/navigation | Expo Router or React Navigation + Zustand + TanStack Query | Separates product navigation/state/server-cache from Unity; testable TS contracts. |
| Clinical core | Pure C#, .NET-compatible assembly | Deterministic state machine, scoring, replay tests with no Unity dependency. |
| Case format | JSON (schema validated) + version + asset keys | Separates content from presentation; RN/Unity/backend speak the same versioned contract. |
| Backend API | NestJS (recommended) or FastAPI | RN/TS experience makes NestJS the natural choice; thin BFF/API for auth, attempts, AI, case manifest. |
| Database | PostgreSQL + JSONB | User/attempt relational; case graph and logs flexible. |
| Managed backend | Supabase Postgres/Auth/Storage (optional) | Fast MVP. Still prefer a BFF over Unity talking directly to the DB. |
| Object storage/CDN | S3-compatible / Supabase Storage + CDN | ECG, imaging, audio, 3D/Addressable bundles, thumbnails. |
| AI gateway | Provider-agnostic backend adapter + JSON schema output | Vendor swap; prompt and safety versioning. |
| Analytics | PostHog/Amplitude/Firebase Analytics or first-party event endpoint | RN funnel + Unity action timeline correlated by `attemptId`. |
| Observability | Sentry React Native + Unity/backend structured logs | RN crash, Unity runtime error, API and AI failure visibility. |
| CI/CD | GitHub Actions + EAS Build/Submit + Unity build pipeline | Combines RN native build/signing with the Unity library artifact in one release flow. |
| Design/3D content | Figma + Blender + licensed assets + Git LFS | Product UI in the RN design system; 3D pipeline separate and modular. |

**RN + Unity decision:** the v1 architecture is a hybrid — React Native product shell,
Unity only as the full-screen 3D simulation module. Unity as a Library embeds into a native
host app on iOS/Android; current Unity docs support only full-screen rendering on mobile, so
do not try to place the simulation inside a small part of an RN screen. Navigation model:
**RN screen -> full-screen Unity simulation -> RN results/debrief.** If Expo is used, the
Development Build / Prebuild flow with custom native code is required (not Expo Go). The
bridge stays small: `startSimulation(config)`, `sendAction/event`,
`simulationCompleted(summary)`, `runtimeError`. Do not go deep on product development before
an iOS + Android bridge smoke test passes in the first 2–3 days.

---

## 5. Technical architecture

### 5.1 Runtime split

```
React Native Product Shell (TypeScript)
  ├─ Home / Cases / Case Briefing
  ├─ Auth / Profile / Progress
  ├─ Results / Timeline / Debrief
  ├─ Analytics / Notifications / Feedback
  └─ UnityHostScreen (full-screen)
        │ startSimulation(caseId, attemptId, locale, difficulty)
        ▼
Native Bridge (iOS + Android)
        │ lifecycle + typed messages
        ▼
Unity Presentation Runtime
  ├─ Room / Patient / Monitor / Camera
  ├─ Animation / Presentation Adapters
  └─ In-simulation action UI (only what is necessary)
        │ PlayerAction
        ▼
Clinical.Core (PURE C#)
  ├─ CaseDefinition
  ├─ SimulationState
  ├─ RuleEvaluator
  ├─ Timeline/EventLog
  ├─ ScoringEngine
  └─ Deterministic RNG (seeded, if needed)
        │ StateSnapshot + PresentationCue
        ▼
Unity visual update
        │ simulationCompleted / attempt summary
        ▼
React Native Results + Debrief

Backend
  ├─ Auth / User / Progress
  ├─ Case Manifest + Versions
  ├─ Attempt/Event Upload
  ├─ AI Gateway (Patient + Debrief)
  └─ Admin/Analytics API
```

The product carries two runtimes with clear responsibilities: React Native manages app
lifecycle/navigation/product UI; Unity is activated only during a simulation. **Do not build
shared global state between RN and Unity.** Carry only versioned, small messages plus
`attemptId` over the bridge. Unity returns a summary/timeline reference on exit; the long
event log is written to the backend or a local persistence layer.

### 5.2 The golden rule — the engine is independent of Unity

Do not store case logic inside `MonoBehaviour`, `Animator` callbacks, or scene object state.
Given the same case JSON and the same action sequence, the engine must produce the same
timeline, final state, and score independent of the Unity Editor. This is the most critical
architectural decision for clinical reliability, regression testing, and future
web/headless replay.

| Domain object | Minimum fields |
| --- | --- |
| `CaseDefinition` | `id`, `version`, `title`, `learningObjectives`, `initialState`, `allowedActions`, `transitions`, `rubric`, `presentationProfile` |
| `PatientState` | `vitals`, `airway`, `breathing`, `circulation`, `neuro`, `pain`, `rhythm`, `hiddenFlags`, `elapsedTime` |
| `ActionDefinition` | `id`, `type`, `params`, `preconditions`, `timeCost`, `effects`, `resultTemplate`, `criterionIds`, `visibility` |
| `TransitionRule` | `condition`, `priority`, `delay`, `stateDelta`, `terminalState`, `presentationCue` |
| `Criterion` | `criticality`, `acceptedActions`, `timingWindow`, `stateConstraints`, `scoreRule`, `rationale`, `evidenceRefs` |
| `AttemptEvent` | `seq`, `simTime`, `actionId`, `params`, `beforeHash`, `afterHash`, `triggeredRules`, `scoreDelta` |
| `AIContext` | `persona`, `allowedFactIds`, `disclosedFactIds`, `currentStateSummary`, `safetyPolicyVersion` |

---

## 6. Case schema — the first real technical contract

The most important week-1 output is the case schema, not a pretty room. Use a versioned,
testable contract. Illustrative shape (values are software-contract illustration only, not
clinically reviewed):

```json
{
  "id": "stemi_001",
  "version": 3,
  "scene": "ed_resus_v1",
  "patientVisual": "male_58_v1",
  "initialState": { "hr": 104, "sbp": 146, "spo2": 96, "pain": 8 },
  "hiddenFacts": ["chest_pain_45m", "no_aspirin_allergy"],
  "actions": [
    { "id": "ecg_12lead", "type": "order", "timeCostSec": 120,
      "effects": [{ "setFlag": "stemi_ecg_available" }] },
    { "id": "aspirin_300", "type": "medication", "timeCostSec": 30,
      "preconditions": ["no_active_bleeding"], "effects": [{ "setFlag": "aspirin_given" }] }
  ],
  "transitions": [
    { "when": "elapsedSec > 600 && !reperfusion_activated",
      "effects": [{ "delta": "sbp", "value": -25 }, { "setFlag": "deteriorating" }] }
  ],
  "rubric": [
    { "criterion": "time_to_ecg", "critical": true, "fullBeforeSec": 600 }
  ]
}
```

Every clinical number and decision in a real schema must pass clinician review.

---

## 7. Deterministic simulation engine v0

1. `LoadCase(caseVersion)` -> immutable `CaseDefinition`.
2. `Initialize()` -> initial `PatientState` + `simTime = 0` + empty event log.
3. `GetAvailableActions()` -> action list filtered by precondition/visibility rules.
4. `ApplyAction(actionId, params)` -> validation -> time cost -> action effect -> transition evaluation -> new state.
5. Emit event-log entry -> before/after state hash + triggered rules.
6. `EvaluateTerminal()` -> terminal state (discharge/admit/death/complete).
7. `ScoreAttempt()` -> critical/timing/efficiency/treatment/disposition scores over the rubric.
8. `BuildDebriefFacts()` -> structured correct/missed/delayed/harmful decisions, independent of the LLM.

**Replay invariant:** same case version + same action sequence + same seed = same final
state + same score. CI must run this guarantee as a regression test.

---

## 8. AI layer — minimal but safe

For the outreach MVP, do not spread AI everywhere. Two uses are enough: (1) text Patient AI
anamnesis; (2) personalized debrief narration at the end of a case. Tutor, voice, authoring
assistant, and adaptive next-case are later phases.

| AI feature | Input | LLM may | LLM may NOT |
| --- | --- | --- | --- |
| Patient AI | User question + allowed fact IDs + persona + disclosed facts | Interpret the question, produce a natural answer from defined facts. | Invent new symptoms, vitals, drugs, history, or diagnosis; change state. |
| Debrief AI | Deterministic timeline + rubric results + approved evidence notes | Explain the user's reasoning pattern; turn it into teaching text. | Recompute the score; invent the correct path. |

- Backend-only API keys; no LLM secret inside the Unity binary.
- Every model response validated with JSON schema / structured output.
- Allowed-fact IDs are requested back with the answer; on an unknown fact, reject/fallback.
- Deterministic fallback template: if AI times out / returns invalid, the simulation does not stop.
- Log prompt version, model id, latency, schema validation result, and safety flag.
- If a real-patient question is detected, keep the "this app is an educational simulation" boundary.

---

## 9. 3D scene and asset architecture

```
Assets/Qaniva/
  Scenes/
    Bootstrap.unity
    Shell.unity
    Rooms/ED_Resus.unity
  Prefabs/
    Patient/PatientRig.prefab
    Equipment/VitalMonitor.prefab
    Equipment/IVPole.prefab
    Environment/Bed.prefab
  PresentationProfiles/
    stemi_001.asset
    anaphylaxis_001.asset
  Animations/
    Idle, DistressMild, DistressSevere, Unconscious, Arrest, Recovery
  Addressables/
    CoreRoom
    PatientVariants
    CaseMedia
  UI/
    Screens, Components, USS, UXML
```

| Presentation profile field | Example |
| --- | --- |
| `roomKey` | `ed_resus_v1` |
| `patientVariant` | `male_58_v1` |
| `animationStateAtStart` | `distress_mild` |
| `monitorLayout` | `ed_standard` |
| `requiredProps` | `oxygen_wall, iv_pole, crash_cart` |
| `cameraPreset` | `bedside_01` |
| `audioProfile` | `room_ambience_low` |

This builds a case -> presentation profile -> prefab/asset key chain instead of embedding
case logic in a scene file. The same STEMI case can later be replayed with an ambulance
presentation profile; the clinical core does not change.

---

## 10. Outreach MVP user experience

| Step | Screen/interaction | Acceptance criterion |
| --- | --- | --- |
| 1 | Home | RN — single main CTA: "Start Case". Login optional/guest. |
| 2 | Case Briefing | RN — chief complaint + triage + learning teaser; no diagnosis spoiler. |
| 3 | 3D Room | Unity full-screen — patient + vitals + timer + 5 main action buttons. Understandable in 3 seconds. |
| 4 | Patient | Unity-native minimal panel; Patient AI goes to backend gateway. |
| 5 | Examine | Unity — system list; selecting shows a short finding + audio/image if needed. |
| 6 | Orders | Unity — ECG/lab/imaging. Sim time may advance until the result is ready. |
| 7 | Treat | Unity — drug/procedure; dose/route parameterized where the case needs it. |
| 8 | More | Unity — differential + consult + disposition + case log. |
| 9 | Debrief | RN — timeline: "2:12 ECG / 3:05 aspirin / 11:40 activation"; critical/missed/harmful. |
| 10 | Replay/Share | RN — retry + beta feedback. Replay reopens `UnityHostScreen` with same/new attempt config. |

---

## 11. AI development agents and the skill system

The "agents" here are **expert software agents that speed up the development team's
code/asset/content work**, not autonomous clinical actors inside the product. Instead of
giving one dev agent full repo authority and saying "build Qaniva," use specialist roles
bounded by deterministic acceptance criteria.

| Agent | Responsibility | Main output | Must not write |
| --- | --- | --- | --- |
| Director / Integrator | Scope, task decomposition, PR acceptance, dependency ordering | Sprint plan, issue specs, merge checklist | Does not alone approve clinical truth or 3D asset production |
| Clinical Schema Agent | Case JSON/schema, rubric mapping, clinical content diffs | Case draft + validation report | Does not publish without clinician approval |
| Simulation Core Agent | Pure C# FSM/rules/scoring/timeline | Unit-tested core assembly | Does not embed clinical logic in Unity scene/UI code |
| Unity 3D Agent | Scene/prefab, state->animation adapters, Unity as a Library lifecycle, mobile perf | Reusable room/patient prefabs + exported Unity library build | Does not change case medical truth |
| React Native Product Agent | RN navigation, screens, app state, `UnityHostScreen`, bridge JS/TS contract | Home/Cases/Results/Profile + typed Unity bridge | Does not copy engine rules; does not write Unity scene logic |
| Backend/API Agent | Auth, cases, attempts, AI gateway, storage; RN/Unity contract endpoints | API + migrations + contract tests | Does not leak client secrets |
| AI Safety Agent | Patient/debrief prompt contract, schemas, evals, fallback | Golden eval set + guardrails | Does not produce clinical outcomes |
| QA/Replay Agent | Determinism, regression, device test, scenario paths | Test matrix + bug report | Does not guess expected results from the prompt |
| Release Agent | Build, signing checklist, TestFlight/Play test, crash symbols | Beta artifacts + release notes | Does not fill store policy declarations without verification |

### 11.1 What a skill is and how to write one

A skill is the agent's persistent working contract that it should not have to relearn every
task. Keep it short: repo facts, invariants, commands, and acceptance criteria — not "how to
think."

```
skills/
  qaniva-product-scope/SKILL.md
  clinical-case-schema/SKILL.md
  deterministic-engine/SKILL.md
  react-native-product-shell/SKILL.md
  rn-unity-bridge/SKILL.md
  unity-mobile-3d/SKILL.md
  backend-contracts/SKILL.md
  ai-patient-safety/SKILL.md
  qa-replay/SKILL.md
  release-mobile/SKILL.md
```

| Skill section | Content |
| --- | --- |
| Purpose | What job does this skill solve? |
| Inputs | Which file/contract must be read? |
| Invariants | Rules that must never break. e.g. LLM does not mutate state. |
| Commands | Test/build/lint/validation commands. |
| Definition of Done | Machine-verifiable acceptance criteria. |
| Forbidden shortcuts | e.g. hard-coded clinical result, hidden truth in a scene, deleting tests. |
| Handoff format | Which file/report is left for the next agent? |

> **[Recommendation]** This repository implements the skill set under `skills/` with a
> slightly consolidated naming (`qaniva-architecture`, `react-native-mobile`,
> `unity-mobile`, `unity-rn-bridge`, `deterministic-clinical-engine`, `case-authoring`,
> `clinical-safety`, `testing-and-golden-replay`, `coding-standards`, `git-and-release`).
> The mapping to the DOCX list is 1:1 in intent.

---

## 12. Monorepo proposal

```
qaniva/
  README.md
  AGENTS.md
  docs/
    product/MVP_SCOPE.md
    architecture/ADR-001-rn-unity-hybrid.md
    architecture/ADR-002-deterministic-core.md
    architecture/ADR-003-rn-unity-message-contract.md
    clinical/CASE_AUTHORING_GUIDE.md
    qa/DEVICE_MATRIX.md
  skills/
  apps/
    mobile/                 # React Native + TypeScript
      app/ or src/
      modules/unity-host/
      tests/
  unity/
    QanivaSimulation/
      Assets/Qaniva/...
      Packages/
      ProjectSettings/
  clinical-core/
    Qaniva.Clinical.Core/
    Qaniva.Clinical.Tests/
  backend/
    src/
    tests/
    migrations/
  packages/
    contracts/             # Zod/JSON schemas, event names, generated types
  cases/
    schema/case.schema.json
    stemi_001/v1/case.json
    anaphylaxis_001/v1/case.json
  tools/
    validate_case.py
    replay_case.py
    export_case_bundle.py
  .github/workflows/
```

- Case JSON pull requests are reviewed like code PRs.
- Golden replay tests run automatically on every case change.
- Binary 3D assets in Git LFS; source Blender files and license metadata in a separate folder.
- ADRs keep critical decisions written down so agents do not "reinvent" the architecture.

> **[Recommendation]** This repo's actual layout differs slightly for clarity: `apps/api`
> instead of top-level `backend/`, `packages/case-schema` holds the schema + fixtures +
> validator (replacing `cases/` + `tools/`), and ADRs live in `docs/adr/`. Intent preserved.

---

## 13. Agent working protocol

1. Director writes a task spec: goal, files to touch, out-of-scope, acceptance criteria.
2. The specialist agent first reads the relevant skill + ADR + tests.
3. Produces a small diff; does not change engine + UI + backend in one task.
4. The agent writes/runs its own test and attaches the command output to the handoff.
5. The QA agent replays the same action sequence in the headless core and compares against the expected snapshot.
6. If there is a clinical change, it is not "done" without a clinician review checklist.
7. The integrator merges only after green CI + review.
8. At the end of each sprint, skills/ADRs are updated; keep prompt knowledge separate from persistent repo knowledge.

**Most important agent rule:** agents do not get "looks like it works, so it's fine"
authority. Especially in the engine and clinical content, the output must have a verifiable
test or a reviewer sign-off.

---

## 14. Test strategy

| Test layer | Example | When |
| --- | --- | --- |
| Schema validation | `case.json` required fields, unique IDs, no dangling criterion | Every PR |
| Pure core unit tests | precondition, time cost, transition priority, scoring | Every PR |
| Golden replay | STEMI ideal path -> expected timeline/hash/score | Every engine/case change |
| Counterfactual replay | aspirin delayed / harmful action -> expected state | Case review |
| AI eval | allowed facts, role break, invalid schema, prompt injection | Prompt/model change |
| Unity play mode | state snapshot -> expected visual cue/UI | Every UI/presentation PR |
| Device perf | FPS, memory, thermal, load time | Weekly + release |
| End-to-end beta | new install -> case -> debrief -> replay -> feedback | Release candidate |

### 14.1 Golden paths for the first two cases

Clinical detail must be verified by a physician on the team. From a software perspective,
each case needs at least these paths as test fixtures: ideal path, delayed critical action,
wrong-but-harmless path, harmful/contraindicated path, early disposition, AI-unavailable path.

---

## 15. Four-week email-ready MVP plan

Target: not a market-ready product but a working beta that, when mailed, lets people open it
and understand Qaniva's value in 5–10 minutes. Feasible if 2 developers work in parallel;
early start on 3D assets and case review is critical.

| Week | Tech A – Engine/Backend | Tech B – RN/Unity | Clinical/Content | Exit criterion |
| --- | --- | --- | --- | --- |
| 0 (2–3 days) | Repo, case schema spike, API skeleton | RN shell + Unity URP project + Unity as a Library iOS/Android bridge smoke test | STEMI + Anaphylaxis blueprint | RN Home -> Unity full-screen -> RN Results round-trip + JSON load |
| 1 | Core FSM, event log, scoring v0, replay tests | RN Home/Cases/Briefing; Unity resus room, patient rig, monitor, action shell | STEMI v1 facts/rubric | STEMI ideal path headless + visual room |
| 2 | Orders/treat effects, case API, attempt upload | Unity Patient/Exam/Orders/Treat/More; RN attempt shell + navigation polish | STEMI review + Anaphylaxis v1 | STEMI played end to end |
| 3 | AI gateway, patient facts, debrief facts, analytics | RN Results/Timeline/Debrief; Unity patient AI UI + mobile perf + lifecycle hardening | Anaphylaxis review + copy | 2 cases end-to-end, no severe logic bug |
| 4 | Crash/error instrumentation, remote manifest, beta ops | RN onboarding/feedback + EAS/native release hardening; Unity bundle/version checks + 60–90s demo capture | Blind playtest fixes, third mini-case optional | TestFlight + Android test link + landing/demo + mail kit |

**Scope cut list (first 4 weeks):** subscription, social login, leaderboard, Daily Patient,
Challenges, offline sync, Educator dashboard, Creator, voice AI, 5 environments, 20 avatars,
open-world navigation, full physiology engine — **none of these.**

---

## 16. First 72 hours

1. Create the RN app shell (TypeScript). If Expo: use Development Build/Prebuild, not Expo Go. Same day: create the Unity 6 URP project and start the Unity-as-a-Library iOS/Android embed spike.
2. On a real device, run RN -> full-screen Unity -> RN on both iOS and Android. See lifecycle, orientation, audio focus, memory, and back-navigation issues at the start.
3. Open the `clinical-core` pure C# project; set up the assembly boundary referenceable from Unity and the RN/Unity message contract schemas.
4. Write `case.schema.json` v0; create a toy STEMI case with only 5 actions + 2 transitions.
5. Write a headless replay CLI/test: JSON + action list -> event timeline.
6. In Unity, run the toy case with a placeholder cube/humanoid + vitals text; send `caseId/attemptId` from RN and show the completion event on the RN Results screen.
7. Backend `/health`, `/cases/:id`, `/attempts` endpoints and an AI gateway stub.
8. In Figma, create a 10-screen happy-path wireframe only.
9. Open the first asset license table: source, license, allowed commercial use, attribution, modification.
10. CI: core tests + case schema validation. Require green for merge.

---

## 17. What the mail-ready MVP package looks like

| Piece | Minimum content |
| --- | --- |
| Beta link | iOS TestFlight public/invite link + Android internal/closed test opt-in. |
| Landing page | What it is in 30 seconds, 3 screenshots/GIFs, "5-minute beta" CTA, feedback form. |
| 90s demo | Briefing -> 3D room -> ECG/order -> deterioration/treatment -> clinical timeline -> debrief. |
| 1-page PDF | Problem, why not a quiz, deterministic engine, clinician-reviewed cases, pilot CTA. |
| Feedback form | Role/class, clarity, realism, most-valuable/least-valuable feature, would reuse, wants a pilot call. |
| Educator email | Not "buy the app"; a 15-minute demo + 2-week mini pilot invitation. |
| Telemetry | Mail cohort/ref code -> install/start/completion/replay measurement. |

### 17.1 Beta distribution realities

TestFlight external testing supports distribution to many testers via email or a public
invitation link; no public App Store release is required. Google Play internal testing
allows fast distribution to up to 100 testers for first QA; closed testing for a wider
group. The technical goal of the first mail campaign is an accessible beta link + clean
onboarding, not store approval.

---

## 18. Store and data limits for a medical education app

- Apple reviews apps that could be used for diagnosis/treatment, or could present incorrect medical information, more strictly. Metadata and UX must clearly position Qaniva as a fictional/synthetic educational simulation.
- No real-patient input / clinical decision support feature in the MVP.
- Privacy policy ready before launch; which analytics and AI data is collected, retention, and deletion must be explicit.
- Google Play health/medical apps require a Health apps declaration and a privacy policy; disclaimer/regulatory-proof requirements vary by intended functionality.
- Keep AI chat logs and user performance data minimal. Voice is out of MVP scope, so no extra sensitive-data surface.
- Author/reviewer/version/evidence metadata for medical content is useful not only for academic trust but also for the accuracy narrative in store review.

---

## 19. Twelve architecture decisions to record now (ADR list)

| ADR | Recommended decision |
| --- | --- |
| ADR-001 | MVP client = React Native product shell + full-screen Unity simulation module. |
| ADR-002 | Clinical core pure C#; independent of `MonoBehaviour` and React Native state. |
| ADR-003 | Case content JSON + JSON Schema + semantic version; RN/Unity/backend use the same `caseVersion`. |
| ADR-004 | Presentation profile asset keys; clinical logic not in scene objects or RN components. |
| ADR-005 | 1 reusable ED resus room; no open world; Unity activated only on the simulation screen. |
| ADR-006 | Backend modular monolith; REST API/BFF. RN and Unity carry no secrets. |
| ADR-007 | Postgres; JSONB case/attempt extension fields. |
| ADR-008 | AI backend-only provider adapter; state mutation forbidden. |
| ADR-009 | Patient AI text-only v1; voice later. |
| ADR-010 | Golden replay tests are a release gate. |
| ADR-011 | 2 production cases before feature expansion. |
| ADR-012 | Beta via TestFlight + Play testing; RN + embedded Unity release pipeline tested together. |

> **[Recommendation]** This repo seeds 7 consolidated ADRs (ADR-001..007) covering the same
> ground; ADR-008..012 are captured as decisions inside `docs/development/BACKLOG.md` and the
> AI/testing docs, to be promoted to standalone ADRs as they are implemented.

---

## 20. Parallel work model for a small team

| Track | Daily focus | Contract that prevents blocking |
| --- | --- | --- |
| A – Engine/API | Core, schema, scoring, backend, AI gateway | `case.schema.json` + engine interface |
| B – RN/Unity Product | RN screens/navigation + Unity room/patient/action UI + bridge/lifecycle/performance | Typed start/completion/error messages + `StateSnapshot`/`PresentationCue` contract |
| C – Clinical/Content | Case blueprint, facts, accepted alternatives, rubric, review | Case authoring template + evidence metadata |
| D – Product/Outreach | Figma, landing, beta list, interviews, feedback | Stable happy path + build cadence |

The 3D team can move with placeholder states without waiting on clinical logic; the engine
team can move with headless tests without waiting on final assets.

---

## 21. MVP "Done" definition

- iOS and Android open on real devices; crash-free smoke test.
- At least 2 cases played end to end.
- The same action sequence produces the same deterministic outcome/score headless and in the Unity run.
- Patient state changes with at least 3 visual states (e.g. mild distress -> severe -> recovery/arrest).
- At least one timed deterioration rule is visible to the user and explained in the debrief.
- If Patient AI produces an out-of-state fact, validation/fallback engages.
- The debrief timeline explains critical/delayed/unnecessary/harmful decisions.
- The clinical reviewer signs off the version of each case.
- Analytics: `case_start`, `action_taken`, `critical_action_latency`, `case_complete`, `replay_start`, `feedback_submit` events arrive.
- TestFlight + Android test link; landing + 90s demo; feedback form ready.
- A mail recipient can install the beta and complete one case within 10 minutes without developer help.

---

## 22. Biggest risks and early defenses

| Risk | Early signal | Defense |
| --- | --- | --- |
| 3D scope creep | Room assets take more time than the engine in week 1 | Placeholder assets, strict asset budget, 1 room. |
| Agent code sprawl | Agents write the same thing with different patterns | ADR + skills + lint/tests + small PRs. |
| Clinical truth scattered | Results hard-coded in scene/UI/backend | Core-only rule invariant + grep/review checklist. |
| LLM hallucination | Patient states a new fact | Fact-ID contract + post validation + fallback. |
| Slow case authoring | Every case needs a code change | JSON-driven engine; generic action/effect primitives. |
| Mobile perf | Thermal/FPS drop | Baked lighting, asset budget, weekly device profile. |
| Beta install friction | Mail recipient cannot install the build | TestFlight/Play links, 1-page install guide, no forced signup. |
| Impressive but not useful | Users do not replay | Debrief/timing depth; 5 interviews after the first week. |

---

## 23. First case selection

| Priority | Case | Why |
| --- | --- | --- |
| 1 | STEMI | Exercises most engine primitives (ECG/order/treatment/timing/consult/disposition); strong demo narrative. |
| 2 | Anaphylaxis | One critical first-line action + fast deterioration dramatizes "time and wrong order." |
| 3 (optional) | Hyperkalemia or SVT | ECG/rhythm + medication sequencing; fast new content with the same room and assets. |
| Later | DKA / Sepsis / Stroke | More lab/timing/branching; after the engine settles. |

---

## 24. MVP infrastructure cost — where not to spend

At a 50–200-person beta, the biggest cost is team time and 3D/medical content production,
not cloud. Managed Postgres/Auth/Storage free/low tiers may be enough; AI cost is limited to
Patient AI + Debrief calls. Do **not** buy Kubernetes, microservices, dedicated GPU, a
vector database, a full data warehouse, or enterprise observability at this stage.

- Free/low managed tiers first; upgrade when real usage is measured.
- Remote Addressables only if case media / app size becomes a real problem; the first beta can bundle all core assets.
- When buying 3D assets, weigh license/rig quality, polycount, and mobile suitability above price.
- LLM routing: low-latency/cost model for short Patient AI replies; stronger model for longer debrief. Swappable via the vendor adapter.

---

## 25. One-sentence implementation order

RN shell + Unity-as-a-Library bridge smoke test -> case schema -> pure C# deterministic
engine -> placeholder Unity room -> STEMI end-to-end -> RN Results/Debrief -> reusable 3D
patient/room polish -> Anaphylaxis -> Patient AI + deterministic debrief facts ->
TestFlight/Play beta -> mail outreach -> (after user signal) Educator Lite / more cases.

---

## 26. Starting backlog (first 30 issues)

See `docs/development/BACKLOG.md` for the full, prioritized backlog with acceptance criteria.
The DOCX's 30-item list is the seed:

1. Create RN TypeScript app shell and real-device iOS/Android smoke builds
2. Create Unity 6 URP simulation project and export as Unity as a Library for iOS/Android
3. Implement minimal RN <-> Unity bridge: startSimulation / completed / error
4. Prove RN Home -> full-screen Unity -> RN Results round-trip on both platforms
5. Create clinical-core solution and Unity assembly reference boundary
6. Define case.schema.json v0 + shared caseVersion/attemptId contract
7. Implement CaseDefinition loader + validation
8. Implement SimulationState + immutable snapshot
9. Implement ApplyAction pipeline
10. Implement preconditions + visibility rules
11. Implement time cost + simulated clock
12. Implement transition evaluator with priority
13. Implement event timeline + before/after hashes
14. Implement scoring criterion primitive
15. Create headless replay test helper
16. Create STEMI toy case (5 actions)
17. Create RN Home / Cases / Case Briefing screens
18. Create ED resus room blockout in Unity
19. Create patient rig prefab + animation state controller
20. Create vitals monitor + StateSnapshot binding
21. Create Unity main action drawer UI
22. Create Patient / Exam / Orders / Treat / More components
23. Create backend /cases + /attempts API
24. Add Postgres schema/migrations
25. Create RN Results / Clinical Timeline / Replay screens
26. Create AI gateway interface + stub
27. Implement allowed-facts Patient AI validator
28. Add unified analytics event contract for RN + Unity
29. Add Sentry/error capture, build/version metadata and Unity runtime logging
30. Create TestFlight/Play beta + RN/Unity bridge regression release checklist

---

## 27. Sources

User-provided sources (not in this repo):

- `Qaniva_Full_Code_Detayli_Rapor.pdf` — competitor teardown, screen inventory, state-machine inference, scoring/AI/MVP recommendations (21.08.2026).
- `Qaniva_Kapsamli_Strateji_Raporu_Agustos_2026.pdf` — strategy, market, product, technology, AI safety, 90-day plan (August 2026).
- `Full_Code_Screen_Inventory.csv` — 105 screen/state inventory.

Web verifications (30 August 2026):

- Unity Learn – Ship your first mobile game: <https://learn.unity.com/collection/ship-your-first-mobile-game>
- Unity Manual – Unity as a Library: <https://docs.unity3d.com/Manual/UnityasaLibrary.html>
- Unity Manual – Addressables: <https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.addressables.html>
- React Native – Native Platform: <https://reactnative.dev/docs/native-platform>
- Expo – Add custom native code: <https://docs.expo.dev/workflow/customizing/>
- Expo – Development builds: <https://docs.expo.dev/develop/development-builds/introduction/>
- Apple – App Review Guidelines: <https://developer.apple.com/app-store/review/guidelines/>
- Apple – TestFlight external testers: <https://developer.apple.com/help/app-store-connect/test-a-beta-version/invite-external-testers>
- Google Play – testing tracks: <https://support.google.com/googleplay/android-developer/answer/9845334>
- Google Play – Health Content and Services: <https://support.google.com/googleplay/android-developer/answer/16679511>
- Expo – EAS: <https://docs.expo.dev/eas/>

> This document is a product/technology plan; it is not a clinical guideline, legal opinion,
> or regulatory compliance report. Case content must be verified by relevant specialist
> physicians, and store / data-protection declarations must be re-checked against current
> regulation before launch.
