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
      'Yerel ilerleme sıfırlansın mı?',
      'Bu cihazda saklanan tüm denemeler ve skorlar silinir. Geri alınamaz.',
      [
        { text: 'Vazgeç', style: 'cancel' },
        {
          text: 'Sıfırla',
          style: 'destructive',
          onPress: async () => {
            const result = await attemptStore.clearAll();
            setResetNote(
              result.ok ? 'Yerel ilerleme sıfırlandı.' : 'İlerleme bu cihazda sıfırlanamadı.',
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
        <SectionHeader>Öğrenme</SectionHeader>
        <Group>
          <SettingsRow grouped label="Zorluk" detail="Standart (MVP)" />
          <Divider inset />
          <SettingsRow grouped label="Dil" detail="Türkçe" />
        </Group>

        <SectionHeader>Veri</SectionHeader>
        <Group>
          <SettingsRow
            grouped
            label="Yerel ilerlemeyi sıfırla"
            onPress={confirmReset}
            destructive
          />
        </Group>
        {resetNote ? <Caption>{resetNote}</Caption> : null}

        <SectionHeader>Hakkında</SectionHeader>
        <Group>
          <SettingsRow
            grouped
            label="Qaniva Hakkında"
            onPress={() => navigation.navigate('About')}
          />
          <Divider inset />
          <SettingsRow
            grouped
            label="Eğitim amaçlı kullanım ve klinik durum"
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
