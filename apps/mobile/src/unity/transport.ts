import { FakeUnityBridge, type UnityBridgeTransport } from './fakeBridge';
import { NativeUnityBridgeTransport, getNativeUnityModule } from './nativeBridge';

export type TransportKind = 'native' | 'fake';

export interface SelectedTransport {
  kind: TransportKind;
  transport: UnityBridgeTransport;
}

/**
 * Transport selection is EXPLICIT, never silent:
 *  - If the QanivaUnityBridge native module exists, the real native transport is
 *    used. (Whether UnityFramework is actually embedded is then the native
 *    module's problem — it rejects loudly if not.)
 *  - Otherwise (Expo Go / dev without the native build) the deterministic
 *    FakeUnityBridge is used, the selection is logged, and the caller receives
 *    kind: 'fake' so the UI can label the run as simulated.
 *
 * The FakeUnityBridge is a development/testing aid only. The integration proof
 * and any release build must run kind === 'native'.
 */
export function selectUnityTransport(): SelectedTransport {
  const native = getNativeUnityModule();
  if (native) {
    return { kind: 'native', transport: new NativeUnityBridgeTransport(native) };
  }
  console.warn(
    '[unity/transport] QanivaUnityBridge native module not found — using FakeUnityBridge (dev only)',
  );
  return { kind: 'fake', transport: new FakeUnityBridge() };
}
