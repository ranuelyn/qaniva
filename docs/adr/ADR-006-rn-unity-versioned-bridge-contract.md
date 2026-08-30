# ADR-006 — Versioned, typed RN↔Unity bridge contract

## Status

Accepted (2026-08-30).

## Context

React Native and Unity must exchange a small number of lifecycle messages across a
native bridge. Ad-hoc string messages drift silently between two languages and two
teams and are impossible to evolve safely.

## Decision

A single **versioned, typed message contract**. `packages/contracts/src/protocol.ts`
is the source of truth; `unity/QanivaSimulation/Assets/Qaniva/Scripts/Bridge/BridgeProtocol.cs`
mirrors it, and `packages/contracts/src/__tests__/csharp-parity.test.ts` fails CI
if they diverge.

- Every message shares an envelope: `{ protocolVersion, type, messageId, sentAt, payload }`.
- `PROTOCOL_VERSION` starts at `1`; bump on **any** breaking envelope/payload change,
  on both sides.
- RN → Unity: `START_SIMULATION`, `PAUSE_SIMULATION`, `RESUME_SIMULATION`, `EXIT_SIMULATION`.
- Unity → RN: `SIMULATION_READY`, `SIMULATION_COMPLETED`, `SIMULATION_FAILED`, `EXIT_REQUESTED`.
- Payloads are small and validated (Zod on the RN side, a strict codec on the Unity
  side). The long event log is persisted via the backend keyed by `attemptId`, not
  carried over the bridge.
- No shared global state between the two runtimes.

The native transport (`NativeUnityBridge`) is a documented spike; the architecture
is proven end-to-end today with a `FakeUnityBridge` (RN) / `StubClinicalRuntime`
(Unity) and integration tests on both sides.

## Alternatives considered

- **Free-form JSON strings.** Rejected: the drift problem this ADR exists to solve.
- **A shared schema language (protobuf / FlatBuffers).** Overkill for ~8 message
  types; adds a codegen step and a runtime dependency to both platforms.
- **A single generated file from one source.** Attractive; deferred. The parity
  test gives most of the benefit now with less machinery. `pnpm --filter
  @qaniva/contracts run gen:protocol-json` emits a language-neutral description for
  future codegen.

## Consequences

- Protocol changes are a two-file change plus a version bump; the parity test
  enforces it.
- The RN host screen and the Unity controller both program against the contract,
  so swapping the fake bridge for the native one changes no calling code.
