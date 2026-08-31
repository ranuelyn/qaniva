import { useEffect, useRef, useState } from 'react';
import { Dimensions, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { PrimaryButton, Wordmark } from '@/components/ui';
import { colors, spacing, typography } from '@/theme/tokens';
import { markOnboardingCompleted } from '@/storage/appPrefs';
import { analytics } from '@/analytics';
import type { ScreenProps } from '@/navigation/types';

/**
 * First-launch onboarding: the product concept in four pages, nothing more.
 * No unsupported claims. Completion is persisted; the flow never reappears.
 */
const PAGES = [
  {
    title: 'Clinical decision simulation',
    body: 'Qaniva puts you in front of a dynamic 3D patient. What happens next is decided by your clinical decisions — not a script.',
  },
  {
    title: 'Assess, investigate, treat',
    body: 'Take the history, examine, order investigations and give treatments — the same way you would think at the bedside.',
  },
  {
    title: 'Timing and order matter',
    body: 'The patient state evolves with the simulated clock. A correct action done late is not the same as a correct action done on time.',
  },
  {
    title: 'Review every decision',
    body: 'After each case you get a deterministic debrief: your clinical timeline, what was on time, delayed or missed — with the evidence behind the scoring.',
  },
];

const { width } = Dimensions.get('window');

export function OnboardingScreen({ navigation, route }: ScreenProps<'Onboarding'>) {
  // Capture/E2E aid: qaniva://onboarding?complete=1 invokes the SAME finish()
  // handler the "Get started" button calls (persist + replace) — no state
  // shortcuts. Unused in normal flows.
  const autoComplete = (route.params as { complete?: string } | undefined)?.complete === '1';
  const insets = useSafeAreaInsets();
  const [page, setPage] = useState(0);
  const listRef = useRef<FlatList>(null);

  useEffect(() => {
    analytics.track({ event: 'onboarding_viewed' });
  }, []);

  useEffect(() => {
    if (autoComplete) finish();
    // finish is stable for this screen's lifetime; deps limited intentionally.
  }, [autoComplete]); // eslint-disable-line

  // Deep-link capture support (qaniva://onboarding?page=N) — scrolls the REAL
  // pager to the requested page; unused in normal flows.
  useEffect(() => {
    const target = route.params?.page;
    if (typeof target === 'number' && target >= 0 && target < PAGES.length) {
      setTimeout(() => listRef.current?.scrollToIndex({ index: target, animated: false }), 50);
    }
  }, [route.params?.page]);

  function finish() {
    analytics.track({ event: 'onboarding_completed' });
    void markOnboardingCompleted();
    navigation.replace('Tabs');
  }

  const isLast = page === PAGES.length - 1;

  return (
    <View
      style={[
        styles.root,
        { paddingTop: insets.top + spacing.xl, paddingBottom: insets.bottom + spacing.lg },
      ]}
    >
      <View style={styles.header}>
        <Wordmark />
        {!isLast && (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Skip"
            onPress={finish}
            hitSlop={12}
          >
            <Text style={styles.skip}>Skip</Text>
          </Pressable>
        )}
      </View>

      <FlatList
        ref={listRef}
        data={PAGES}
        keyExtractor={(p) => p.title}
        horizontal
        pagingEnabled
        showsHorizontalScrollIndicator={false}
        onMomentumScrollEnd={(e) => setPage(Math.round(e.nativeEvent.contentOffset.x / width))}
        getItemLayout={(_, i) => ({ length: width, offset: width * i, index: i })}
        renderItem={({ item, index }) => (
          <View style={styles.page}>
            <Text style={styles.pageNumber}>{`0${index + 1}`}</Text>
            <Text style={styles.pageTitle}>{item.title}</Text>
            <Text style={styles.pageBody}>{item.body}</Text>
          </View>
        )}
      />

      <View style={styles.dots}>
        {PAGES.map((_, i) => (
          <View key={i} style={[styles.dot, i === page && styles.dotActive]} />
        ))}
      </View>

      <View style={styles.footer}>
        {isLast ? (
          <PrimaryButton label="Get started" onPress={finish} />
        ) : (
          <PrimaryButton
            label="Next"
            onPress={() => {
              const next = Math.min(page + 1, PAGES.length - 1);
              listRef.current?.scrollToIndex({ index: next, animated: true });
              setPage(next);
            }}
          />
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.background },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: spacing.lg,
  },
  skip: { ...typography.button, color: colors.textMuted },
  page: { width, paddingHorizontal: spacing.lg, justifyContent: 'center', gap: spacing.md },
  pageNumber: { ...typography.caption, color: colors.brand, letterSpacing: 2 },
  pageTitle: { ...typography.display, color: colors.text },
  pageBody: { ...typography.body, fontSize: 17, lineHeight: 26, color: colors.textMuted },
  dots: {
    flexDirection: 'row',
    justifyContent: 'center',
    gap: spacing.sm,
    marginBottom: spacing.lg,
  },
  dot: { width: 8, height: 8, borderRadius: 4, backgroundColor: colors.border },
  dotActive: { backgroundColor: colors.brand, width: 20 },
  footer: { paddingHorizontal: spacing.lg },
});
