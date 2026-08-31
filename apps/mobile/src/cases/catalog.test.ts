import { readFileSync, existsSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { CASE_CATALOG } from './catalog';

/**
 * Drift guard: the bundled catalog must match the versioned case fixtures.
 * If a case's title/version/complaint/minutes/review status changes, this
 * fails until the catalog is updated (content-in-code cannot silently rot).
 */
const FIXTURES = resolve(__dirname, '../../../../packages/case-schema/fixtures');

describe('case catalog', () => {
  for (const entry of CASE_CATALOG) {
    it(`matches the ${entry.manifest.id} fixture`, () => {
      const path = resolve(FIXTURES, entry.manifest.id, `v${entry.manifest.version}`, 'case.json');
      expect(existsSync(path), `fixture missing for catalog entry ${entry.manifest.id}`).toBe(true);
      const fixture = JSON.parse(readFileSync(path, 'utf8')) as {
        version: number;
        metadata: {
          title: string;
          chiefComplaint: string;
          specialty: string;
          estimatedMinutes: number;
          clinicalReview: { status: string };
        };
      };
      expect(entry.manifest.title).toBe(fixture.metadata.title);
      expect(entry.manifest.version).toBe(fixture.version);
      expect(entry.manifest.chiefComplaint).toBe(fixture.metadata.chiefComplaint);
      expect(entry.manifest.specialty).toBe(fixture.metadata.specialty);
      expect(entry.manifest.estimatedMinutes).toBe(fixture.metadata.estimatedMinutes);
      expect(entry.manifest.clinicalReviewStatus).toBe(fixture.metadata.clinicalReview.status);
    });

    it(`${entry.manifest.id} briefing carries no diagnosis spoiler`, () => {
      const text = entry.briefing.join(' ').toLowerCase();
      for (const spoiler of ['stemi', 'infarct', 'anaphyla', 'bradycard']) {
        expect(text.includes(spoiler), `briefing leaks "${spoiler}"`).toBe(false);
      }
    });
  }
});
