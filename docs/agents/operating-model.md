# Agent operating model

Qaniva will be built largely by AI coding agents working alongside people. This is
how the work is divided so it stays coherent. These agents are **development**
agents — they write code, assets, and content. They are not runtime actors in the
product.

## Roles

Nine roles, deliberately small. Definitions live in `.claude/agents/`.

| # | Role | Owns | Must not |
| --- | --- | --- | --- |
| 1 | **Director / Orchestrator** | task decomposition, routing to a specialist, PR acceptance, dependency ordering, scope-creep defense | approve clinical truth or 3D assets alone; write feature code |
| 2 | **Product Architect** | architecture, ADRs, domain boundaries, contracts | implement large features; bypass an ADR silently |
| 3 | **Mobile Agent** | RN/TS, navigation, screens, the Unity host, the bridge client | copy engine rules into RN; write Unity scene logic |
| 4 | **Unity Agent** | Unity/C#/URP, scenes, prefabs, presentation adapters, Unity-as-a-Library, mobile perf | change case medical truth; put clinical logic in `MonoBehaviour`s |
| 5 | **Clinical Engine Agent** | the pure C# engine: state machine, rules, determinism, scoring, replay | reference Unity; add wall-clock or unseeded randomness; weaken a golden test |
| 6 | **Backend Agent** | NestJS API, persistence, contract endpoints | leak client secrets; run clinical logic in the API |
| 7 | **Clinical Content Agent** | authoring `case.json` against the schema, rubric mapping | assert clinical correctness — must flag every clinical number for clinician review |
| 8 | **QA / Skeptic Agent** | tests, invariants, architecture-drift checks, regression, safety review | guess an expected result from the prompt instead of computing it |
| 9 | **Release Agent** | CI, builds, versioning, TestFlight/Play checklist, crash symbols | fill store/data declarations without verification |

Combine roles when a task is small; never give one agent "make Qaniva" scope.

## Task protocol

1. **Director** writes a task spec: goal, files in scope, explicit out-of-scope,
   machine-checkable acceptance criteria.
2. The specialist reads the relevant **skill** + **ADR** + existing tests first.
3. It produces a **small diff** — not engine + UI + backend in one change.
4. It writes/runs its own tests and pastes the command output into the handoff.
5. **QA** replays the same action sequence in the headless engine and compares to
   the expected snapshot; checks invariants and drift.
6. A clinical change is not "done" without a clinician review checklist entry.
7. **Director / integrator** merges only on green CI + review.
8. Skills and ADRs are updated in the same PR as the change they describe.

## The rule that matters most

Agents do **not** get "looks like it works, so it's fine" authority. In the engine
and in clinical content, output needs a verifiable test or a reviewer sign-off.
Placeholder code must announce that it is a placeholder.

## Handoff format

Each task ends with: what changed (files), why, commands run + output, what the
next agent should pick up, and any new `Open Question`.
