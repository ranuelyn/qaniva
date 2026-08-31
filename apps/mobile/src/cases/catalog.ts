import type { CaseManifestEntry } from '@/api/client';
import stemiCase from '../../../../packages/case-schema/fixtures/stemi_anterior_001/v1/case.json';
import anaphylaxisCase from '../../../../packages/case-schema/fixtures/anaphylaxis_food_001/v1/case.json';
import demoCase from '../../../../packages/case-schema/fixtures/demo_sync_bradycardia_001/v1/case.json';

/**
 * Bundled case catalog, derived DIRECTLY from the versioned case fixtures —
 * titles, briefings, complaints and durations are authored case data
 * (`metadata.briefing` etc.), never product copy maintained in app code. A new
 * case = author its case.json and add ONE import line here; every shell
 * surface (Home, Cases, Briefing, Progress) picks it up.
 *
 * The backend `GET /cases`, when reachable, serves the same fixture set.
 */

interface BundledCase {
  id: string;
  version: number;
  metadata: {
    title: string;
    chiefComplaint: string;
    briefing?: string[];
    specialty: string;
    estimatedMinutes: number;
    clinicalReview: { status: string };
  };
}

const BUNDLED: BundledCase[] = [
  stemiCase as unknown as BundledCase,
  anaphylaxisCase as unknown as BundledCase,
  demoCase as unknown as BundledCase,
];

export interface CatalogCase {
  manifest: CaseManifestEntry;
  briefing: string[];
  /** Short presentation teaser (the authored chief complaint — never the diagnosis). */
  teaser: string;
}

export const DEFAULT_BRIEFING = [
  'You will be taken to the full-screen 3D simulation. Assess the patient, order and treat, then choose a disposition. No diagnosis is shown up front.',
];

export const CASE_CATALOG: CatalogCase[] = BUNDLED.map((c) => ({
  manifest: {
    id: c.id,
    version: c.version,
    title: c.metadata.title,
    chiefComplaint: c.metadata.chiefComplaint,
    specialty: c.metadata.specialty,
    estimatedMinutes: c.metadata.estimatedMinutes,
    clinicalReviewStatus: c.metadata.clinicalReview.status,
  },
  briefing: c.metadata.briefing ?? DEFAULT_BRIEFING,
  teaser: c.metadata.chiefComplaint,
}));

export function catalogCase(caseId: string): CatalogCase | undefined {
  return CASE_CATALOG.find((c) => c.manifest.id === caseId);
}

/** Human wording for the specialty slug (presentation only). */
export function specialtyLabel(slug: string): string {
  return slug.replace(/_/g, ' ').replace(/\b\w/g, (m) => m.toUpperCase());
}
