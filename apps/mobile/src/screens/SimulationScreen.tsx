import { useEffect, useRef } from 'react';
import { ActivityIndicator } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import type { SimulationMode } from '@qaniva/contracts';
import { Body, Eyebrow, PrimaryButton, Screen, Wordmark } from '@/components/ui';
import { useUnitySimulation } from '@/unity/useUnitySimulation';
import { analytics } from '@/analytics';
import { colors, spacing } from '@/theme/tokens';
import type { ScreenProps } from '@/navigation/types';

/** E2E capture mode: an aborted run returns to Home so the run loop continues. */
const E2E_MODE = Boolean(process.env.EXPO_PUBLIC_E2E_AUTOSTART);

/** Friendly wording per bridge failure code; the raw detail stays muted below. */
const FAILURE_MESSAGES: Record<string, string> = {
  CASE_LOAD_FAILED: 'This case could not be loaded. Please go back and try again.',
  BRIDGE_PROTOCOL_ERROR: 'The simulation runtime reported a communication problem.',
  UNKNOWN: 'The simulation could not be started.',
};

/**
 * Host for the full-screen Unity simulation. On a Unity-embedded build the Unity
 * window covers this screen while the simulation runs; this RN view is what shows
 * before READY and after EXIT/COMPLETED. Mode is 'interactive' for every user
 * launch; E2E launches (HomeScreen autostart) may pass an e2e mode.
 */
export function SimulationScreen({ navigation, route }: ScreenProps<'Simulation'>) {
  const { caseId, caseVersion, attemptId, seed, title, mode } = route.params;
  const sim = useUnitySimulation();
  const insets = useSafeAreaInsets();

  const { start } = sim;
  useEffect(() => {
    analytics.track({ event: 'case_start', caseId, caseVersion, attemptId });
    start({ caseId, caseVersion, attemptId, seed, mode: mode as SimulationMode | undefined });
  }, [start, caseId, caseVersion, attemptId, seed, mode]);

  const trackedEnd = useRef(false);
  useEffect(() => {
    if (sim.phase === 'completed' && sim.summary) {
      if (!trackedEnd.current) {
        trackedEnd.current = true;
        analytics.track({
          event: 'case_complete',
          caseId,
          caseVersion,
          attemptId,
          terminalOutcome: sim.summary.terminalState,
          totalScore: sim.summary.totalScore,
          durationRealSec: 0, // real-time duration is derivable from the summary timestamps
        });
      }
      navigation.replace('Results', { caseId, title, summary: sim.summary });
    } else if (sim.phase === 'exited') {
      // User aborted inside Unity (EXIT_REQUESTED): no results for an aborted
      // attempt — return to where they came from.
      if (!trackedEnd.current) {
        trackedEnd.current = true;
        analytics.track({ event: 'case_abort', caseId, attemptId });
      }
      if (E2E_MODE) {
        navigation.popToTop();
      } else {
        navigation.goBack();
      }
    }
  }, [sim.phase, sim.summary, navigation, caseId, caseVersion, attemptId, title]);

  return (
    <Screen
      style={{ paddingTop: insets.top + spacing.lg, paddingBottom: insets.bottom + spacing.lg }}
    >
      <Wordmark compact />
      <Eyebrow>{sim.phase === 'failed' ? 'Simulation unavailable' : 'Simulation mode'}</Eyebrow>
      <Body muted>{title}</Body>
      {sim.phase === 'failed' ? (
        <>
          <Body>
            {FAILURE_MESSAGES[sim.error?.split(':')[0] ?? 'UNKNOWN'] ?? FAILURE_MESSAGES.UNKNOWN}
          </Body>
          <Body muted>Details: {sim.error}</Body>
          <PrimaryButton label="Back to cases" onPress={() => navigation.popToTop()} />
        </>
      ) : (
        <>
          <ActivityIndicator color={colors.brand} />
          <Body muted>
            {sim.phase === 'starting' && 'Preparing the simulation…'}
            {sim.phase === 'ready' && 'Entering the simulation…'}
            {sim.phase === 'idle' && 'Preparing the simulation…'}
            {sim.phase === 'exited' && 'Leaving the simulation…'}
          </Body>
          {sim.transportKind === 'fake' && (
            <Body muted>
              ⚠ Development build without the simulation runtime — results are simulated
              placeholders.
            </Body>
          )}
        </>
      )}
    </Screen>
  );
}
