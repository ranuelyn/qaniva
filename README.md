# Qaniva

Mobile-first, 3D **clinical decision simulation** platform. Pick a fictional case,
work the patient in a full-screen 3D scene, then review a timeline of every
decision you made and why it mattered.

> **Status: foundation.** This repository is the clean architectural base for
> Qaniva — a deterministic engine skeleton, a versioned case format, a typed
> RN↔Unity bridge, a thin backend, an AI safety boundary, and the docs/skills/CI
> to build on it safely. It is **not** a finished product. See
> [`docs/QANIVA_MVP_BLUEPRINT.md`](docs/QANIVA_MVP_BLUEPRINT.md) for the full plan.

## The one architectural idea

```
Clinical Engine (pure C#)  = truth
Unity 6 + URP              = presentation / simulation renderer
React Native + TypeScript  = product / mobile shell
AI (backend gateway)       = constrained language / tutoring layer
```

An LLM never produces a vital, a state transition, a drug result, a diagnosis, or a
score. Those come only from the deterministic engine. See
[`AGENTS.md`](AGENTS.md) for the full working contract.

## Layout

| Path | What |
| --- | --- |
| `clinical-core/` | Pure C# deterministic engine (`netstandard2.1`) + xUnit tests + headless replay CLI |
| `unity/QanivaSimulation/` | Unity 6 / URP simulation runtime (bridge, presentation adapters, EditMode tests) |
| `apps/mobile/` | React Native + TypeScript product shell (Expo dev-build capable) |
| `apps/api/` | NestJS thin modular monolith (cases, attempts, analytics, AI gateway) |
| `packages/contracts/` | RN↔Unity bridge protocol + attempt-summary contract (single source of truth) |
| `packages/case-schema/` | Case JSON Schema + validator + the fictional demo case fixture |
| `packages/analytics-schema/` | Unified RN + Unity analytics event contract |
| `docs/` | Blueprint, architecture, ADRs, agent operating model, dev guides, backlog |
| `skills/` | Reusable, tool-agnostic working instructions for each area |

## Quick start

```bash
# prerequisites: Node >= 20.18, pnpm >= 10, .NET SDK >= 8 (10 works)
pnpm install
pnpm run ci                       # format:check + lint + typecheck + test (all TS)
pnpm run validate:cases           # JSON Schema + cross-reference checks on case fixtures

cd clinical-core && dotnet test   # deterministic engine + golden replay tests
```

Then see [`docs/development/getting-started.md`](docs/development/getting-started.md).

## First vertical slice this foundation targets

```
RN Home → Cases → Case Briefing → START_SIMULATION → Unity (deterministic demo case)
        → SIMULATION_COMPLETED → RN Results / Clinical timeline
```

The RN side of that flow runs today against a deterministic fake bridge
(`apps/mobile/src/unity/`); the native Unity embed is the first open task
([`docs/development/BACKLOG.md`](docs/development/BACKLOG.md), QAN-004).

## License

Not yet chosen — see BACKLOG QAN-030. Treat as "all rights reserved" until then.
