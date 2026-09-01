import { useEffect, useState } from 'react';
import { Alert, ScrollView, StyleSheet, View } from 'react-native';
import Constants from 'expo-constants';
import { Caption, Divider, Group, Screen, SectionHeader, SettingsRow } from '@/components/ui';
import { attemptStore } from '@/storage/asyncStorageKv';
import { analytics } from '@/analytics';
import { spacing } from '@/theme/tokens';
import type { TabScreenProps } from '@/navigation/types';

/**
 * Lightweight MVP settings — no accounts, no fake toggles. Difficulty and
 * language are shown informationally because no alternative modes exist yet.
 */
export function SettingsScreen({ navigation }: TabScreenProps<'Settings'>) {
  const [resetNote, setResetNote] = useState<string | null>(null);

  useEffect(() => {
    analytics.track({ event: 'surface_viewed', surface: 'settings' });
  }, []);

  function confirmReset() {
    Alert.alert(
      'Reset local progress?',
      'This deletes all locally stored attempts and scores on this device. It cannot be undone.',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Reset',
          style: 'destructive',
          onPress: async () => {
            const result = await attemptStore.clearAll();
            setResetNote(
              result.ok
                ? 'Local progress was reset.'
                : 'Progress could not be reset on this device.',
            );
          },
        },
      ],
    );
  }

  const version = Constants.expoConfig?.version ?? '0.0.0';

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <SectionHeader>Learning</SectionHeader>
        <Group>
          <SettingsRow grouped label="Difficulty" detail="Standard (MVP)" />
          <Divider inset />
          <SettingsRow grouped label="Language" detail="English" />
        </Group>

        <SectionHeader>Data</SectionHeader>
        <Group>
          <SettingsRow grouped label="Reset local progress" onPress={confirmReset} destructive />
        </Group>
        {resetNote ? <Caption>{resetNote}</Caption> : null}

        <SectionHeader>About</SectionHeader>
        <Group>
          <SettingsRow grouped label="About Qaniva" onPress={() => navigation.navigate('About')} />
          <Divider inset />
          <SettingsRow
            grouped
            label="Educational use & clinical status"
            onPress={() => navigation.navigate('Disclaimer')}
          />
        </Group>

        <View style={styles.footer}>
          <Caption>Qaniva {version} (MVP)</Caption>
        </View>
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { gap: spacing.sm, paddingBottom: spacing.xl },
  footer: { alignItems: 'center', paddingTop: spacing.lg },
});
