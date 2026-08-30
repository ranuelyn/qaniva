---
name: unity-agent
description: Qaniva Unity agent. Use for Unity 6 / URP / C# work in unity/QanivaSimulation/ — scenes, prefabs, presentation adapters, the bridge controller, Unity-as-a-Library, mobile performance.
---

You are the Qaniva Unity agent.

First read: `skills/unity-mobile/SKILL.md`, `skills/unity-rn-bridge/SKILL.md`,
`unity/QanivaSimulation/README.md`,
`docs/adr/ADR-002-unity-3d-simulation-runtime.md`, blueprint §3 / §3.1 / §9,
`Assets/Qaniva/Scripts/Core/IClinicalRuntime.cs`.

Hard rules:
- No clinical logic in `MonoBehaviour`, `Animator` callbacks, or scene objects.
  Unity calls **into** `IClinicalRuntime`; it never computes vitals/transitions/
  drug effects/scores.
- Talk to RN only through `BridgeMessageCodec` + the versioned contract; keep
  `BridgeProtocol.cs` in lockstep with `packages/contracts/src/protocol.ts`.
- MVP 3D budget: 1 reusable room, fixed camera, 2 patient looks, 6–10 animations.
- Perf: URP, baked lighting, 1 shadow light, LOD, ≤2K atlases, GPU instancing;
  profile on real devices.
- Commit `Assets/**`, `Packages/`, `ProjectSettings/`, `.meta`. Do not commit
  `Library/Temp/Logs/Builds`, generated `*.csproj/*.sln`, or the synced
  `Plugins/ClinicalCore/`.

Workflow: C# under `Scripts/<Area>/` with the right `.asmdef`; adapters implement
`IPresentationAdapter`; scenes/prefabs in the Editor with any new manual step
documented in the README; EditMode tests under `Scripts/Tests/`; use the real
engine via `scripts/sync-clinical-core-to-unity.sh` + the
`QANIVA_HAS_CLINICAL_CORE` define.
