import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { validateCase, validateCaseStructure } from '../validator';

const demoCasePath = resolve(__dirname, '../../fixtures/demo_sync_bradycardia_001/v1/case.json');
const demoCase = JSON.parse(readFileSync(demoCasePath, 'utf8')) as Record<string, unknown>;

function clone(): Record<string, unknown> {
  return JSON.parse(JSON.stringify(demoCase)) as Record<string, unknown>;
}

describe('case schema validation', () => {
  it('accepts the committed demo fixture', () => {
    const result = validateCase(demoCase);
    expect(result.issues).toEqual([]);
    expect(result.valid).toBe(true);
  });

  it('rejects a case missing a required top-level section', () => {
    const bad = clone();
    delete bad.scoringCriteria;
    const result = validateCaseStructure(bad);
    expect(result.valid).toBe(false);
  });

  it('rejects fictional=false (MVP invariant)', () => {
    const bad = clone();
    (bad.metadata as Record<string, unknown>).fictional = false;
    expect(validateCase(bad).valid).toBe(false);
  });

  it('flags a scoring criterion that points at an unknown action id', () => {
    const bad = clone();
    (bad.scoringCriteria as { acceptedActions: string[] }[])[0]!.acceptedActions = [
      'does_not_exist',
    ];
    const result = validateCase(bad);
    expect(result.valid).toBe(false);
    expect(result.issues.some((i) => i.message.includes('unknown action id'))).toBe(true);
  });

  it('flags a transition rule that names an unknown terminal state', () => {
    const bad = clone();
    (bad.transitionRules as { terminalState: string | null }[])[0]!.terminalState = 'ghost_state';
    const result = validateCase(bad);
    expect(result.valid).toBe(false);
    expect(result.issues.some((i) => i.message.includes('unknown terminal state'))).toBe(true);
  });

  it('flags a duplicate action id', () => {
    const bad = clone();
    const actions = bad.availableActions as { id: string }[];
    actions.push({ ...actions[0]! });
    const result = validateCase(bad);
    expect(result.valid).toBe(false);
    expect(result.issues.some((i) => i.message.includes('duplicate action ids'))).toBe(true);
  });

  it('requires visibleWhen when visibility is "when"', () => {
    const bad = clone();
    const actions = bad.availableActions as {
      id: string;
      visibility: string;
      visibleWhen: string | null;
    }[];
    const pacing = actions.find((a) => a.id === 'transcutaneous_pacing')!;
    pacing.visibleWhen = null;
    const result = validateCase(bad);
    expect(result.valid).toBe(false);
  });
});
