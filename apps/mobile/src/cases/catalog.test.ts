import { describe, expect, it } from 'vitest';
import { CASE_CATALOG, catalogCase, specialtyLabel } from './catalog';

/**
 * The catalog is DERIVED from the case fixtures (imported JSON), so drift is
 * structurally impossible; these tests guard the derivation contract and the
 * no-spoiler rule for everything the shell shows before a diagnosis is made.
 */
describe('case catalog', () => {
  it('exposes every bundled case with complete manifest data', () => {
    expect(CASE_CATALOG.length).toBeGreaterThanOrEqual(3);
    for (const c of CASE_CATALOG) {
      expect(c.manifest.id).toMatch(/^[a-z0-9_]+$/);
      expect(c.manifest.version).toBeGreaterThanOrEqual(1);
      expect(c.manifest.title.length).toBeGreaterThan(3);
      expect(c.manifest.estimatedMinutes).toBeGreaterThan(0);
      expect(c.briefing.length).toBeGreaterThan(0);
      expect(c.teaser.length).toBeGreaterThan(3);
    }
  });

  it('learner-visible pre-sim text carries no diagnosis spoiler', () => {
    for (const c of CASE_CATALOG) {
      const visible = [c.manifest.title, c.teaser, ...c.briefing].join(' ').toLowerCase();
      for (const spoiler of ['stemi', 'infarct', 'anaphyla', 'bradycard']) {
        // The fictional demo case is exempt for 'bradycard' (its title is a
        // labeled engine demo, not a diagnostic-discovery case).
        if (c.manifest.id.startsWith('demo_') && spoiler === 'bradycard') continue;
        expect(visible.includes(spoiler), `${c.manifest.id} leaks "${spoiler}"`).toBe(false);
      }
    }
  });

  it('looks up cases by id and prettifies specialties', () => {
    expect(catalogCase('stemi_anterior_001')?.manifest.version).toBe(1);
    expect(catalogCase('missing')).toBeUndefined();
    expect(specialtyLabel('emergency_medicine')).toBe('Emergency Medicine');
  });
});
