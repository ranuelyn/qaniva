# ADR-001 — React Native as the product shell

## Status

Accepted (2026-08-30).

## Context

The team's existing strength is React / TypeScript. The product needs fast
iteration on non-simulation surfaces — onboarding, home, case library, briefing,
progress, profile, results, clinical timeline, debrief — plus networking,
analytics, and (later) auth. The 3D simulation is a distinct, occasional,
full-screen activity, not a persistent part of every screen.

## Decision

React Native + TypeScript is the **product shell** and app entry point. It owns
navigation, product state, product UI, the network boundary, the analytics
abstraction, and the screen that hosts the full-screen Unity simulation. Expo is
used in a **development-build / prebuild** configuration (custom native code is
required for the Unity embed) — **not** Expo Go.

## Alternatives considered

- **Native (Swift/Kotlin) shell.** Rejected: throws away team velocity for a
  product that is mostly forms, lists, and a timeline.
- **Unity for the whole app.** Rejected: Unity UI iteration for product screens is
  slow, and app-store/product tooling (deep links, notifications, payments later)
  is weaker.
- **Flutter.** Rejected: no team experience; still needs the same Unity embed work.
- **Expo Go as the target runtime.** Rejected: cannot embed Unity as a library.

## Consequences

- Two runtimes ship in one binary. Their responsibilities must stay strictly
  separated (see ADR-003, ADR-006).
- The build pipeline must combine an RN native build with the exported Unity
  library artifact.
- CI cannot fully build the mobile app without native toolchains; RN-free logic
  (bridge handling, analytics) is unit-tested, and screens are typchececked.
