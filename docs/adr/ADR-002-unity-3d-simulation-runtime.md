# ADR-002 — Unity 6 as the 3D simulation runtime

## Status

Accepted (2026-08-30).

## Context

The MVP is 3D (patient variation, scene variation, reusable hospital props) but
**not** an open world: one reusable clinical room + a modular patient/prop system.
We need a mature mobile 3D toolchain: rendering, animation, a device profiler,
asset bundling, and the ability to embed into a native host app.

## Decision

Unity 6 (`6000.x`) + URP is the **full-screen 3D simulation renderer**, embedded
via **Unity as a Library**. It owns the scene, patient rendering, animation,
camera, environment props, the vitals monitor visualization, and visual
patient-state changes. It is activated only during a simulation.

Unity consumes the deterministic engine as a compiled DLL through the
`IClinicalRuntime` interface. Until that DLL is synced and the
`QANIVA_HAS_CLINICAL_CORE` scripting define is set, a deterministic
`StubClinicalRuntime` keeps the project compiling and the bridge round trip
runnable.

## Alternatives considered

- **2D / 2.5D (sprites).** Smaller and cheaper, but does not deliver the reusable
  patient/scene system that is the reason to go 3D now.
- **A web/native 3D engine (three.js, RealityKit, SceneKit).** Weaker cross-
  platform mobile tooling and asset pipeline; more custom work.
- **Godot.** Viable, but less mature mobile profiling / Addressables story and no
  team experience.

## Consequences

- Mobile performance is a standing constraint: baked lighting, strict asset
  budget, one shadow-casting light, LOD, texture atlases; profile on real devices.
- Scenes and prefabs must be authored in the Editor; the repo documents the manual
  steps (`unity/QanivaSimulation/README.md`) and commits only the C# + asmdefs +
  package manifest.
- Unity → RN communication is only through the versioned bridge (ADR-006).
