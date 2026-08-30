# QanivaSimulation (Unity 6 / URP)

The full-screen 3D **simulation renderer**. Unity is a presentation layer only — it
never owns clinical truth (see
[ADR-003](../../docs/adr/ADR-003-deterministic-engine-owns-clinical-truth.md) and
[AGENTS.md](../../AGENTS.md)).

## What is committed vs. generated

**Committed** (source of truth):

- `Assets/Qaniva/Scripts/**` — all C# + `.asmdef` files (the bridge, presentation
  adapters, the `IClinicalRuntime` seam, EditMode tests).
- `Packages/manifest.json` — package set (URP, Test Framework, Newtonsoft JSON).
- `ProjectSettings/ProjectVersion.txt` — target Editor version.

**Generated on first Editor open** (git-ignored): `Library/`, `Temp/`, `obj/`,
`Logs/`, `UserSettings/`, `*.csproj`, `*.sln`, and every `.meta` file for the
committed scripts. Opening the project in Unity 6 regenerates `ProjectSettings/*`
defaults, `Packages/packages-lock.json`, and the `.meta` files. Commit the `.meta`
files Unity creates for `Assets/Qaniva/**` in your first Editor commit — from then
on they are tracked (they are *not* in `.gitignore`; only generated project noise
is).

## Manual steps that require the Unity Editor

These cannot be produced headlessly and are intentionally left for a person:

1. **Create the project settings.** Open `unity/QanivaSimulation` in Unity
   `6000.0.x` with the URP template resolved. Let it import; commit the new
   `ProjectSettings/*` and `Assets/**/*.meta`.
2. **Scenes** (`Assets/Qaniva/Scenes/` — see that folder's README):
   - `Bootstrap.unity` — a single `SimulationBridgeController` on a persistent
     GameObject; entry scene in Build Settings.
   - `ED_Resus.unity` — blockout room: one bed, one monitor, one IV pole, one
     crash cart, a fixed `bedside_01` camera, one baked directional light.
3. **Prefabs** (`Assets/Qaniva/Prefabs/`):
   - `PatientRig.prefab` — humanoid rig + `Animator` with params
     `DistressLevel` (int) and `Unconscious` (bool); add `PatientAnimationBinding`.
   - `VitalMonitor.prefab` — canvas with TMP_Text fields; add `VitalsMonitorBinding`
     and connect the text fields.
4. **PresentationProfiles** (`Assets/Qaniva/PresentationProfiles/`): one
   `ScriptableObject` asset per case mapping `roomKey`/`patientVariant`/
   `cameraPreset`/etc. from the case's `presentationProfile` block.
5. **Enable the real engine** — see below.

## Sync the clinical engine (real `IClinicalRuntime`)

The engine is a separate pure-C# project (`clinical-core/`). Unity consumes it as a
compiled DLL:

```bash
scripts/sync-clinical-core-to-unity.sh
```

This builds `Qaniva.Clinical.Core` (netstandard2.1) and copies the DLL + its
dependencies into `Assets/Qaniva/Plugins/ClinicalCore/` (git-ignored), and copies
the demo case JSON into `Assets/Qaniva/Resources/Qaniva/Cases/` (git-ignored).

Then, in **Project Settings → Player → Scripting Define Symbols**, add:

```
QANIVA_HAS_CLINICAL_CORE
```

The `Qaniva.Clinical.Runtime` assembly has a `defineConstraint` on that symbol, so
until you set it the project compiles and runs against `StubClinicalRuntime`
(deterministic canned progression — enough for the bridge round trip, not medicine).

## Running the bridge round trip without native embed

`SimulationBridgeControllerTests` (EditMode) drives
`FakeUnityBridge → SimulationBridgeController → StubClinicalRuntime` and asserts
`START_SIMULATION → SIMULATION_READY → SIMULATION_COMPLETED`. Run it from
**Window → General → Test Runner → EditMode**. This is the architecture proof while
the native "Unity as a Library" embed is still the open spike (QAN-004).

## Native embed spike (QAN-004)

`NativeUnityBridge` is the single integration seam. iOS reaches
`SimulationBridgeController` via `UnityFramework`'s `sendMessageToGO`; Android via
`UnitySendMessage`. Unity → host goes through an exported `__Internal` function
(iOS) / a small Java plugin (Android). Until that lands, inject a bridge in the
Bootstrap scene or use the fake bridge. Do **not** assume the native path works
until the round trip is green on a real device on both platforms.
