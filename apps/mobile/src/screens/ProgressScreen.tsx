import { useCallback, useEffect, useState } from 'react';
import { ScrollView, StyleSheet, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import {
  Body,
  Caption,
  Card,
  CardTitle,
  EmptyState,
  Numeric,
  Screen,
  SectionHeader,
} from '@/components/ui';
import { CASE_CATALOG, catalogCase } from '@/cases/catalog';
import { attemptStore } from '@/storage/asyncStorageKv';
import type { CaseProgress, StoredAttempt } from '@/storage/attemptStore';
import { analytics } from '@/analytics';
import { spacing } from '@/theme/tokens';
import type { TabScreenProps } from '@/navigation/types';

/**
 * Progress: persisted-attempt mastery per case + recent attempts. Deterministic
 * facts only — no streaks, XP or badges.
 */
export function ProgressScreen({ navigation }: TabScreenProps<'Progress'>) {
  const [progress, setProgress] = useState<CaseProgress[]>([]);
  const [recent, setRecent] = useState<StoredAttempt[]>([]);

  useEffect(() => {
    analytics.track({ event: 'surface_viewed', surface: 'progress' });
  }, []);

  useFocusEffect(
    useCallback(() => {
      let active = true;
      Promise.all(CASE_CATALOG.map((c) => attemptStore.progressForCase(c.manifest.id))).then(
        (all) => active && setProgress(all),
      );
      attemptStore.listAll().then((all) => active && setRecent(all.slice(-5).reverse()));
      return () => {
        active = false;
      };
    }, []),
  );

  const totalAttempts = progress.reduce((s, p) => s + p.attempts, 0);
  const completed = progress.filter((p) => p.completed).length;
  const attempted = progress.filter((p) => p.attempts > 0);

  if (totalAttempts === 0) {
    return (
      <Screen>
        <EmptyState
          title="You haven't completed a case yet."
          hint="Play a case from the library — your attempts, scores and clinical timelines will be tracked here."
        />
      </Screen>
    );
  }

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <View style={styles.metricsRow}>
          <Card>
            <Numeric>
              {completed}/{CASE_CATALOG.length}
            </Numeric>
            <Caption>cases completed</Caption>
          </Card>
          <Card>
            <Numeric>{totalAttempts}</Numeric>
            <Caption>total attempts</Caption>
          </Card>
        </View>

        <SectionHeader>By case</SectionHeader>
        {attempted.map((p) => {
          const c = catalogCase(p.caseId);
          if (!c) return null;
          return (
            <Card
              key={p.caseId}
              onPress={() =>
                navigation.navigate('CaseDetail', {
                  caseId: c.manifest.id,
                  caseVersion: c.manifest.version,
                  title: c.manifest.title,
                })
              }
            >
              <CardTitle>{c.manifest.title}</CardTitle>
              <Body muted>
                {p.completed ? 'Completed' : 'Attempted'} · best {p.bestScore} pts · last{' '}
                {p.lastScore} pts · {p.attempts} {p.attempts === 1 ? 'attempt' : 'attempts'}
              </Body>
              {p.lastAttemptedAt ? (
                <Caption>Last played {new Date(p.lastAttemptedAt).toLocaleDateString()}</Caption>
              ) : null}
              <Body muted>Replay ›</Body>
            </Card>
          );
        })}

        <SectionHeader>Recent attempts</SectionHeader>
        {recent.map((a) => {
          const c = catalogCase(a.summary.caseId);
          return (
            <Card key={a.summary.attemptId}>
              <Body>
                {c?.manifest.title ?? a.summary.caseId} — {a.summary.totalScore} pts
              </Body>
              <Body muted>
                {a.summary.terminalState} · {new Date(a.summary.completedAt).toLocaleString()}
              </Body>
            </Card>
          );
        })}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { gap: spacing.md, paddingBottom: spacing.xl },
  metricsRow: {
    flexDirection: 'row',
    gap: spacing.md,
    // both metric cards share the row equally
    justifyContent: 'space-between',
  },
});
