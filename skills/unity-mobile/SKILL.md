# Skill: unity-mobile

## Purpose

Build the Unity simulation runtime as a presentation layer that renders engine
snapshots — fast on mobile, with zero clinical logic.

## When to use

Any change under `unity/QanivaSimulation/`.

## Inputs (read first)

- `unity/QanivaSimulation/README.md` (manual Editor steps, engine sync)
- `docs/adr/ADR-002-unity-3d-simulation-runtime.md`
- `docs/QANIVA_MVP_BLUEPRINT.md` §3, §3.1, §9 (3D budget + perf rules)
- `Assets/Qaniva/Scripts/Core/IClinicalRuntime.cs`

## Non-negotiable rules

1. No clinical logic in `MonoBehaviour`, `Animator` callbacks, or scene objects.
   The engine decides *when*; adapters decide only *how it looks*.
2. Unity code calls **into** `IClinicalRuntime`; it never computes vitals,
   transitions, drug effects, or scores.
3. Talk to RN only through `BridgeMessageCodec` + the versioned contract.
4. MVP 3D budget: 1 room, 1 bed, 1 monitor, 1 trolley, 1 O2/IV set, 2 patient
   looks, 6–10 animations. One reusable room; fixed camera; no free roam.
5. Perf: URP, baked lighting, 1 shadow light, LOD, texture atlases (≤2K except
   face/hands), GPU instancing. Profile on real devices; Editor FPS ≠ acceptance.
6. `Assets/**`, `Packages/`, `ProjectSettings/`, and `.meta` files are committed;
   `Library/Temp/Logs/Builds/UserSettings`, generated `*.csproj/*.sln`, and the
   synced `Plugins/ClinicalCore/` + `Resources/Qaniva/Cases/` are not.

## Workflow

1. C# under `Assets/Qaniva/Scripts/<Area>/` with the right `.asmdef` reference.
2. Presentation adapters implement `IPresentationAdapter`; bind to
   `SimulationBridgeController.SnapshotUpdated` / cues.
3. Scenes/prefabs in the Editor — document any new manual step in the README.
4. To use the real engine: `scripts/sync-clinical-core-to-unity.sh` + set the
   `QANIVA_HAS_CLINICAL_CORE` define.
5. Run EditMode tests (Test Runner). Add tests under `Scripts/Tests/`.

## Validation

- Project compiles in `6000.0.x` with no console errors.
- EditMode tests green (`BridgeCodecTests`, `SimulationBridgeControllerTests`, plus
  yours).
- Device frame-budget report attached for perf-affecting changes.

## Done criteria

Compiles clean; EditMode tests green; no clinical logic in scene/`MonoBehaviour`
code; asset budget respected; README updated for any new manual step.

## Common failure modes

- Reading a vital and computing a follow-on value in a `MonoBehaviour`.
- Committing `Library/` or generated `.csproj` files, or *ignoring* `.meta`/source.
- A realtime GI / extra shadow-casting light that tanks mobile FPS.
- Wiring the scene to `StubClinicalRuntime` and forgetting it's a stand-in.
- Hard-coding a case's asset paths instead of reading `presentationProfile`.
