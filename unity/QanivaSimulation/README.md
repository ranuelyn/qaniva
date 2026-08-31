# QanivaSimulation (Unity 6 / URP)

The full-screen 3D **simulation renderer**. Unity is a presentation layer only — it
never owns clinical truth (see
[ADR-003](../../docs/adr/ADR-003-deterministic-engine-owns-clinical-truth.md) and
[AGENTS.md](../../AGENTS.md)). The iOS embed design is
[ADR-008](../../docs/adr/ADR-008-unity-as-a-library-ios-integration.md).

## Editor version

Unity **6000.5.x** (see `ProjectSettings/ProjectVersion.txt` for the exact pinned
version). The original foundation pinned a placeholder `6000.0.0f1` that was never
opened; the project was deliberately adopted onto the installed 6000.5 editor at
integration time.

## What is committed vs. generated

**Committed:** `Assets/Qaniva/**` (C#, `.asmdef`, `.meta`, the generated-then-
committed `Bootstrap.unity` scene), `Packages/manifest.json` (+`packages-lock.json`
once generated), `ProjectSettings/*`.

**Git-ignored:** `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, `build/`
(iOS export), generated `*.csproj`/`*.sln`, and the two synced-artifact folders:
`Assets/Qaniva/Plugins/ClinicalCore/` (engine DLL) and
`Assets/Qaniva/Resources/Qaniva/Cases/` (case JSON) — both produced by
`scripts/sync-clinical-core-to-unity.sh`.

## Runtime wiring (no scene dependencies)

- `BridgeBootstrap` (`RuntimeInitializeOnLoadMethod`, BeforeSceneLoad) creates the
  **`SimulationBridge`** GameObject with `NativeUnityBridge` +
  `SimulationBridgeController`, choosing the runtime implementation:
  - `QANIVA_HAS_CLINICAL_CORE` defined → real `ClinicalRuntime` over the synced
    `Qaniva.Clinical.Core.dll`;
  - otherwise → `StubClinicalRuntime` with a loud warning (dev stand-in only).
- `SimulationUiController` self-attaches (AfterSceneLoad) and builds the
  interactive UI Toolkit surface (vitals, category tabs, action list, case log,
  exit) — see the "Interactive simulation UI" section below.
- The GameObject name `SimulationBridge` is part of the native contract
  (`sendMessageToGO` target); change it only together with
  `apps/mobile/modules/unity-host`.
- `Plugins/iOS/QanivaBridgeNative.mm` defines the `DllImport("__Internal")`
  symbol `_QanivaBridge_SendToHost` inside UnityFramework and lets the host
  register its callback (`QanivaRegisterHostHandler`, found via `dlsym`).

## Commands (reproducible, no Editor clicking)

```bash
# engine DLL + demo case into the project (also run by the export script)
scripts/sync-clinical-core-to-unity.sh

UNITY="/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity"

# one-time / after wiping: minimal scene + Build Settings entry
"$UNITY" -batchmode -nographics -quit -projectPath unity/QanivaSimulation \
  -executeMethod Qaniva.EditorTools.QanivaBuild.CreateMinimalScene -logFile -

# enable the real engine (adds QANIVA_HAS_CLINICAL_CORE to Player settings,
# persisted in ProjectSettings/ProjectSettings.asset — committed, not local-only)
"$UNITY" -batchmode -nographics -quit -projectPath unity/QanivaSimulation \
  -executeMethod Qaniva.EditorTools.QanivaBuild.EnableClinicalCoreDefine -logFile -

# EditMode tests (bridge codec, controller round trip, real-engine integration)
"$UNITY" -batchmode -projectPath unity/QanivaSimulation \
  -runTests -testPlatform EditMode \
  -testResults "$PWD/unity-editmode-results.xml" -logFile -

# full iOS export + UnityFramework.framework build + install into the RN host
scripts/export-unity-ios.sh          # device SDK
SIM=1 scripts/export-unity-ios.sh    # simulator SDK
```

**Stale-DLL / stale-framework rule:** after changing `clinical-core/`, re-run the
sync script; after changing anything under `Assets/`, re-run the export before
trusting a device/simulator run. The sync script always rebuilds from source
(`dotnet publish`) — it never copies a cached DLL.

## Tests

| File | Needs | Proves |
| --- | --- | --- |
| `BridgeCodecTests` (EditMode) | — | envelope codec, protocol-version + channel rejection |
| `SimulationBridgeControllerTests` (EditMode) | — | START→READY→COMPLETED, duplicate-START idempotency, completion exactly-once, availability/timeline pass-through, RequestExit, e2e-driver mode gates |
| `RealClinicalRuntimeTests` (EditMode) | synced DLL + `QANIVA_HAS_CLINICAL_CORE` | the round trip over the **real engine**, locked to the committed golden (score 80, `replayHash fe2191ff…`), determinism across runs, rejected-action state invariance |
| `InteractiveUiPlayModeTests` (PlayMode) | same | the INTERACTIVE path: real UI buttons pressed through event dispatch reproduce the golden replay; hidden/disabled rendering from the engine projection; double-tap guard; interactive mode never auto-plays |

## Interactive simulation UI

The in-simulation UI (UI Toolkit — committed `uxml`/`uss`/`tss` under
`Assets/Qaniva/Resources/Qaniva/UI/` + a generated `QanivaPanelSettings.asset`)
renders the engine's canonical action availability, vitals and timeline, and
routes taps into `SubmitPlayerAction`. Runtime modes (`interactive` default,
`e2e_autoplay`, `e2e_ui`) and the whole flow are documented in
[docs/architecture/simulation-ui.md](../../docs/architecture/simulation-ui.md).

## 3D presentation (QAN-002)

The reusable ED/resus presentation is generated from primitives by
`QanivaPresentationAssets.CreateAll` (materials + `ed_resus_v1` environment,
`adult_neutral_v1` primitive patient, `adult_rigged_v1` rigged patient — built
from `Assets/Qaniva/Art/Patients/adult_rigged_v1.fbx`, itself generated by
`scripts/generate-patient-blender.py` in Blender — and `BedsideMonitor` prefabs,
committed under `Assets/Qaniva/Resources/Qaniva/`). Case result assets (e.g.
the STEMI ECG placeholder) live under `Resources/Qaniva/CaseAssets/` and are
opened by the in-sim result viewer. At runtime `EnvironmentBootstrap` composes
the scene from the case's `presentationProfile` via `PresentationRegistry`;
`PatientPresentationMapper` derives the presentation-only visual state and the
monitor renders canonical snapshot vitals. Full architecture, prefab contracts,
and how to add a room/patient: `docs/architecture/3d-presentation.md`. Asset
policy/licensing: `docs/art/asset-manifest.md`.

Composition previews without a device: run PlayMode tests with
`QANIVA_CAPTURE_DIR=<dir>` (real-pipeline portrait PNGs).

Still later: production room/patient art (recommended purchase in the asset
manifest), clip-based animation set, second environment/patient content. Keep
to the MVP 3D budget (blueprint §3).
