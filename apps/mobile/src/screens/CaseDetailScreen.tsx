import { Body, PrimaryButton, Screen, Title } from '@/components/ui';
import { apiClient } from '@/api/client';
import type { ScreenProps } from '@/navigation/types';

/**
 * Learner-facing prebriefs (INACSL-style: role, setting, resources, handoff,
 * task, fiction contract — no diagnosis spoilers). Product copy derived from the
 * case blueprints; clinical truth stays in the case data.
 */
const BRIEFINGS: Record<string, string[]> = {
  stemi_anterior_001: [
    'Role: you are the ED doctor receiving this patient in the resuscitation room.',
    'Setting: an urban PCI-capable hospital, weekday daytime. The cath lab is operational; cardiology and interventional cardiology are on call.',
    'Resources: monitor/defibrillator, ED drug stock, laboratory, portable X-ray, a resus nurse.',
    'Triage note: "54M, severe central chest pain for 90 minutes, sweaty. Triage category 2 — taken to resus."',
    'Your task: assess and manage the patient until a disposition decision. Time advances with your actions — it matters.',
    'This is an educational simulation of a fictional patient (internal MVP demo — clinical validation pending). Act as you would clinically.',
  ],
};

const DEFAULT_BRIEFING = [
  'Briefing: you will be taken to the full-screen 3D simulation. Assess the patient, order and treat, then choose a disposition. No diagnosis is shown up front.',
];

export function CaseDetailScreen({ navigation, route }: ScreenProps<'CaseDetail'>) {
  const { caseId, caseVersion, title } = route.params;
  const briefing = BRIEFINGS[caseId] ?? DEFAULT_BRIEFING;

  async function begin() {
    let seed = Math.floor(Math.random() * 2 ** 31);
    let attemptId = cryptoRandomId();
    try {
      const started = await apiClient.startAttempt(caseId, caseVersion, 'standard');
      seed = started.seed;
      attemptId = started.attemptId;
    } catch {
      // Offline: use a locally generated attempt id + seed.
    }
    navigation.navigate('Simulation', { caseId, caseVersion, attemptId, seed, title });
  }

  return (
    <Screen>
      <Title>{title}</Title>
      {briefing.map((line, i) => (
        <Body key={i} muted>
          {line}
        </Body>
      ))}
      <PrimaryButton label="Enter simulation" onPress={begin} />
    </Screen>
  );
}

function cryptoRandomId(): string {
  // RFC-4122-shaped; good enough for an offline attempt id.
  const hex = Array.from({ length: 32 }, () => Math.floor(Math.random() * 16).toString(16)).join(
    '',
  );
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-4${hex.slice(13, 16)}-8${hex.slice(17, 20)}-${hex.slice(20, 32)}`;
}
