import { useEffect } from 'react';
import { ActivityIndicator } from 'react-native';
import { Body, PrimaryButton, Screen, Title } from '@/components/ui';
import { useUnitySimulation } from '@/unity/useUnitySimulation';
import type { ScreenProps } from '@/navigation/types';

/**
 * Host for the full-screen Unity simulation. In the foundation this uses the
 * FakeUnityBridge (deterministic) so the RN flow runs end to end. The real
 * `<UnityView />` embed replaces the body here without changing the surrounding
 * navigation (QAN-004).
 */
export function SimulationScreen({ navigation, route }: ScreenProps<'Simulation'>) {
  const { caseId, caseVersion, attemptId, seed, title } = route.params;
  const sim = useUnitySimulation();

  const { start } = sim;
  useEffect(() => {
    start({ caseId, caseVersion, attemptId, seed });
  }, [start, caseId, caseVersion, attemptId, seed]);

  useEffect(() => {
    if (sim.phase === 'completed' && sim.summary) {
      navigation.replace('Results', { caseId, title, summary: sim.summary });
    }
  }, [sim.phase, sim.summary, navigation, caseId, title]);

  return (
    <Screen>
      <Title>{title}</Title>
      {sim.phase === 'failed' ? (
        <>
          <Body>Simulation failed to start.</Body>
          <Body muted>{sim.error}</Body>
          <PrimaryButton label="Back to cases" onPress={() => navigation.popToTop()} />
        </>
      ) : (
        <>
          <ActivityIndicator />
          <Body muted>
            {sim.phase === 'starting' && 'Loading the deterministic case and warming up Unity…'}
            {sim.phase === 'ready' && 'Running the simulation…'}
            {sim.phase === 'idle' && 'Preparing…'}
          </Body>
        </>
      )}
    </Screen>
  );
}
