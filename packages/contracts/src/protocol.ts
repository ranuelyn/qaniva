/**
 * Qaniva RN <-> Unity bridge protocol — single source of truth.
 *
 * The C# mirror lives at
 * `unity/QanivaSimulation/Assets/Qaniva/Scripts/Bridge/BridgeProtocol.cs`
 * and is checked against this file by `src/__tests__/csharp-parity.test.ts`.
 *
 * Rules:
 *  - Bump PROTOCOL_VERSION on ANY breaking change to an envelope or payload shape.
 *  - Never reuse a removed message-type string.
 *  - Payloads are small. Long event logs are persisted via the backend, not the bridge.
 */

export const PROTOCOL_VERSION = 1 as const;

/** Messages sent from React Native into the Unity simulation runtime. */
export const RN_TO_UNITY_TYPES = [
  'START_SIMULATION',
  'PAUSE_SIMULATION',
  'RESUME_SIMULATION',
  'EXIT_SIMULATION',
] as const;

/** Messages emitted by the Unity simulation runtime back to React Native. */
export const UNITY_TO_RN_TYPES = [
  'SIMULATION_READY',
  'SIMULATION_COMPLETED',
  'SIMULATION_FAILED',
  'EXIT_REQUESTED',
] as const;

export type RnToUnityType = (typeof RN_TO_UNITY_TYPES)[number];
export type UnityToRnType = (typeof UNITY_TO_RN_TYPES)[number];
export type BridgeMessageType = RnToUnityType | UnityToRnType;

/** Stable failure codes for SIMULATION_FAILED. */
export const SIMULATION_FAILURE_CODES = [
  'CASE_LOAD_FAILED',
  'CASE_VERSION_MISMATCH',
  'ENGINE_ERROR',
  'RENDER_ERROR',
  'BRIDGE_PROTOCOL_ERROR',
  'UNKNOWN',
] as const;

export type SimulationFailureCode = (typeof SIMULATION_FAILURE_CODES)[number];
