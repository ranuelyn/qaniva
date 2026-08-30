# Skill: unity-rn-bridge

## Purpose

Evolve the RN↔Unity message contract without the TS and C# sides drifting.

## When to use

Adding/changing a bridge message or payload, changing the envelope, or wiring the
native transport (QAN-004).

## Inputs (read first)

- `docs/architecture/rn-unity-boundary.md`
- `docs/adr/ADR-006-rn-unity-versioned-bridge-contract.md`
- `packages/contracts/src/protocol.ts`, `bridge-messages.ts`
- `unity/QanivaSimulation/Assets/Qaniva/Scripts/Bridge/BridgeProtocol.cs`, `BridgeMessageCodec.cs`

## Non-negotiable rules

1. `packages/contracts/src/protocol.ts` is the source of truth. `BridgeProtocol.cs`
   mirrors it exactly (version, message-type strings, failure codes).
2. Bump `PROTOCOL_VERSION` on **both** sides for any breaking envelope/payload change.
3. Payloads stay small. Long event logs go to the backend keyed by `attemptId`,
   never over the bridge.
4. No new "generic" message types. Keep the 4 + 4 lifecycle set unless there's an
   ADR to extend it.
5. Both sides validate: Zod on RN (`decodeRnToUnity`/`decodeUnityToRn`), the strict
   codec on Unity (`BridgeMessageCodec.DecodeEnvelope`).

## Workflow

1. Edit `protocol.ts` (+ `bridge-messages.ts` for a payload schema).
2. Mirror the constants into `BridgeProtocol.cs`; add/adjust the DTO in
   `BridgeEnvelope.cs`; handle it in `SimulationBridgeController`.
3. If breaking: bump `PROTOCOL_VERSION` in `protocol.ts` and `BridgeProtocol.cs`.
4. `pnpm --filter @qaniva/contracts test` (round-trip + `csharp-parity`).
5. Update the message table in `rn-unity-boundary.md`.
6. `pnpm --filter @qaniva/contracts run gen:protocol-json` if you want the neutral
   description refreshed.

## Validation

- `csharp-parity.test.ts` green (version + every string present in `BridgeProtocol.cs`).
- `bridge-messages.test.ts` round-trips the new message and rejects a wrong-channel
  / wrong-version variant.
- Unity `BridgeCodecTests` + `SimulationBridgeControllerTests` still pass (run in
  the Editor).

## Done criteria

TS + C# agree (parity test green); breaking changes bumped the version on both
sides; both codecs validate the new shape; the boundary doc's table matches.

## Common failure modes

- Editing `BridgeProtocol.cs` and forgetting `protocol.ts` (or vice versa) — the
  parity test catches the constants but not payload shape; review both.
- Sneaking a large blob into a payload.
- Adding a message with no failure/timeout handling in the controller.
- Assuming the native transport works before it's verified on real devices.
