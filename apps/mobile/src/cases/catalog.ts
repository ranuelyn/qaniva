import type { CaseManifestEntry } from '@/api/client';

/**
 * Bundled case catalog + learner-facing prebriefs for offline/local use (the
 * backend `GET /cases`, when reachable, serves the same set from the fixtures).
 *
 * DRIFT GUARD: catalog.test.ts asserts every entry here matches the actual
 * versioned case JSON in packages/case-schema/fixtures — title, version,
 * chief complaint, minutes and review status cannot silently diverge.
 *
 * Briefings are product copy (INACSL-style: role, setting, resources, handoff,
 * task, fiction contract — no diagnosis spoilers). Clinical truth stays in the
 * case data.
 */

export interface CatalogCase {
  manifest: CaseManifestEntry;
  briefing: string[];
}

export const CASE_CATALOG: CatalogCase[] = [
  {
    manifest: {
      id: 'stemi_anterior_001',
      version: 1,
      title: 'Crushing chest pain in a 54-year-old',
      chiefComplaint: 'Severe central chest pain for the last 90 minutes',
      specialty: 'emergency_medicine',
      estimatedMinutes: 10,
      clinicalReviewStatus: 'mvp_demo_approved',
    },
    briefing: [
      'Role: you are the ED doctor receiving this patient in the resuscitation room.',
      'Setting: an urban PCI-capable hospital, weekday daytime. The cath lab is operational; cardiology and interventional cardiology are on call.',
      'Resources: monitor/defibrillator, ED drug stock, laboratory, portable X-ray, a resus nurse.',
      'Triage note: "54M, severe central chest pain for 90 minutes, sweaty. Triage category 2 — taken to resus."',
      'Your task: assess and manage the patient until a disposition decision. Time advances with your actions — it matters.',
      'This is an educational simulation of a fictional patient (internal MVP demo — clinical validation pending). Act as you would clinically.',
    ],
  },
  {
    manifest: {
      id: 'anaphylaxis_food_001',
      version: 1,
      title: 'Sudden rash and wheeze after lunch',
      chiefComplaint: 'Rash, facial swelling and difficulty breathing for 25 minutes',
      specialty: 'emergency_medicine',
      estimatedMinutes: 8,
      clinicalReviewStatus: 'mvp_demo_approved',
    },
    briefing: [
      'Role: you are the ED doctor receiving this patient in the resuscitation room.',
      'Setting: an urban hospital ED, weekday lunchtime. Resus is stocked; ICU and anesthesia are on call.',
      'Resources: monitor/defibrillator, ED drug stock incl. emergency medications, oxygen, IV fluids, a resus nurse.',
      'Triage note: "24F, sudden rash, lip swelling and wheeze ~25 minutes after eating at a restaurant. Triage category 2 — taken to resus."',
      'Your task: assess and manage the patient until a disposition decision. Time advances with your actions — it matters.',
      'This is an educational simulation of a fictional patient (internal MVP demo — clinical validation pending). Act as you would clinically.',
    ],
  },
  {
    manifest: {
      id: 'demo_sync_bradycardia_001',
      version: 1,
      title: 'FICTIONAL DEMO — Adult with a slow pulse and dizziness',
      chiefComplaint: 'Feeling dizzy and weak for the last hour',
      specialty: 'emergency_medicine',
      estimatedMinutes: 8,
      clinicalReviewStatus: 'not_reviewed',
    },
    briefing: [
      'Briefing: you will be taken to the full-screen 3D simulation. Assess the patient, order and treat, then choose a disposition. No diagnosis is shown up front.',
    ],
  },
];

export function catalogCase(caseId: string): CatalogCase | undefined {
  return CASE_CATALOG.find((c) => c.manifest.id === caseId);
}

export const DEFAULT_BRIEFING = [
  'Briefing: you will be taken to the full-screen 3D simulation. Assess the patient, order and treat, then choose a disposition. No diagnosis is shown up front.',
];
