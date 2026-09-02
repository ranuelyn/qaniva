import { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import type { SimulationMode } from '@qaniva/contracts';
import { Body, Caption, PrimaryButton, Screen, Wordmark } from '@/components/ui';
import { useUnitySimulation } from '@/unity/useUnitySimulation';
import { analytics } from '@/analytics';
import { catalogCase } from '@/cases/catalog';
import { cryptoRandomId, randomSeed } from '@/lib/ids';
import { colors, radius, spacing, typography } from '@/theme/tokens';
import type { ScreenProps } from '@/navigation/types';

/** E2E capture mode: an aborted run returns to Home so the run loop continues. */
const E2E_MODE = Boolean(process.env.EXPO_PUBLIC_E2E_AUTOSTART);

/** Friendly wording per bridge failure code; the raw detail stays behind a disclosure. */
const FAILURE_MESSAGES: Record<string, { title: string; body: string }> = {
  CASE_LOAD_FAILED: {
    title: 'Bu vaka şu anda kullanılamıyor',
    body: 'Vaka içeriği bu cihaza yüklenemedi. Geri dönüp tekrar açmak genellikle sorunu çözer.',
  },
  BRIDGE_PROTOCOL_ERROR: {
    title: 'Simülasyonda bir sorun oluştu',
    body: 'Simülasyon çalışma ortamı bir iletişim sorunu bildirdi. Lütfen vakadan çıkıp tekrar deneyin.',
  },
  UNKNOWN: {
    title: 'Simülasyon başlatılamadı',
    body: 'Bu vaka hazırlanırken bir sorun oluştu. Lütfen geri dönüp tekrar deneyin.',
  },
};

/**
 * Host for the full-screen Unity simulation. On a Unity-embedded build the Unity
 * window covers this screen while the simulation runs; this RN view is what shows
 * before READY and after EXIT/COMPLETED. Mode is 'interactive' for every user
 * launch; E2E launches (HomeScreen autostart) may pass an e2e mode.
 */
export function SimulationScreen({ navigation, route }: ScreenProps<'Simulation'>) {
  // The production deep link (qaniva://simulate/:caseId) may arrive without the
  // attempt identity the in-app path always supplies — mint it once here so the
  // bridge contract (uuid attemptId, numeric seed/version) always holds.
  const minted = useRef({ attemptId: cryptoRandomId(), seed: randomSeed() });
  const { caseId, mode } = route.params;
  const catalogEntry = catalogCase(caseId);
  const caseVersion = route.params.caseVersion ?? catalogEntry?.manifest.version ?? 1;
  const attemptId = route.params.attemptId ?? minted.current.attemptId;
  const seed = route.params.seed ?? minted.current.seed;
  const title = route.params.title || (catalogEntry?.manifest.title ?? caseId);
  const sim = useUnitySimulation();
  const insets = useSafeAreaInsets();
  const [showDetails, setShowDetails] = useState(false);

  const { start } = sim;
  useEffect(() => {
    analytics.track({ event: 'case_start', caseId, caseVersion, attemptId });
    start({ caseId, caseVersion, attemptId, seed, mode: mode as SimulationMode | undefined });
  }, [start, caseId, caseVersion, attemptId, seed, mode]);

  const trackedEnd = useRef(false);
  useEffect(() => {
    if (sim.phase === 'completed' && sim.summary) {
      if (!trackedEnd.current) {
        trackedEnd.current = true;
        analytics.track({
          event: 'case_complete',
          caseId,
          caseVersion,
          attemptId,
          terminalOutcome: sim.summary.terminalState,
          totalScore: sim.summary.totalScore,
          durationRealSec: 0, // real-time duration is derivable from the summary timestamps
        });
      }
      navigation.replace('Results', { caseId, title, summary: sim.summary });
    } else if (sim.phase === 'exited') {
      // User aborted inside Unity (EXIT_REQUESTED): no results for an aborted
      // attempt — return to where they came from.
      if (!trackedEnd.current) {
        trackedEnd.current = true;
        analytics.track({ event: 'case_abort', caseId, attemptId });
      }
      if (E2E_MODE) {
        navigation.popToTop();
      } else {
        navigation.goBack();
      }
    }
  }, [sim.phase, sim.summary, navigation, caseId, caseVersion, attemptId, title]);

  const unknownFailure = FAILURE_MESSAGES.UNKNOWN!;
  const failure = FAILURE_MESSAGES[sim.error?.split(':')[0] ?? 'UNKNOWN'] ?? unknownFailure;

  return (
    <Screen
      style={{ paddingTop: insets.top + spacing.lg, paddingBottom: insets.bottom + spacing.lg }}
    >
      <Wordmark compact />
      {sim.phase === 'failed' ? (
        <>
          <View style={styles.center}>
            <View style={styles.iconBadge}>
              <Ionicons name="cloud-offline-outline" size={30} color={colors.textMuted} />
            </View>
            <Text style={styles.failureTitle}>{failure.title}</Text>
            <Text style={styles.failureBody}>{failure.body}</Text>
            <Pressable
              accessibilityRole="button"
              style={styles.detailsToggle}
              onPress={() => setShowDetails((v) => !v)}
            >
              <Caption>{showDetails ? 'Teknik ayrıntıları gizle' : 'Teknik ayrıntılar'}</Caption>
            </Pressable>
            {showDetails ? (
              <View style={styles.detailsBlock}>
                <Caption>{sim.error}</Caption>
              </View>
            ) : null}
          </View>
          <PrimaryButton label="Vakalara dön" onPress={() => navigation.popToTop()} />
        </>
      ) : (
        <View style={styles.center}>
          <ActivityIndicator color={colors.brand} />
          <Body muted>
            {sim.phase === 'starting' && 'Simülasyon hazırlanıyor…'}
            {sim.phase === 'ready' && 'Simülasyona giriliyor…'}
            {sim.phase === 'idle' && 'Simülasyon hazırlanıyor…'}
            {sim.phase === 'exited' && 'Simülasyondan çıkılıyor…'}
          </Body>
          <Caption>{title}</Caption>
          {sim.transportKind === 'fake' && (
            <Body muted>
              ⚠ Simülasyon çalışma ortamı olmayan geliştirme sürümü — sonuçlar simüle edilmiş yer
              tutuculardır.
            </Body>
          )}
        </View>
      )}
    </Screen>
  );
}

const styles = StyleSheet.create({
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.md,
    paddingHorizontal: spacing.md,
  },
  iconBadge: {
    width: 64,
    height: 64,
    borderRadius: 32,
    backgroundColor: colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.xs,
  },
  failureTitle: {
    ...typography.screenTitle,
    fontSize: 22,
    color: colors.text,
    textAlign: 'center',
  },
  failureBody: {
    ...typography.body,
    color: colors.textMuted,
    textAlign: 'center',
    maxWidth: 320,
  },
  detailsToggle: { minHeight: 32, justifyContent: 'center' },
  detailsBlock: {
    backgroundColor: colors.surface,
    borderRadius: radius.sm,
    padding: spacing.md,
    maxWidth: 340,
  },
});
