import { NativeEventEmitter, NativeModules } from 'react-native';
import {
  PROTOCOL_VERSION,
  decodeUnityToRn,
  encodeMessage,
  type RnToUnityMessage,
  type UnityToRnMessage,
} from '@qaniva/contracts';

/**
 * Real transport over the QanivaUnityBridge native module (Unity as a Library).
 * Same `UnityBridgeTransport` contract as the fake — the screens don't change.
 *
 * Every inbound message is validated against the shared protocol before it
 * reaches a subscriber; an invalid payload is surfaced loudly, never swallowed.
 */

interface QanivaUnityBridgeModule {
  isUnityAvailable(): Promise<boolean>;
  startUnity(): Promise<boolean>;
  sendToUnity(json: string): void;
  hideUnity(): void;
  resumeUnity(): void;
}

const UNITY_MESSAGE_EVENT = 'QanivaUnityMessage';

export function getNativeUnityModule(): QanivaUnityBridgeModule | null {
  const mod = (NativeModules as Record<string, unknown>)['QanivaUnityBridge'];
  return mod ? (mod as unknown as QanivaUnityBridgeModule) : null;
}

export class NativeUnityBridgeTransport {
  private readonly handlers = new Set<(m: UnityToRnMessage) => void>();
  private readonly emitter: NativeEventEmitter;
  private subscription: { remove(): void } | null = null;
  private started = false;
  private startRetryTimer: ReturnType<typeof setInterval> | null = null;

  constructor(private readonly native: QanivaUnityBridgeModule) {
    this.emitter = new NativeEventEmitter(NativeModules['QanivaUnityBridge']);
  }

  send(message: RnToUnityMessage): void {
    const json = encodeMessage(message);
    if (message.type === 'START_SIMULATION' && !this.started) {
      this.started = true;
      const attemptId = message.payload.attemptId;
      void this.native
        .startUnity()
        .then(() => {
          // Unity's runtime boots asynchronously after runEmbedded returns;
          // sendMessageToGO before the SimulationBridge GameObject exists is
          // dropped silently. Re-send START until Unity answers (any inbound
          // message clears the retry) or a bounded timeout expires.
          this.native.sendToUnity(json);
          let attempts = 0;
          this.clearStartRetry();
          this.startRetryTimer = setInterval(() => {
            attempts += 1;
            if (attempts > 20) {
              this.clearStartRetry();
              this.emitLocal({
                protocolVersion: PROTOCOL_VERSION,
                messageId: '00000000-0000-4000-8000-00000000dea1',
                sentAt: new Date().toISOString(),
                type: 'SIMULATION_FAILED',
                payload: {
                  attemptId,
                  code: 'BRIDGE_PROTOCOL_ERROR',
                  message: 'Unity did not acknowledge START_SIMULATION within 15s',
                },
              });
              return;
            }
            this.native.sendToUnity(json);
          }, 750);
        })
        .catch((err: unknown) => {
          console.error('[NativeUnityBridge] startUnity failed', err);
          // Surface the failure through the normal protocol so the UI reaches
          // the failed state instead of spinning forever.
          this.emitLocal({
            protocolVersion: PROTOCOL_VERSION,
            messageId: '00000000-0000-4000-8000-00000000dead',
            sentAt: new Date().toISOString(),
            type: 'SIMULATION_FAILED',
            payload: {
              attemptId,
              code: 'RENDER_ERROR',
              message: err instanceof Error ? err.message : 'Unity runtime failed to start',
            },
          });
        });
      return;
    }
    this.native.sendToUnity(json);
    if (message.type === 'EXIT_SIMULATION') {
      this.native.hideUnity();
    }
  }

  subscribe(handler: (message: UnityToRnMessage) => void): () => void {
    this.handlers.add(handler);
    if (this.subscription === null) {
      this.subscription = this.emitter.addListener(UNITY_MESSAGE_EVENT, (raw: string) => {
        let decoded: UnityToRnMessage;
        try {
          decoded = decodeUnityToRn(raw);
        } catch (err) {
          console.error('[NativeUnityBridge] invalid Unity->RN message rejected', err, raw);
          return;
        }
        // Unity is alive and talking — stop re-sending START.
        this.clearStartRetry();
        this.handlers.forEach((h) => h(decoded));
      });
    }
    return () => {
      this.handlers.delete(handler);
    };
  }

  private clearStartRetry(): void {
    if (this.startRetryTimer !== null) {
      clearInterval(this.startRetryTimer);
      this.startRetryTimer = null;
    }
  }

  dispose(): void {
    this.clearStartRetry();
    this.subscription?.remove();
    this.subscription = null;
    this.handlers.clear();
    // The Unity runtime intentionally stays warm (initialise-once lifecycle);
    // hide returns control to the RN window.
    this.native.hideUnity();
  }

  private emitLocal(message: UnityToRnMessage): void {
    this.handlers.forEach((h) => h(message));
  }
}
