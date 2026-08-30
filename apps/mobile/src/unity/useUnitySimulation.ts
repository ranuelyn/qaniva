import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  PROTOCOL_VERSION,
  type AttemptSummary,
  type RnToUnityMessage,
  type UnityToRnMessage,
} from '@qaniva/contracts';
import { FakeUnityBridge, type UnityBridgeTransport } from './fakeBridge';

export type SimulationPhase = 'idle' | 'starting' | 'ready' | 'completed' | 'failed';

export interface SimulationLaunch {
  caseId: string;
  caseVersion: number;
  attemptId: string;
  seed: number;
  locale?: string;
  difficulty?: 'standard' | 'hard';
}

interface UseUnitySimulationResult {
  phase: SimulationPhase;
  summary: AttemptSummary | null;
  error: string | null;
  start: (launch: SimulationLaunch) => void;
  exit: () => void;
}

let seq = 0;
function messageId(): string {
  seq += 1;
  return `00000000-0000-4000-8000-${seq.toString(16).padStart(12, '0')}`;
}

/**
 * Owns the RN side of the simulation conversation. Swap `makeBridge` for the real
 * native transport once QAN-004 lands; the screen code does not change.
 */
export function useUnitySimulation(
  makeBridge: () => UnityBridgeTransport = () => new FakeUnityBridge(),
): UseUnitySimulationResult {
  const bridge = useMemo(makeBridge, [makeBridge]);
  const [phase, setPhase] = useState<SimulationPhase>('idle');
  const [summary, setSummary] = useState<AttemptSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const startedRef = useRef(false);

  useEffect(() => {
    const unsubscribe = bridge.subscribe((message: UnityToRnMessage) => {
      switch (message.type) {
        case 'SIMULATION_READY':
          setPhase('ready');
          break;
        case 'SIMULATION_COMPLETED':
          setSummary(message.payload.summary);
          setPhase('completed');
          break;
        case 'SIMULATION_FAILED':
          setError(`${message.payload.code}: ${message.payload.message}`);
          setPhase('failed');
          break;
        case 'EXIT_REQUESTED':
          setPhase('idle');
          break;
      }
    });
    return () => {
      unsubscribe();
      bridge.dispose();
    };
  }, [bridge]);

  const start = useCallback(
    (launch: SimulationLaunch) => {
      if (startedRef.current) return;
      startedRef.current = true;
      setPhase('starting');
      const message: RnToUnityMessage = {
        protocolVersion: PROTOCOL_VERSION,
        messageId: messageId(),
        sentAt: new Date().toISOString(),
        type: 'START_SIMULATION',
        payload: {
          caseId: launch.caseId,
          caseVersion: launch.caseVersion,
          attemptId: launch.attemptId,
          locale: launch.locale ?? 'en',
          difficulty: launch.difficulty ?? 'standard',
          seed: launch.seed,
        },
      };
      bridge.send(message);
    },
    [bridge],
  );

  const exit = useCallback(() => {
    bridge.send({
      protocolVersion: PROTOCOL_VERSION,
      messageId: messageId(),
      sentAt: new Date().toISOString(),
      type: 'EXIT_SIMULATION',
      payload: { reason: 'user_quit' },
    });
  }, [bridge]);

  return {
    phase,
    summary,
    error,
    start,
    exit,
  };
}
