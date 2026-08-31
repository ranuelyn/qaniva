# Development backlog

Canonical source for near-term work. Derived from the blueprint §26 seed list,
adjusted for what the foundation already delivers. Priority: **P0** (unblocks the
first vertical slice) · **P1** (needed for a demoable beta) · **P2** (after user
signal).

Legend for "Owner": PA Product Architect · MOB Mobile · UNI Unity · ENG Clinical
Engine · BE Backend · CNT Clinical Content · QA QA/Skeptic · REL Release.

---

## Integration risks discovered during the proof sprint (2026-08-31)

- **START delivery race** — Unity's runtime boots after `runEmbedded` returns;
  early `sendMessageToGO` is dropped silently. Mitigated: host retries START until
  any Unity message arrives; Unity treats duplicate START for the in-flight
  attempt as a retry (re-announces READY). Keep this invariant when adding
  messages.
- **MSAA >1 floods RenderPass errors on the iOS simulator** (URP/Metal:
  "Attachment 0 was created with 1 samples but 4 samples were requested" every
  frame). MSAA is off in the URP asset until verified on a physical device.
- **Unity simulator exports are x64-only** — the export script swaps in Unity's
  shipped universal `UnityRuntime`/`baselib` sim variants and overrides `ARCHS`;
  revisit if Unity exposes a public simulator-arch setting.
- **dlsym symbols must be pinned** — anything the host resolves from
  UnityFramework needs `__attribute__((used, visibility("default")))` or the
  linker dead-strips it.
- **Stale framework risk** — Unity-side changes require re-running
  `scripts/export-unity-ios.sh`; the app otherwise runs an old simulation binary
  silently. (Documented in local-development.md; consider a build-stamp check.)
- ~~`IntegrationAutoPlayer` must die with QAN-006~~ — resolved differently: kept as
  the `e2e_autoplay`-mode regression driver, double-gated (scripting define +
  typed runtime mode) and PlayMode-tested inert in `interactive` mode.

## Already delivered by the foundation

RN shell + navigation; deterministic engine skeleton + tests + golden replay;
case JSON Schema + validator + one fictional demo case; typed RN↔Unity bridge
contract + C# mirror + parity test; thin NestJS API (cases/attempts/analytics/AI
gateway) with tests; AI safety gateway + deterministic fallback; Unity C#
foundation (bridge codec, controller, presentation adapters, stub runtime, EditMode
tests); ADRs 001–007; docs; CI for the TS workspace + engine.

---

## P0 — unblock the first vertical slice

| ID | Title | Owner | Status | Evidence / acceptance criteria | Depends on |
| --- | --- | --- | --- | --- | --- |
| QAN-001 | Real Unity project initialization | UNI | **DONE** (2026-08-31) | Opened + compiled in Unity 6000.5.10f1 batchmode, exit 0, zero compile errors (three real errors found & fixed). `ProjectSettings/*`, `.meta`, migrated `Packages/manifest.json` + lock committed. URP pipeline asset created and assigned (Graphics + Quality). | — |
| QAN-002 | Reusable 3D ED/resus presentation | UNI | **DONE** (2026-08-31) | Code-generated (license-clean, headless-reproducible) `ed_resus_v1` environment prefab (room shell, resus bed, IV pole, cart, wall props, composed portrait camera, mobile URP lighting), `adult_neutral_v1` patient prefab (procedural breathing from canonical RR, presentation-only visual states Normal/Distressed/Unconscious/Unresponsive with deterministic mapper, procedure anchors) and snapshot-driven `BedsideMonitor` prefab — all selected per-case via `presentationProfile` → `PresentationRegistry` (new case = prefab + one registry entry, no new scene). Verified: 9/9 PlayMode presentation+UI tests (composition, canonical monitor values 38→68, Distressed→Normal mapping, warm-relaunch reuse+reset, honest unknown-room failure), real-pipeline captures, and live on the iPhone 16 Pro simulator with the interactive UI on top. Docs: `docs/architecture/3d-presentation.md`, `docs/art/asset-manifest.md`. | QAN-001 |
| QAN-003 | Real `ClinicalRuntime` inside Unity | UNI+ENG | **DONE** (2026-08-31) | `QANIVA_HAS_CLINICAL_CORE` persisted in ProjectSettings; EditMode `RealClinicalRuntimeTests` (4 tests) run the demo ideal path through the real `Qaniva.Clinical.Core.dll` to the committed golden (complete / 80 / `fe2191ff…`), assert cross-run determinism and rejected-action state invariance. 13/13 EditMode green. | — |
| QAN-004 | Native "Unity as a Library" embed | MOB+UNI | **PARTIAL — iOS simulator DONE; device + Android OPEN** | Full round trip verified live on iPhone 16 Pro **simulator**: RN → `QanivaUnityBridge` (runtime-loaded UnityFramework, dlsym handler) → real engine → `SIMULATION_READY`/`SIMULATION_COMPLETED` → RN Results rendering the golden payload. Lifecycle RN→Unity→RN→Unity→RN verified (initialise-once runtime, both runs deterministic-identical). NOT yet: physical-device run (device offline/signing), orientation/audio/memory soak, Android transport. | QAN-001 |
| QAN-005 | Native transport selection in RN | MOB | **DONE** (2026-08-31) | `selectUnityTransport()` uses the native module when present (verified on simulator); `FakeUnityBridge` only without the module, with console warning + on-screen badge; screens unchanged between transports; vitest keeps using the fake. | QAN-004 |
| QAN-006 | Interactive in-simulation action UI | UNI | **DONE** (2026-08-31) | UI Toolkit surface (tabs Patient/Examine/Orders/Treat/More, action list, vitals, case log, result banner, exit) rendering the engine's canonical `GetActionAvailability()` projection (hidden / visible+disabled with engine reason / enabled — new clinical-core API, offerable==Visible&&Enabled by construction). Verified: 18 EditMode + 4 PlayMode tests incl. `ManualUiPlayReproducesTheGoldenReplay` (real UI buttons via event dispatch reproduce golden `fe2191ff…`, COMPLETED exactly once) and on-simulator: interactive launch idles (byte-identical screenshots 15s apart), `e2e_ui` run walks the REAL UI — abort run (2 actions + real Exit → EXIT_REQUESTED → RN back) then completion run → RN Results with the golden payload. `IntegrationAutoPlayer` kept but double-gated (define + `mode==e2e_autoplay`); interactive mode PlayMode-tested to never auto-play. See docs/architecture/simulation-ui.md. | QAN-003 |
| QAN-007 | Attempt event-log upload from the client | MOB+BE | OPEN | Client posts events to `POST /attempts/:id/events` during/after a run. | — |
| QAN-006b | Parameterized action input (dose/route pickers) | UNI | OPEN | The demo case proves the loop with parameterless submits; `give_atropine`'s optional `dose_mg` param needs a small generic picker (enum/number) before parameterized cases ship. | QAN-006 |
| QAN-008 | iOS **device** run + lifecycle hardening | MOB+UNI | **DEFERRED — pre-beta device validation** (2026-08-31) | Intentionally postponed until the MVP matures (content + screens first); no watcher processes remain. Everything is staged for when it resumes: a valid "Apple Development: yusufasimarslan@gmail.com" signing identity exists; steps = connect the iPhone via cable, unlock, "Trust This Computer", then `scripts/export-unity-ios.sh` (device SDK), `xcodebuild ... -destination 'platform=iOS' -allowProvisioningUpdates`, install + run, verify the interactive 3D slice + second launch. Schedule before any TestFlight build (QAN-025). | QAN-004 |
| QAN-009 | Android Unity-as-a-Library transport | MOB+UNI | OPEN | Mirror of ADR-008 on Android: `UnityPlayer` host + `UnitySendMessage` in / Java plugin out; round trip on emulator + device. | QAN-004 |

## P1 — demoable beta

| ID | Title | Owner | Purpose | Acceptance criteria | Depends on |
| --- | --- | --- | --- | --- | --- |
| QAN-010 | Postgres persistence + migrations | BE | Real storage for cases/attempts/events/analytics | `apps/api/db/schema.sql` promoted to migrations; repositories swap from in-memory; a `docker-compose` Postgres for local; CI spins an ephemeral DB job | — |
| QAN-011 | Case publishing workflow (draft → review → published) | BE+CNT | Only reviewed cases are served | `GET /cases` returns only `published` + `clinicalReview.status == approved`; unpublished reachable only with an admin flag | QAN-010 |
| QAN-012A | STEMI evidence research | CNT | Source-driven foundation for the first real case | **DONE** (2026-08-31): `docs/clinical/cases/stemi/research.md` + `evidence.yaml` (ACC/AHA 2025 + ESC 2023 verified current; divergences D1–D6 explicit; Turkish localization + INACSL + competitor lessons recorded) | — |
| QAN-012B | STEMI clinical blueprint + review package | CNT | Review-ready deterministic case design | **DONE** (2026-08-31): `BLUEPRINT.md` (scope, LOs, actions, timing/causality criteria, deterioration graph, scoring, debrief, prebrief), `ECG_ASSET_SPEC.md`, `IMPLEMENTATION_SPEC.md` (gap analysis), `REVIEW.md`. Status: **DRAFT — CLINICAL REVIEW REQUIRED** | QAN-012A |
| QAN-012C | STEMI clinician review | CNT (human clinician) | The mandatory approval gate | A qualified clinician works `docs/clinical/cases/stemi/REVIEW.md` (section verdicts + Q1–Q14/OQ-1–7) and signs; iterate CHANGES_REQUESTED as needed. **Blocks 012D. AI cannot perform this.** | QAN-012B |
| QAN-012D | STEMI approved-case implementation | CNT+ENG | Turn the approved blueprint into engine truth | Versioned `case.json` from the approved blueprint; ECG asset acquired per spec (license recorded, clinician-verified); prerequisite gaps closed first: QAN-022-minimal (ECG image via resultTemplateId) + terminal-outcome vocabulary (IMPLEMENTATION_SPEC GAP-2); schema + CLI valid; `metadata.clinicalReview` recorded | QAN-012C |
| QAN-012E | STEMI technical QA + golden replay | QA+ENG | Regression net for the first real case | 6 golden-path scripts + golden files green in CI; determinism verified; blind playtest note before publish | QAN-012D |
| QAN-013 | Anaphylaxis case v1 | CNT+ENG | Second real case, dramatic timing | Starts by invoking `skills/case-authoring/SKILL.md` stage 1 (research), same lifecycle as QAN-012A–E | QAN-012E |
| QAN-014 | RN Results / Clinical timeline / Debrief screens (real data) | MOB | The payoff screen | Timeline renders from `AttemptSummary`; critical/missed/harmful highlighted; replay button re-launches with a new attempt | QAN-005 |
| QAN-015 | Patient AI panel in Unity + gateway call | UNI+BE | Text anamnesis | Panel posts to `POST /ai/patient` with allowed/disclosed facts; out-of-scope + fact-leak paths show the fallback; logged | — |
| QAN-016 | Debrief narration via the gateway | BE+MOB | Personalized debrief text | `POST /ai/debrief` narrates the deterministic timeline; score-tamper path falls back; shown on the Debrief screen | QAN-014 |
| QAN-017 | Unified analytics wired in RN + Unity | MOB+UNI | Funnel + action-latency measurement | `case_start`, `action_taken`, `critical_action_latency`, `case_complete`, `replay_start`, `feedback_submit` emitted with `attemptId`; backend ingests | — |
| QAN-018 | Sentry (RN) + structured logs (Unity + API) | REL | Crash/error visibility | RN crash + JS error captured; Unity runtime errors logged with build/version metadata; API error middleware | — |
| QAN-019 | Auth-ready module (guest + optional account) | BE+MOB | Login later without a rewrite | Guest sessions issue an anonymous id; `JWT_*` verified path stubbed; RN stores/sends a session token; no PII collected in MVP | — |
| QAN-020 | Reusable 3D patient look #2 + 6–10 state animations | UNI | Visible patient-state change (≥3 states) | `distress_mild/severe/unconscious/arrest/recovery` states driven by `PatientAnimationBinding` from engine cues; profiled on device | QAN-002 |
| QAN-021 | Mobile device performance pass | UNI+QA | Stable 30 FPS mid-tier | Frame budget report from a real iOS + Android device; baked lighting; asset budget respected | QAN-020 |
| QAN-022 | Case media pipeline (ECG/image/audio) | UNI+BE | Orders return artifacts | A `resultTemplateId` can reference bundled media; served via object storage; shown in Unity | QAN-011 |
| QAN-023 | Counterfactual golden fixtures per case | QA+ENG | Regression coverage for wrong/delayed/harmful paths | Each shipped case has all 6 golden-path scripts + golden files in CI | QAN-012D |
| QAN-024 | Unity CI (license-gated EditMode + export) | REL | Automate what local runs proved | Learned 2026-08-31: `Unity -batchmode -runTests -testPlatform EditMode` works headlessly on macOS arm64 and licensing auto-provisions from a signed-in Hub account; a runner needs the ~5GB editor + 2.4GB iOS support + a licensed Unity account (secret) + macOS for the iOS export. Add a GameCI/self-hosted mac job running EditMode + (nightly) the SIM=1 export; never a stub job. | QAN-001 |
| QAN-025 | EAS build + TestFlight/Play internal pipeline | REL | Get a link into testers' hands | `eas build` for iOS + Android with the Unity library; TestFlight external + Play internal tracks configured | QAN-004 |

## P2 — after user signal

| ID | Title | Owner | Purpose | Acceptance criteria | Depends on |
| --- | --- | --- | --- | --- | --- |
| QAN-030 | Choose a license + release checklist | PA+REL | Legal clarity + repeatable release | `LICENSE` added; privacy policy drafted (analytics + AI data, retention, deletion); store data-safety declarations filled and verified | — |
| QAN-031 | Third mini-case (Hyperkalemia or SVT) | CNT+ENG | More content, same room | Schema/golden/review lifecycle as QAN-012A–E | QAN-013 |
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
