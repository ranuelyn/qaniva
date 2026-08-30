import { Body, PrimaryButton, Screen, Title } from '@/components/ui';
import { apiClient } from '@/api/client';
import type { ScreenProps } from '@/navigation/types';

export function CaseDetailScreen({ navigation, route }: ScreenProps<'CaseDetail'>) {
  const { caseId, caseVersion, title } = route.params;

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
      <Body muted>
        Briefing: you will be taken to the full-screen 3D simulation. Assess the patient, order and
        treat, then choose a disposition. No diagnosis is shown up front.
      </Body>
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
