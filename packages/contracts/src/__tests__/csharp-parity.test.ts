import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import {
  PROTOCOL_VERSION,
  RN_TO_UNITY_TYPES,
  UNITY_TO_RN_TYPES,
  SIMULATION_FAILURE_CODES,
} from '../protocol';

/**
 * Drift guard: the C# mirror of the bridge protocol must agree with the TS source
 * of truth. This test fails loudly if someone changes one side without the other.
 */
const CSHARP_MIRROR = resolve(
  __dirname,
  '../../../../unity/QanivaSimulation/Assets/Qaniva/Scripts/Bridge/BridgeProtocol.cs',
);

describe('C# bridge protocol mirror parity', () => {
  const source = readFileSync(CSHARP_MIRROR, 'utf8');

  it('declares the same PROTOCOL_VERSION', () => {
    const match = source.match(/ProtocolVersion\s*=\s*(\d+)/);
    expect(match, 'ProtocolVersion constant not found in BridgeProtocol.cs').not.toBeNull();
    expect(Number(match![1])).toBe(PROTOCOL_VERSION);
  });

  it('contains every RN->Unity message type', () => {
    for (const type of RN_TO_UNITY_TYPES) {
      expect(source, `missing ${type}`).toContain(`"${type}"`);
    }
  });

  it('contains every Unity->RN message type', () => {
    for (const type of UNITY_TO_RN_TYPES) {
      expect(source, `missing ${type}`).toContain(`"${type}"`);
    }
  });

  it('contains every simulation failure code', () => {
    for (const code of SIMULATION_FAILURE_CODES) {
      expect(source, `missing ${code}`).toContain(`"${code}"`);
    }
  });
});
