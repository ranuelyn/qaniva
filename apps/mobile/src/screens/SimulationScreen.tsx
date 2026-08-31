import { useEffect } from 'react';
import { ActivityIndicator } from 'react-native';
import type { SimulationMode } from '@qaniva/contracts';
import { Body, PrimaryButton, Screen, Title } from '@/components/ui';
import { useUnitySimulation } from '@/unity/useUnitySimulation';
import type { ScreenProps } from '@/navigation/types';

/**
 * Host for the full-screen Unity simulation. On a Unity-embedded build the Unity
 * window covers this screen while the simulation runs; this RN view is what shows
 * before READY and after EXIT/COMPLETED. Mode is 'interactive' for every user
 * launch; E2E launches (HomeScreen autostart) may pass an e2e mode.
 */
export function SimulationScreen({ navigation, route }: ScreenProps<'Simulation'>) {
  const { caseId, caseVersion, attemptId, seed, title, mode } = route.params;
  const sim = useUnitySimulation();

  const { start } = sim;
  useEffect(() => {
    start({ caseId, caseVersion, attemptId, seed, mode: mode as SimulationMode | undefined });
  }, [start, caseId, caseVersion, attemptId, seed, mode]);

  useEffect(() => {
    if (sim.phase === 'completed' && sim.summary) {
      navigation.replace('Results', { caseId, title, summary: sim.summary });
    } else if (sim.phase === 'exited') {
      // User aborted inside Unity (EXIT_REQUESTED): no results for an aborted
      // attempt — return to where they came from.
      navigation.goBack();
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
            {sim.phase === 'exited' && 'Leaving the simulation…'}
          </Body>
          {sim.transportKind === 'fake' && (
            <Body muted>⚠ FAKE BRIDGE (dev build without Unity runtime)</Body>
          )}
        </>
      )}
    </Screen>
  );
}
