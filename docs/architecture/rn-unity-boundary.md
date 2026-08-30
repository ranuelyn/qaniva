# RN ↔ Unity boundary

See [ADR-006](../adr/ADR-006-rn-unity-versioned-bridge-contract.md) for the
decision. This page is the working reference.

## Navigation model

```
RN screen  →  full-screen Unity simulation  →  RN results / debrief
```

Unity is **only** active during a simulation. There is no "Unity in a small view",
and no shared global state between the runtimes.

## The envelope

```jsonc
{
  "protocolVersion": 1,          // packages/contracts PROTOCOL_VERSION
  "type": "START_SIMULATION",
  "messageId": "uuid-v4",        // sender-generated, for logs/correlation
  "sentAt": "2026-08-30T10:00:00.000Z",
  "payload": { /* per-type */ }
}
```

## Messages

| Direction | Type | Payload | Meaning |
| --- | --- | --- | --- |
| RN → Unity | `START_SIMULATION` | `caseId, caseVersion, attemptId, locale, difficulty, seed` | load + init + render |
| RN → Unity | `PAUSE_SIMULATION` / `RESUME_SIMULATION` | `{}` | app lifecycle |
| RN → Unity | `EXIT_SIMULATION` | `reason` (`user_quit`/`app_background`/`host_navigation`) | tear down |
| Unity → RN | `SIMULATION_READY` | `caseId, attemptId, warmupSec` | first frame rendered |
| Unity → RN | `SIMULATION_COMPLETED` | `attemptId, summary: AttemptSummary` | terminal reached |
| Unity → RN | `SIMULATION_FAILED` | `attemptId?, code, message` | unrecoverable (see failure codes) |
| Unity → RN | `EXIT_REQUESTED` | `attemptId?, reason` | Unity wants the host to unload it |

`AttemptSummary` (compact): `attemptId, caseId, caseVersion, seed, startedAt,
completedAt, terminalState, totalScore, scoreBreakdown, timeline[], replayHash`.
The full event log is uploaded to the backend separately, keyed by `attemptId`.

## Keeping the two sides in sync

- TS source of truth: `packages/contracts/src/protocol.ts` + `bridge-messages.ts`
  (Zod-validated).
- C# mirror: `unity/QanivaSimulation/Assets/Qaniva/Scripts/Bridge/BridgeProtocol.cs`
  (constants) + `BridgeMessageCodec.cs` (strict decode).
- Enforcement: `packages/contracts/src/__tests__/csharp-parity.test.ts` fails CI if
  the version or any message/failure-code string diverges.
- `pnpm --filter @qaniva/contracts run gen:protocol-json` emits a neutral
  description for future full codegen.

**To change the protocol:** edit `protocol.ts`, mirror into `BridgeProtocol.cs`,
bump `PROTOCOL_VERSION` on both sides for any breaking change, update this page.

## Native embed status (open — QAN-004)

`NativeUnityBridge` is the single seam:

- **iOS**: the host app's `UnityFramework` calls `sendMessageToGO` to reach
  `SimulationBridgeController.OnHostMessage`; Unity → host via an exported
  `__Internal` function.
- **Android**: `UnitySendMessage` in; a small Java plugin (`QanivaBridgePlugin`)
  out.

Until it lands, the architecture is proven with `FakeUnityBridge` (RN,
`apps/mobile/src/unity/`) and `StubClinicalRuntime` (Unity), each with tests that
assert the full `START → READY → COMPLETED` round trip. The RN host screen and the
Unity controller both program against the contract, so the swap is transport-only.

### Spike plan for QAN-004

1. `expo prebuild` the mobile app; add the exported Unity library to the iOS and
   Android native projects.
2. Implement `NativeUnityBridge.SendToHost` for each platform and the host→Unity
   entry point.
3. Replace `makeBridge` in `useUnitySimulation` with the native transport behind a
   feature check; keep the fake for tests.
4. Verify `START → READY → COMPLETED` on a real iOS device and a real Android
   device. Check lifecycle, orientation, audio focus, memory, back navigation.
