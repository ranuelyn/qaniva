import { Body, PrimaryButton, Screen, Title } from '@/components/ui';
import type { ScreenProps } from '@/navigation/types';

export function HomeScreen({ navigation }: ScreenProps<'Home'>) {
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
