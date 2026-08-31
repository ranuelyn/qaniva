import { useCallback, useEffect, useState } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Body, Caption, Card, PrimaryButton, Screen, SectionHeader, Title } from '@/components/ui';
import { apiClient } from '@/api/client';
import { catalogCase, DEFAULT_BRIEFING, specialtyLabel } from '@/cases/catalog';
import { attemptStore } from '@/storage/asyncStorageKv';
import type { StoredAttempt } from '@/storage/attemptStore';
import { analytics } from '@/analytics';
import { cryptoRandomId, randomSeed } from '@/lib/ids';
import { colors, spacing, typography } from '@/theme/tokens';
import type { ScreenProps } from '@/navigation/types';

/**
 * Case briefing (INACSL prebrief). Every line comes from the AUTHORED case
 * data (`metadata.briefing` in case.json via the catalog) — no case text lives
 * in app code.
 */
export function CaseDetailScreen({ navigation, route }: ScreenProps<'CaseDetail'>) {
  const { caseId, caseVersion, title } = route.params;
  const entry = catalogCase(caseId);
  const briefing = entry?.briefing ?? DEFAULT_BRIEFING;
  const [history, setHistory] = useState<StoredAttempt[]>([]);

  useEffect(() => {
    analytics.track({ event: 'case_viewed', caseId });
  }, [caseId]);

  useFocusEffect(
    useCallback(() => {
      let active = true;
      attemptStore.listForCase(caseId).then((attempts) => {
        if (active) setHistory(attempts.slice(-3).reverse());
      });
      return () => {
        active = false;
      };
    }, [caseId]),
  );

  async function begin() {
    let seed = randomSeed();
    let attemptId = cryptoRandomId();
    try {
      const started = await apiClient.startAttempt(caseId, caseVersion, 'standard');
      seed = started.seed;
      attemptId = started.attemptId;
    } catch {
      // Offline: use a locally generated attempt id + seed.
    }
    navigation.navigate('Simulation', { caseId, caseVersion, attemptId, seed, title });
  }

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <Title>{title}</Title>
        {entry ? (
          <Caption>
            {specialtyLabel(entry.manifest.specialty)} · estimated ~
            {entry.manifest.estimatedMinutes} min session
          </Caption>
        ) : null}

        <SectionHeader>Briefing</SectionHeader>
        <Card>
          {briefing.map((line, i) => (
            <View key={i} style={styles.briefingLine}>
              <Text style={styles.briefingBullet}>—</Text>
              <Body muted>{line}</Body>
            </View>
          ))}
        </Card>

        {history.length > 0 && (
          <>
            <SectionHeader>Your recent attempts</SectionHeader>
            {history.map((a) => (
              <Card key={a.summary.attemptId}>
                <Body muted>
                  {a.summary.totalScore} pts · {a.summary.terminalState} ·{' '}
                  {new Date(a.summary.completedAt).toLocaleString()}
                </Body>
              </Card>
            ))}
          </>
        )}
      </ScrollView>
      <PrimaryButton
        label={history.length > 0 ? 'Play again' : 'Enter simulation'}
        onPress={begin}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { gap: spacing.sm, paddingBottom: spacing.md },
  briefingLine: { flexDirection: 'row', gap: spacing.sm, paddingRight: spacing.md },
  briefingBullet: { ...typography.body, color: colors.brand },
});
