import { useCallback, useEffect, useRef, useState } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import {
  Body,
  Caption,
  Card,
  CardTitle,
  SecondaryButton,
  SectionHeader,
  Wordmark,
} from '@/components/ui';
import { CASE_CATALOG, catalogCase, specialtyLabel } from '@/cases/catalog';
import { attemptStore } from '@/storage/asyncStorageKv';
import type { CaseProgress, StoredAttempt } from '@/storage/attemptStore';
import { analytics } from '@/analytics';
import { cryptoRandomId } from '@/lib/ids';
import { colors, spacing, typography } from '@/theme/tokens';
import { CaseCard } from '@/components/CaseCard';
import type { TabScreenProps } from '@/navigation/types';

/**
 * Dev/E2E convenience: when EXPO_PUBLIC_E2E_AUTOSTART is set to a caseId at
 * bundle time, Home navigates through the REAL Case Briefing screen and then
 * into that simulation. Unset (every normal build/run), none of this executes.
 */
const E2E_AUTOSTART_CASE = process.env.EXPO_PUBLIC_E2E_AUTOSTART ?? '';
const E2E_MODE =
  process.env.EXPO_PUBLIC_E2E_MODE === 'autoplay'
    ? 'e2e_autoplay'
    : process.env.EXPO_PUBLIC_E2E_MODE === 'interactive'
      ? 'interactive'
      : 'e2e_ui';
const E2E_MAX_RUNS = 2;
const E2E_BRIEFING_DWELL_MS = 4000;

export function HomeScreen({ navigation }: TabScreenProps<'Home'>) {
  const insets = useSafeAreaInsets();
  const [latest, setLatest] = useState<StoredAttempt | null>(null);
  const [progress, setProgress] = useState<CaseProgress[]>([]);

  useEffect(() => {
    analytics.track({ event: 'app_open' });
    analytics.track({ event: 'surface_viewed', surface: 'home' });
  }, []);

  useFocusEffect(
    useCallback(() => {
      let active = true;
      attemptStore.latestAttempt().then((a) => active && setLatest(a));
      Promise.all(CASE_CATALOG.map((c) => attemptStore.progressForCase(c.manifest.id))).then(
        (all) => active && setProgress(all),
      );
      return () => {
        active = false;
      };
    }, []),
  );

  const runs = useRef(0);
  useEffect(() => {
    if (!E2E_AUTOSTART_CASE) return;
    const timers: ReturnType<typeof setTimeout>[] = [];
    const kick = () => {
      if (runs.current >= E2E_MAX_RUNS) return;
      runs.current += 1;
      const run = runs.current;
      navigation.navigate('CaseDetail', {
        caseId: E2E_AUTOSTART_CASE,
        caseVersion: 1,
        title: `E2E run ${run}`,
      });
      timers.push(
        setTimeout(() => {
          navigation.navigate('Simulation', {
            caseId: E2E_AUTOSTART_CASE,
            caseVersion: 1,
            attemptId: cryptoRandomId(), // fresh per run — never collides across cases
            seed: 20260830,
            title: `E2E run ${run}`,
            mode: E2E_MODE,
          });
        }, E2E_BRIEFING_DWELL_MS),
      );
    };
    const unsubscribe = navigation.addListener('focus', kick);
    return () => {
      unsubscribe();
      timers.forEach(clearTimeout);
    };
  }, [navigation]);

  const completedCount = progress.filter((p) => p.completed).length;
  const totalAttempts = progress.reduce((sum, p) => sum + p.attempts, 0);
  const latestCase = latest ? catalogCase(latest.summary.caseId) : undefined;
  const firstCase = CASE_CATALOG[0]!;

  return (
    <ScrollView
      style={styles.root}
      contentContainerStyle={[styles.content, { paddingTop: insets.top + spacing.lg }]}
    >
      <View style={styles.header}>
        <Wordmark />
        <Caption>Clinical decision simulation</Caption>
      </View>

      <SectionHeader>Continue</SectionHeader>
      {latest && latestCase ? (
        <Card
          onPress={() =>
            navigation.navigate('CaseDetail', {
              caseId: latestCase.manifest.id,
              caseVersion: latestCase.manifest.version,
              title: latestCase.manifest.title,
            })
          }
        >
          <CardTitle>{latestCase.manifest.title}</CardTitle>
          <Body muted>
            Last attempt: {latest.summary.totalScore} pts · {latest.summary.terminalState}
          </Body>
          <View style={styles.continueRow}>
            <Text style={styles.continueCta}>Replay this case ›</Text>
          </View>
        </Card>
      ) : (
        <Card
          onPress={() => {
            navigation.navigate('CaseDetail', {
              caseId: firstCase.manifest.id,
              caseVersion: firstCase.manifest.version,
              title: firstCase.manifest.title,
            });
          }}
        >
          <Caption>YOUR FIRST SIMULATION</Caption>
          <CardTitle>{firstCase.manifest.title}</CardTitle>
          <Body muted>
            Start with a focused emergency case and review every decision afterward.
          </Body>
          <Text style={styles.continueCta}>Start first case ›</Text>
        </Card>
      )}

      <View style={styles.sectionRow}>
        <SectionHeader>Cases</SectionHeader>
        <Text
          style={styles.sectionLink}
          onPress={() => navigation.navigate('Tabs', { screen: 'Cases' })}
        >
          View all ›
        </Text>
      </View>
      {CASE_CATALOG.slice(0, 2).map((c, index) => {
        const p = progress.find((x) => x.caseId === c.manifest.id);
        return (
          <CaseCard
            key={c.manifest.id}
            index={index}
            compact
            title={c.manifest.title}
            teaser={c.teaser}
            specialty={specialtyLabel(c.manifest.specialty)}
            minutes={c.manifest.estimatedMinutes}
            progress={p}
            onPress={() =>
              navigation.navigate('CaseDetail', {
                caseId: c.manifest.id,
                caseVersion: c.manifest.version,
                title: c.manifest.title,
              })
            }
          />
        );
      })}

      <SectionHeader>Your progress</SectionHeader>
      {totalAttempts > 0 ? (
        <Card onPress={() => navigation.navigate('Tabs', { screen: 'Progress' })}>
          <Body>
            {completedCount} of {CASE_CATALOG.length} cases completed · {totalAttempts}{' '}
            {totalAttempts === 1 ? 'attempt' : 'attempts'}
          </Body>
          <Body muted>See your attempt history and best scores ›</Body>
        </Card>
      ) : (
        <Body muted>Your scores and clinical timelines will appear after your first case.</Body>
      )}
      <SecondaryButton
        label="Educational use & clinical status"
        onPress={() => navigation.navigate('Disclaimer')}
      />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.background },
  content: { padding: spacing.lg, gap: spacing.md, paddingBottom: spacing.xl },
  header: { gap: 2, marginBottom: spacing.xs },
  sectionRow: { flexDirection: 'row', alignItems: 'baseline', justifyContent: 'space-between' },
  sectionLink: { ...typography.caption, color: colors.brand },
  continueRow: { marginTop: spacing.xs },
  continueCta: { ...typography.button, color: colors.brand },
});
