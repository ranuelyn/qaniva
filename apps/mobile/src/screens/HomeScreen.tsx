import { useEffect, useRef } from 'react';
import { Body, PrimaryButton, Screen, Title } from '@/components/ui';
import { analytics } from '@/analytics';
import type { ScreenProps } from '@/navigation/types';

/**
 * Dev/E2E convenience: when EXPO_PUBLIC_E2E_AUTOSTART is set to a caseId at
 * bundle time, Home navigates through the REAL Case Briefing screen and then
 * into that simulation — this lets the scripted integration proof (and the
 * screenshot capture of the briefing) run headlessly while changing nothing
 * about the interactive flow. Unset (every normal build/run), none of this
 * executes and the user drives Home -> Cases -> Briefing themselves.
 */
const E2E_AUTOSTART_CASE = process.env.EXPO_PUBLIC_E2E_AUTOSTART ?? '';
/**
 * Which e2e driver the Unity side arms: 'ui' walks the REAL interactive UI
 * (default — proves the interactive path), 'autoplay' drives the runtime
 * directly (bridge/lifecycle regression only).
 */
const E2E_MODE =
  process.env.EXPO_PUBLIC_E2E_MODE === 'autoplay'
    ? 'e2e_autoplay'
    : process.env.EXPO_PUBLIC_E2E_MODE === 'interactive'
      ? 'interactive' // navigate in headlessly but drive nothing (idle-UI proof)
      : 'e2e_ui';
/** Up to 2 runs so the scripted proof also exercises RN->Unity->RN->Unity->RN. */
const E2E_MAX_RUNS = 2;
/** Dwell on the briefing screen so the capture series can photograph it. */
const E2E_BRIEFING_DWELL_MS = 4000;

export function HomeScreen({ navigation }: ScreenProps<'Home'>) {
  useEffect(() => {
    analytics.track({ event: 'app_open' });
  }, []);

  const runs = useRef(0);
  useEffect(() => {
    if (!E2E_AUTOSTART_CASE) return;
    const timers: ReturnType<typeof setTimeout>[] = [];
    const kick = () => {
      if (runs.current >= E2E_MAX_RUNS) return;
      runs.current += 1;
      const run = runs.current;
      // Route through the real briefing screen first (same screens a user sees).
      navigation.navigate('CaseDetail', {
        caseId: E2E_AUTOSTART_CASE,
        caseVersion: 1,
        title: `E2E run ${run}`,
      });
      timers.push(
        setTimeout(() => {
          navigation.navigate('Simulation', {
            caseId: E2E_AUTOSTART_CASE,
            caseVersion: 1,
            // Distinct attemptId per run: Unity treats a repeated attemptId as a
            // host retry (idempotent START), which would skip the second run.
            attemptId: `22222222-2222-4222-8222-22222222222${run}`,
            seed: 20260830,
            title: `E2E run ${run}`,
            mode: E2E_MODE,
          });
        }, E2E_BRIEFING_DWELL_MS),
      );
    };
    // The initial mount also emits 'focus', so the listener alone covers both
    // the first run and each return to Home (calling kick() here too would burn
    // the whole run budget at startup).
    const unsubscribe = navigation.addListener('focus', kick);
    return () => {
      unsubscribe();
      timers.forEach(clearTimeout);
    };
  }, [navigation]);

  return (
    <Screen>
      <Title>Qaniva</Title>
      <Body muted>
        A 3D clinical decision simulation. Pick a case, work the patient, then review a timeline of
        every decision you made.
      </Body>
      <PrimaryButton label="Start a case" onPress={() => navigation.navigate('Cases')} />
    </Screen>
  );
}
