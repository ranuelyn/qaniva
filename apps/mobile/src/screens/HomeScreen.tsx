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

export function HomeScreen({ navigation }: ScreenProps<'Home'>) {
  const autostarted = useRef(false);
  useEffect(() => {
    if (E2E_AUTOSTART_CASE && !autostarted.current) {
      autostarted.current = true;
      navigation.navigate('Simulation', {
        caseId: E2E_AUTOSTART_CASE,
        caseVersion: 1,
        attemptId: '22222222-2222-4222-8222-222222222222',
        seed: 20260830,
        title: `E2E ${E2E_AUTOSTART_CASE}`,
      });
    }
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
