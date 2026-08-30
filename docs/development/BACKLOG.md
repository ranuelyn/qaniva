# Development backlog

Canonical source for near-term work. Derived from the blueprint §26 seed list,
adjusted for what the foundation already delivers. Priority: **P0** (unblocks the
first vertical slice) · **P1** (needed for a demoable beta) · **P2** (after user
signal).

Legend for "Owner": PA Product Architect · MOB Mobile · UNI Unity · ENG Clinical
Engine · BE Backend · CNT Clinical Content · QA QA/Skeptic · REL Release.

---

## Already delivered by the foundation

RN shell + navigation; deterministic engine skeleton + tests + golden replay;
case JSON Schema + validator + one fictional demo case; typed RN↔Unity bridge
contract + C# mirror + parity test; thin NestJS API (cases/attempts/analytics/AI
gateway) with tests; AI safety gateway + deterministic fallback; Unity C#
foundation (bridge codec, controller, presentation adapters, stub runtime, EditMode
tests); ADRs 001–007; docs; CI for the TS workspace + engine.

---

## P0 — unblock the first vertical slice

| ID | Title | Owner | Purpose | Acceptance criteria | Depends on |
| --- | --- | --- | --- | --- | --- |
| QAN-001 | Generate Unity `ProjectSettings` + `.meta` and commit | UNI | Make the Unity project openable from a clean clone | Project opens in `6000.0.x` with no console errors; `ProjectSettings/*` and `Assets/**/*.meta` committed | — |
| QAN-002 | Bootstrap + ED_Resus scenes, patient/monitor prefabs | UNI | The blockout room the sim renders into | Scenes in Build Settings; `PatientRig`/`VitalMonitor` prefabs wired to the presentation adapters; runs in the Editor | QAN-001 |
| QAN-003 | Wire `ClinicalRuntime` end to end in Unity | UNI+ENG | Real engine drives the scene | With `QANIVA_HAS_CLINICAL_CORE` set, EditMode test drives the demo case START→COMPLETED through `ClinicalRuntime` (not the stub) | QAN-001, sync script |
| QAN-004 | Native "Unity as a Library" embed (iOS + Android) | MOB+UNI | The real bridge transport | `START→READY→COMPLETED` verified on a real iOS device and a real Android device; lifecycle/orientation/audio/memory/back checked; `NativeUnityBridge` implemented both platforms | QAN-002 |
| QAN-005 | Swap `useUnitySimulation` to the native transport behind a flag | MOB | Use the real bridge in the app, keep the fake for tests | App uses native transport on device; `FakeUnityBridge` still used in `vitest`; no calling-code change | QAN-004 |
| QAN-006 | In-simulation action UI in Unity (5 main actions) | UNI | Player can actually act | Action drawer calls `SubmitPlayerAction`; rejects show feedback; available actions come from `GetAvailableActionIds` | QAN-003 |
| QAN-007 | Attempt event-log upload from the client | MOB+BE | Persist the full timeline (bridge carries only the summary) | Client posts events to `POST /attempts/:id/events` during/after a run; backend stores count now, full rows after QAN-010 | — |

## P1 — demoable beta

| ID | Title | Owner | Purpose | Acceptance criteria | Depends on |
| --- | --- | --- | --- | --- | --- |
| QAN-010 | Postgres persistence + migrations | BE | Real storage for cases/attempts/events/analytics | `apps/api/db/schema.sql` promoted to migrations; repositories swap from in-memory; a `docker-compose` Postgres for local; CI spins an ephemeral DB job | — |
| QAN-011 | Case publishing workflow (draft → review → published) | BE+CNT | Only reviewed cases are served | `GET /cases` returns only `published` + `clinicalReview.status == approved`; unpublished reachable only with an admin flag | QAN-010 |
| QAN-012 | STEMI case v1 (schema JSON + golden scripts) | CNT+ENG | First real case | Schema + semantic valid; 6 golden paths committed with golden files; clinician sign-off recorded | — |
| QAN-013 | Anaphylaxis case v1 | CNT+ENG | Second real case, dramatic timing | Same criteria as QAN-012 | QAN-012 |
| QAN-014 | RN Results / Clinical timeline / Debrief screens (real data) | MOB | The payoff screen | Timeline renders from `AttemptSummary`; critical/missed/harmful highlighted; replay button re-launches with a new attempt | QAN-005 |
| QAN-015 | Patient AI panel in Unity + gateway call | UNI+BE | Text anamnesis | Panel posts to `POST /ai/patient` with allowed/disclosed facts; out-of-scope + fact-leak paths show the fallback; logged | — |
| QAN-016 | Debrief narration via the gateway | BE+MOB | Personalized debrief text | `POST /ai/debrief` narrates the deterministic timeline; score-tamper path falls back; shown on the Debrief screen | QAN-014 |
| QAN-017 | Unified analytics wired in RN + Unity | MOB+UNI | Funnel + action-latency measurement | `case_start`, `action_taken`, `critical_action_latency`, `case_complete`, `replay_start`, `feedback_submit` emitted with `attemptId`; backend ingests | — |
| QAN-018 | Sentry (RN) + structured logs (Unity + API) | REL | Crash/error visibility | RN crash + JS error captured; Unity runtime errors logged with build/version metadata; API error middleware | — |
| QAN-019 | Auth-ready module (guest + optional account) | BE+MOB | Login later without a rewrite | Guest sessions issue an anonymous id; `JWT_*` verified path stubbed; RN stores/sends a session token; no PII collected in MVP | — |
| QAN-020 | Reusable 3D patient look #2 + 6–10 state animations | UNI | Visible patient-state change (≥3 states) | `distress_mild/severe/unconscious/arrest/recovery` states driven by `PatientAnimationBinding` from engine cues; profiled on device | QAN-002 |
| QAN-021 | Mobile device performance pass | UNI+QA | Stable 30 FPS mid-tier | Frame budget report from a real iOS + Android device; baked lighting; asset budget respected | QAN-020 |
| QAN-022 | Case media pipeline (ECG/image/audio) | UNI+BE | Orders return artifacts | A `resultTemplateId` can reference bundled media; served via object storage; shown in Unity | QAN-011 |
| QAN-023 | Counterfactual golden fixtures per case | QA+ENG | Regression coverage for wrong/delayed/harmful paths | Each shipped case has all 6 golden-path scripts + golden files in CI | QAN-012 |
| QAN-024 | Unity CI (License-gated build + EditMode tests) | REL | Automate what the foundation can't | GameCI (or equivalent) job builds the project and runs EditMode tests on a self-hosted/licensed runner; documented, not a broken stub | QAN-001 |
| QAN-025 | EAS build + TestFlight/Play internal pipeline | REL | Get a link into testers' hands | `eas build` for iOS + Android with the Unity library; TestFlight external + Play internal tracks configured | QAN-004 |

## P2 — after user signal

| ID | Title | Owner | Purpose | Acceptance criteria | Depends on |
| --- | --- | --- | --- | --- | --- |
| QAN-030 | Choose a license + release checklist | PA+REL | Legal clarity + repeatable release | `LICENSE` added; privacy policy drafted (analytics + AI data, retention, deletion); store data-safety declarations filled and verified | — |
| QAN-031 | Third mini-case (Hyperkalemia or SVT) | CNT+ENG | More content, same room | Schema/golden/review as QAN-012 | QAN-013 |
| QAN-032 | Real AI provider adapter (low-latency patient, stronger debrief) | BE | Replace the stub | Implements `AiProvider`; keys backend-only via `.env`; fallback still works; latency budget met; evals green | QAN-015, QAN-016 |
| QAN-033 | Prompt + safety eval set for the AI gateway | QA+BE | Guard against regressions | Golden eval set: allowed-facts, role break, invalid schema, prompt injection; runs on prompt/model change | QAN-032 |
| QAN-034 | Educator Lite (share a case, see cohort results) | BE+MOB | The pilot ask | Read-only cohort view keyed by ref code; no PII | QAN-017 |
| QAN-035 | Full protocol codegen from `packages/contracts` | PA | Remove the hand-mirrored `BridgeProtocol.cs` | `BridgeProtocol.cs` + payload DTOs generated from `protocol.json`; parity test becomes a build step | — |
| QAN-036 | Landing page + 90s demo capture + mail kit | REL | Outreach package | 30s explainer, 3 GIFs, feedback form, 1-page PDF; telemetry ties ref code → install/start/complete | QAN-025 |

---

## Explicitly NOT v1 (park here, do not build)

Photorealistic hospital · paid 3D asset sprawl · many patient avatars ·
multiple environments · production medical case library · full educator dashboard ·
subscription/payments · full AI patient (voice, memory) · multiplayer · LMS
integration · MENA localization · microservices / Kubernetes / dedicated GPU /
vector DB / data warehouse · premature event streaming · open-world navigation ·
full physiology engine. (Blueprint §15 scope-cut list, §18, §24.)
