import { useEffect, useRef } from 'react';
import { Body, PrimaryButton, Screen, Title } from '@/components/ui';
import type { ScreenProps } from '@/navigation/types';

/**
 * Dev/E2E convenience: when EXPO_PUBLIC_E2E_AUTOSTART is set to a caseId at
 * bundle time, Home navigates straight into that simulation once — this lets the
 * scripted integration proof run headlessly (no UI automation) while changing
 * nothing about the interactive flow.
 */
const E2E_AUTOSTART_CASE = process.env.EXPO_PUBLIC_E2E_AUTOSTART ?? '';
/** Up to 2 runs so the scripted proof also exercises RN->Unity->RN->Unity->RN. */
const E2E_MAX_RUNS = 2;

export function HomeScreen({ navigation }: ScreenProps<'Home'>) {
  const runs = useRef(0);
  useEffect(() => {
    if (!E2E_AUTOSTART_CASE) return;
    const kick = () => {
      if (runs.current >= E2E_MAX_RUNS) return;
      runs.current += 1;
      navigation.navigate('Simulation', {
        caseId: E2E_AUTOSTART_CASE,
        caseVersion: 1,
        // Distinct attemptId per run: Unity treats a repeated attemptId as a
        // host retry (idempotent START), which would skip the second run.
        attemptId: `22222222-2222-4222-8222-22222222222${runs.current}`,
        seed: 20260830,
        title: `E2E run ${runs.current}`,
      });
    };
    // The initial mount also emits 'focus', so the listener alone covers both
    // the first run and each return to Home (calling kick() here too would burn
    // the whole run budget at startup).
    return navigation.addListener('focus', kick);
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
