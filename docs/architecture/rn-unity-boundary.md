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

## Native embed — iOS (implemented; see ADR-008)

The full message path on iOS:

```
RN JS  useUnitySimulation
         └─ selectUnityTransport()            src/unity/transport.ts
              └─ NativeUnityBridgeTransport   src/unity/nativeBridge.ts
                   └─ QanivaUnityBridge       modules/unity-host (local pod, ObjC++)
                        │  NSBundle-load + objc_msgSend (no link-time Unity dep)
                        ▼
                   UnityFramework  ── sendMessageToGO("SimulationBridge","OnHostMessage",json)
                        ▼
                   SimulationBridgeController (created by BridgeBootstrap at Unity init)
                        │  NativeUnityBridge.SendToHost -> DllImport "__Internal"
                        ▼
                   _QanivaBridge_SendToHost   Assets/Qaniva/Plugins/iOS/QanivaBridgeNative.mm
                        │  host callback registered via dlsym("QanivaRegisterHostHandler")
                        ▼
                   QanivaUnityBridge -> RCTEventEmitter "QanivaUnityMessage" -> RN JS
```

Key decisions (details + alternatives in
[ADR-008](../adr/ADR-008-unity-as-a-library-ios-integration.md)):

- `apps/mobile/ios/` is committed (bare workflow); the transport is the local pod
  `apps/mobile/modules/unity-host`; UnityFramework is embedded by the
  `QanivaUnityFramework` wrapper pod **only when the export exists** at
  `apps/mobile/unity-frameworks/ios/UnityFramework.framework` (git-ignored,
  produced by `scripts/export-unity-ios.sh`).
- The Unity runtime initialises **once per process** and is shown/hidden after
  that (no unload/relaunch cycles).
- Transport selection in JS is explicit: native when the module exists, otherwise
  the labelled `FakeUnityBridge` with a warning + on-screen badge. The fake is a
  dev/test aid only and is never part of a real proof or release path.
- **Android**: not yet implemented — same pattern later (`UnityPlayer` +
  `UnitySendMessage` in, a small Java plugin out).
