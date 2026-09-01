import { useCallback, useEffect, useState } from 'react';
import { ScrollView, StyleSheet, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import {
  Body,
  Caption,
  Card,
  Divider,
  Eyebrow,
  PrimaryButton,
  Screen,
  SectionHeader,
  Title,
} from '@/components/ui';
import { apiClient } from '@/api/client';
import { catalogCase, DEFAULT_BRIEFING, specialtyLabel } from '@/cases/catalog';
import { attemptStore } from '@/storage/asyncStorageKv';
import type { StoredAttempt } from '@/storage/attemptStore';
import { analytics } from '@/analytics';
import { cryptoRandomId, randomSeed } from '@/lib/ids';
import { colors, spacing } from '@/theme/tokens';
import type { ScreenProps } from '@/navigation/types';

/**
 * Case briefing (INACSL prebrief). Every line comes from the AUTHORED case
 * data (`metadata.briefing` in case.json via the catalog) — no case text lives
 * in app code.
 */
export function CaseDetailScreen({ navigation, route }: ScreenProps<'CaseDetail'>) {
  const { caseId, caseVersion, title: routeTitle } = route.params;
  const entry = catalogCase(caseId);
  // Deep links (qaniva://case/<id>) carry no title param — fall back to the catalog.
  const title = routeTitle || (entry?.manifest.title ?? caseId);
  const briefing = entry?.briefing ?? DEFAULT_BRIEFING;
  const [history, setHistory] = useState<StoredAttempt[]>([]);
  const authoredTaskLines = briefing.filter(
    (line) => line.startsWith('Your task:') || line.startsWith('This is an educational'),
  );
  const taskLines = authoredTaskLines.length > 0 ? authoredTaskLines : briefing;
  const contextLines =
    authoredTaskLines.length > 0 ? briefing.filter((line) => !taskLines.includes(line)) : [];

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

        {contextLines.length > 0 ? <SectionHeader>Case information</SectionHeader> : null}
        {contextLines.length > 0 ? (
          <Card>
            {contextLines.map((line, i) => {
              const separator = line.indexOf(':');
              const label = separator > 0 ? line.slice(0, separator) : 'Context';
              const detail = separator > 0 ? line.slice(separator + 1).trim() : line;
              return (
                <View key={line}>
                  {i > 0 ? <Divider /> : null}
                  <View style={styles.briefingLine}>
                    <Eyebrow>{label}</Eyebrow>
                    <Body muted>{detail}</Body>
                  </View>
                </View>
              );
            })}
          </Card>
        ) : null}

        <SectionHeader>Your task</SectionHeader>
        <View style={styles.taskBlock}>
          {taskLines.map((line) => {
            const separator = line.indexOf(':');
            const detail = separator > 0 ? line.slice(separator + 1).trim() : line;
            return <Body key={line}>{detail}</Body>;
          })}
        </View>

        {history.length > 0 && (
          <>
            <SectionHeader>Your recent attempts</SectionHeader>
            {history.map((a) => (
              <View key={a.summary.attemptId} style={styles.attemptRow}>
                <Body muted>
                  {a.summary.totalScore} pts · {a.summary.terminalState} ·{' '}
                  {new Date(a.summary.completedAt).toLocaleString()}
                </Body>
              </View>
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
  briefingLine: { gap: spacing.xs, paddingVertical: spacing.sm },
  taskBlock: {
    borderLeftWidth: 3,
    borderLeftColor: colors.brand,
    paddingLeft: spacing.md,
    gap: spacing.sm,
  },
  attemptRow: {
    paddingVertical: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
});
