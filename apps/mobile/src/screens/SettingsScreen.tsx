import { useEffect, useState } from 'react';
import { Alert, ScrollView, StyleSheet, View } from 'react-native';
import Constants from 'expo-constants';
import { Caption, Divider, Screen, SectionHeader, SettingsRow } from '@/components/ui';
import { attemptStore } from '@/storage/asyncStorageKv';
import { analytics } from '@/analytics';
import { colors, radius, spacing } from '@/theme/tokens';
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
        <View style={styles.group}>
          <SettingsRow grouped label="Difficulty" detail="Standard (MVP)" />
          <Divider />
          <SettingsRow grouped label="Language" detail="English" />
        </View>

        <SectionHeader>Data</SectionHeader>
        <View style={styles.group}>
          <SettingsRow grouped label="Reset local progress" onPress={confirmReset} destructive />
        </View>
        {resetNote ? <Caption>{resetNote}</Caption> : null}

        <SectionHeader>About</SectionHeader>
        <View style={styles.group}>
          <SettingsRow grouped label="About Qaniva" onPress={() => navigation.navigate('About')} />
          <Divider />
          <SettingsRow
            grouped
            label="Educational use & clinical status"
            onPress={() => navigation.navigate('Disclaimer')}
          />
          <Divider />
          <SettingsRow grouped label="Version" detail={`${version} (MVP)`} />
        </View>
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { gap: spacing.sm, paddingBottom: spacing.xl },
  group: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    overflow: 'hidden',
  },
});
