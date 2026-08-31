# System overview

```
┌──────────────────────── mobile binary ────────────────────────┐
│                                                               │
│  React Native product shell (TypeScript)                      │
│   Home · Cases · Briefing · Progress · Profile · Results ·    │
│   Clinical timeline · Debrief · analytics · API client        │
│                          │                                    │
│                          │ START_SIMULATION (typed, versioned) │
│                          ▼                                    │
│  Native bridge (iOS + Android)  ──  packages/contracts         │
│                          │                                    │
│                          ▼                                    │
│  Unity 6 / URP simulation runtime                             │
│   room · patient · monitor · camera · animation adapters      │
│   interactive UI (UI Toolkit): tabs · action list · vitals ·  │
│   case log · exit  — renders GetActionAvailability()/timeline │
│                          │ PlayerAction (user tap)             │
│                          ▼                                    │
│  IClinicalRuntime  ──►  Qaniva.Clinical.Core.dll (pure C#)     │
│   CaseDefinition · PatientState · RuleEvaluator ·             │
│   AttemptTimeline · ScoringEngine · seeded RNG                │
│                          │ SimulationSnapshot + cues           │
│                          ▼                                    │
│  Unity visual update                                          │
│                          │ SIMULATION_COMPLETED (AttemptSummary)│
│                          ▼                                    │
│  React Native Results + Debrief                               │
└───────────────────────────────────────────────────────────────┘
                           │  attempts, events, analytics, AI
                           ▼
        apps/api  (NestJS modular monolith)
         cases · attempts · analytics · AI gateway (+ Postgres, later)
                           │
                           ▼
                  AI provider adapter (backend-only)
```

## Responsibilities

| Component | Owns | Never does |
| --- | --- | --- |
| RN shell | product navigation/state/UI, networking, analytics, hosting Unity | compute clinical results; hold Unity scene state |
| Unity runtime | rendering the engine's snapshots, animation, camera, in-sim UI | compute vitals/transitions/scores; store case logic in `MonoBehaviour`s |
| Clinical engine | all clinical truth: rules, timeline, scoring, replay, determinism | I/O, Unity APIs, wall-clock, unseeded randomness |
| Backend | case manifest/versions, attempt & event persistence, analytics, AI gateway | expose secrets to clients; run clinical logic (MVP) |
| AI gateway | one guarded place for LLM calls, structured-output validation, fallback | produce or alter clinical state, vitals, or scores |

## Data that crosses boundaries

| From → To | Payload | Contract |
| --- | --- | --- |
| RN → Unity | lifecycle messages + `attemptId` + `seed` | `packages/contracts` (mirrored in `BridgeProtocol.cs`) |
| Unity → RN | `AttemptSummary` (compact timeline + score + `replayHash`) | `packages/contracts` `attemptSummarySchema` |
| RN/Unity → backend | attempt records, event logs, analytics events | `@qaniva/analytics-schema`, `apps/api` DTOs |
| Backend → RN/Unity | case manifest + full case JSON | `packages/case-schema` |

## Where the code is

See the table in the root [`README.md`](../../README.md#layout).
