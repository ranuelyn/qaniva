import { useCallback, useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import {
  Body,
  Caption,
  Divider,
  Eyebrow,
  EmptyState,
  Numeric,
  Screen,
  SectionHeader,
} from '@/components/ui';
import { CASE_CATALOG, catalogCase } from '@/cases/catalog';
import { attemptStore } from '@/storage/asyncStorageKv';
import type { CaseProgress, StoredAttempt } from '@/storage/attemptStore';
import { analytics } from '@/analytics';
import { colors, radius, spacing, typography } from '@/theme/tokens';
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
        <View style={styles.metricsPanel}>
          <View style={styles.metric}>
            <Numeric>
              {completed}/{CASE_CATALOG.length}
            </Numeric>
            <Caption>cases completed</Caption>
          </View>
          <View style={styles.metricDivider} />
          <View style={styles.metric}>
            <Numeric>{totalAttempts}</Numeric>
            <Caption>total attempts</Caption>
          </View>
        </View>

        <SectionHeader>By case</SectionHeader>
        <View style={styles.group}>
          {attempted.map((p, index) => {
            const c = catalogCase(p.caseId);
            if (!c) return null;
            return (
              <View key={p.caseId}>
                {index > 0 ? <Divider /> : null}
                <Pressable
                  accessibilityRole="button"
                  style={({ pressed }) => [styles.caseRow, pressed && styles.rowPressed]}
                  onPress={() =>
                    navigation.navigate('CaseDetail', {
                      caseId: c.manifest.id,
                      caseVersion: c.manifest.version,
                      title: c.manifest.title,
                    })
                  }
                >
                  <View style={styles.rowCopy}>
                    <Text style={styles.rowTitle}>{c.manifest.title}</Text>
                    <Body muted>
                      {p.completed ? 'Completed' : 'Attempted'} · best {p.bestScore} pts ·{' '}
                      {p.attempts} {p.attempts === 1 ? 'attempt' : 'attempts'}
                    </Body>
                    {p.lastAttemptedAt ? (
                      <Caption>
                        Last played {new Date(p.lastAttemptedAt).toLocaleDateString()}
                      </Caption>
                    ) : null}
                  </View>
                  <Text style={styles.chevron}>›</Text>
                </Pressable>
              </View>
            );
          })}
        </View>

        <SectionHeader>Recent attempts</SectionHeader>
        <View style={styles.recentList}>
          {recent.map((a, index) => {
            const c = catalogCase(a.summary.caseId);
            return (
              <View key={a.summary.attemptId}>
                {index > 0 ? <Divider /> : null}
                <View style={styles.recentRow}>
                  <View style={styles.recentScore}>
                    <Eyebrow>Score</Eyebrow>
                    <Text style={styles.rowTitle}>{a.summary.totalScore}</Text>
                  </View>
                  <View style={styles.rowCopy}>
                    <Body>{c?.manifest.title ?? a.summary.caseId}</Body>
                    <Body muted>
                      {a.summary.terminalState} · {new Date(a.summary.completedAt).toLocaleString()}
                    </Body>
                  </View>
                </View>
              </View>
            );
          })}
        </View>
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { gap: spacing.md, paddingBottom: spacing.xl },
  metricsPanel: {
    flexDirection: 'row',
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    padding: spacing.md,
  },
  metric: { flex: 1, gap: spacing.xs },
  metricDivider: { width: 1, backgroundColor: colors.border, marginHorizontal: spacing.md },
  group: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    overflow: 'hidden',
  },
  caseRow: { flexDirection: 'row', alignItems: 'center', padding: spacing.md, gap: spacing.md },
  rowPressed: { backgroundColor: colors.surfaceAlt },
  rowCopy: { flex: 1, gap: spacing.xs },
  rowTitle: { ...typography.cardTitle, color: colors.text },
  chevron: { fontSize: 24, color: colors.textFaint },
  recentList: { borderTopWidth: 1, borderTopColor: colors.border },
  recentRow: { flexDirection: 'row', gap: spacing.md, paddingVertical: spacing.md },
  recentScore: { width: 52, gap: spacing.xs },
});
