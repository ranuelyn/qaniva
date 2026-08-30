/**
 * Emits a language-neutral description of the bridge protocol to
 * `packages/contracts/generated/protocol.json`. Useful for code generation and
 * for humans diffing protocol changes in a PR.
 *
 * Run: pnpm --filter @qaniva/contracts run gen:protocol-json
 */
import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import {
  PROTOCOL_VERSION,
  RN_TO_UNITY_TYPES,
  UNITY_TO_RN_TYPES,
  SIMULATION_FAILURE_CODES,
} from '../src/protocol';

const outDir = resolve(__dirname, '../generated');
mkdirSync(outDir, { recursive: true });

const doc = {
  protocolVersion: PROTOCOL_VERSION,
  channels: {
    rnToUnity: RN_TO_UNITY_TYPES,
    unityToRn: UNITY_TO_RN_TYPES,
  },
  simulationFailureCodes: SIMULATION_FAILURE_CODES,
  generatedAt: new Date().toISOString(),
};

const outFile = resolve(outDir, 'protocol.json');
writeFileSync(outFile, `${JSON.stringify(doc, null, 2)}\n`);
console.error(`wrote ${outFile}`);
