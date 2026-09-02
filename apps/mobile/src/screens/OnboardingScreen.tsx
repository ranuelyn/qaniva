import { useEffect, useRef, useState, type ComponentProps } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View, useWindowDimensions } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { PrimaryButton, Wordmark } from '@/components/ui';
import { colors, radius, spacing, typography } from '@/theme/tokens';
import { markOnboardingCompleted } from '@/storage/appPrefs';
import { analytics } from '@/analytics';
import type { ScreenProps } from '@/navigation/types';

/**
 * First-launch onboarding: the product concept in four pages, nothing more.
 * No unsupported claims. Completion is persisted; the flow never reappears.
 */
type OnboardingPage = {
  title: string;
  body: string;
  icon: ComponentProps<typeof Ionicons>['name'];
  steps: string[];
};

const PAGES: OnboardingPage[] = [
  {
    title: 'Klinik karar simülasyonu',
    body: 'Qaniva seni dinamik bir 3B hastanın karşısına koyar. Sonra ne olacağına bir senaryo değil, senin klinik kararların karar verir.',
    icon: 'body-outline' as const,
    steps: ['Hasta', 'Karar', 'Yanıt'],
  },
  {
    title: 'Değerlendir, araştır, tedavi et',
    body: 'Öykü al, muayene et, tetkik iste ve tedavi ver — hasta başında düşündüğün gibi.',
    icon: 'medkit-outline' as const,
    steps: ['Değerlendir', 'Araştır', 'Tedavi et'],
  },
  {
    title: 'Zamanlama ve sıra önemlidir',
    body: 'Hastanın durumu simüle edilen saatle birlikte değişir. Geç yapılan doğru bir eylem, zamanında yapılan doğru eylemle aynı değildir.',
    icon: 'time-outline' as const,
    steps: ['00:00', 'Eylem', 'Durum değişimi'],
  },
  {
    title: 'Her kararı gözden geçir',
    body: 'Her vakadan sonra deterministik bir değerlendirme alırsın: klinik zaman çizelgen, neyin zamanında, neyin gecikmiş ya da kaçırılmış olduğu — puanlamanın arkasındaki kanıtlarla.',
    icon: 'git-compare-outline' as const,
    steps: ['Zaman çizelgesi', 'Neden', 'Kanıt'],
  },
];

export function OnboardingScreen({ navigation, route }: ScreenProps<'Onboarding'>) {
  // Capture/E2E aid: qaniva://onboarding?complete=1 invokes the SAME finish()
  // handler the "Get started" button calls (persist + replace) — no state
  // shortcuts. Unused in normal flows.
  const autoComplete = (route.params as { complete?: string } | undefined)?.complete === '1';
  const insets = useSafeAreaInsets();
  const { width } = useWindowDimensions();
  const [page, setPage] = useState(0);
  const listRef = useRef<FlatList<OnboardingPage>>(null);

  useEffect(() => {
    analytics.track({ event: 'onboarding_viewed' });
  }, []);

  useEffect(() => {
    if (autoComplete) finish();
  }, [autoComplete]);

  // Deep-link capture support (qaniva://onboarding?page=N) — scrolls the REAL
  // pager to the requested page; unused in normal flows.
  useEffect(() => {
    const target = route.params?.page;
    if (typeof target === 'number' && target >= 0 && target < PAGES.length) {
      setPage(target);
      const timer = setTimeout(
        () => listRef.current?.scrollToOffset({ offset: width * target, animated: false }),
        100,
      );
      return () => clearTimeout(timer);
    }
    return undefined;
  }, [route.params?.page, width]);

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
            <Text style={styles.skip}>Geç</Text>
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
        onScrollToIndexFailed={({ index }) =>
          listRef.current?.scrollToOffset({ offset: width * index, animated: false })
        }
        getItemLayout={(_, i) => ({ length: width, offset: width * i, index: i })}
        renderItem={({ item, index }) => (
          <View style={[styles.page, { width }]}>
            <View style={styles.visual} accessibilityElementsHidden>
              <View style={styles.visualIcon}>
                <Ionicons name={item.icon} size={34} color={colors.brand} />
              </View>
              <View style={styles.visualFlow}>
                {item.steps.map((step: string, stepIndex: number) => (
                  <View key={step} style={styles.visualStepWrap}>
                    {stepIndex > 0 ? <View style={styles.visualConnector} /> : null}
                    <View style={styles.visualStep}>
                      <Text style={styles.visualStepIndex}>{stepIndex + 1}</Text>
                      <Text style={styles.visualStepLabel}>{step}</Text>
                    </View>
                  </View>
                ))}
              </View>
            </View>
            <View style={styles.copy}>
              <Text style={styles.pageNumber}>{`0${index + 1}`}</Text>
              <Text style={styles.pageTitle}>{item.title}</Text>
              <Text style={styles.pageBody}>{item.body}</Text>
            </View>
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
          <PrimaryButton label="Başla" onPress={finish} />
        ) : (
          <PrimaryButton
            label="İleri"
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
  page: { paddingHorizontal: spacing.lg, justifyContent: 'center', gap: spacing.xl },
  visual: {
    minHeight: 176,
    backgroundColor: colors.surface,
    borderColor: colors.border,
    borderWidth: 1,
    borderRadius: radius.lg,
    padding: spacing.lg,
    justifyContent: 'space-between',
    overflow: 'hidden',
  },
  visualIcon: {
    width: 58,
    height: 58,
    borderRadius: radius.md,
    backgroundColor: colors.surfaceAlt,
    alignItems: 'center',
    justifyContent: 'center',
  },
  visualFlow: { flexDirection: 'row', alignItems: 'center' },
  visualStepWrap: { flex: 1, flexDirection: 'row', alignItems: 'center' },
  visualConnector: { width: spacing.md, height: 1, backgroundColor: colors.brandDim },
  visualStep: { flex: 1, gap: spacing.xs },
  visualStepIndex: { ...typography.caption, color: colors.brand },
  visualStepLabel: { ...typography.caption, color: colors.text, flexShrink: 1 },
  copy: { gap: spacing.md },
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
