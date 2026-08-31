import type { ReactNode } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, radius, sizes, spacing, tones, typography, type Tone } from '@/theme/tokens';

/**
 * Qaniva shell components. Every screen composes these — no per-screen hex
 * values, font sizes or radii. Clinical meaning is never color-only: badges
 * and status rows always carry text.
 */

export function Screen({ children }: { children: ReactNode }) {
  return <View style={styles.screen}>{children}</View>;
}

export function Card({ children, onPress }: { children: ReactNode; onPress?: () => void }) {
  const Wrapper = onPress ? Pressable : View;
  return (
    <Wrapper
      accessibilityRole={onPress ? 'button' : undefined}
      style={({ pressed }: { pressed?: boolean }) => [styles.card, pressed && styles.cardPressed]}
      onPress={onPress}
    >
      {children}
    </Wrapper>
  );
}

export function PrimaryButton({ label, onPress }: { label: string; onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      style={({ pressed }) => [styles.button, pressed && styles.buttonPressed]}
      onPress={onPress}
    >
      <Text style={styles.buttonLabel}>{label}</Text>
    </Pressable>
  );
}

export function SecondaryButton({ label, onPress }: { label: string; onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      style={({ pressed }) => [styles.buttonSecondary, pressed && styles.buttonPressed]}
      onPress={onPress}
    >
      <Text style={styles.buttonSecondaryLabel}>{label}</Text>
    </Pressable>
  );
}

export function Title({ children }: { children: ReactNode }) {
  return <Text style={styles.title}>{children}</Text>;
}

export function SectionHeader({ children }: { children: ReactNode }) {
  return <Text style={styles.sectionHeader}>{children}</Text>;
}

export function CardTitle({ children }: { children: ReactNode }) {
  return <Text style={styles.cardTitle}>{children}</Text>;
}

export function Body({ children, muted }: { children: ReactNode; muted?: boolean }) {
  return <Text style={[styles.body, muted && styles.muted]}>{children}</Text>;
}

export function Caption({ children }: { children: ReactNode }) {
  return <Text style={styles.caption}>{children}</Text>;
}

/** Big deterministic number (scores, vitals-style emphasis). */
export function Numeric({ children }: { children: ReactNode }) {
  return <Text style={styles.numeric}>{children}</Text>;
}

/** Labeled status chip. Tone tints the accent; the LABEL carries the meaning. */
export function StatusBadge({ label, tone = 'neutral' }: { label: string; tone?: Tone }) {
  const color = tones[tone].color;
  return (
    <View style={[styles.badge, { borderColor: color }]}>
      <View style={[styles.badgeDot, { backgroundColor: color }]} />
      <Text style={[styles.badgeLabel, { color }]}>{label}</Text>
    </View>
  );
}

export function BadgeRow({ children }: { children: ReactNode }) {
  return <View style={styles.badgeRow}>{children}</View>;
}

export function EmptyState({ title, hint }: { title: string; hint?: string }) {
  return (
    <View style={styles.empty}>
      <Text style={styles.emptyTitle}>{title}</Text>
      {hint ? <Text style={styles.emptyHint}>{hint}</Text> : null}
    </View>
  );
}

export function SettingsRow({
  label,
  detail,
  onPress,
  destructive,
}: {
  label: string;
  detail?: string;
  onPress?: () => void;
  destructive?: boolean;
}) {
  const Wrapper = onPress ? Pressable : View;
  return (
    <Wrapper
      accessibilityRole={onPress ? 'button' : undefined}
      accessibilityLabel={label}
      style={({ pressed }: { pressed?: boolean }) => [
        styles.settingsRow,
        pressed && styles.cardPressed,
      ]}
      onPress={onPress}
    >
      <Text style={[styles.settingsLabel, destructive && styles.destructive]}>{label}</Text>
      <View style={styles.settingsRight}>
        {detail ? <Text style={styles.settingsDetail}>{detail}</Text> : null}
        {onPress ? <Text style={styles.chevron}>›</Text> : null}
      </View>
    </Wrapper>
  );
}

/** Compact Qaniva wordmark (provisional brand — text treatment, no logo asset yet). */
export function Wordmark({ compact }: { compact?: boolean }) {
  return (
    <Text style={[styles.wordmark, compact && styles.wordmarkCompact]}>
      Qaniva<Text style={styles.wordmarkDot}>.</Text>
    </Text>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background, padding: spacing.lg, gap: spacing.md },
  card: {
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    gap: spacing.xs,
  },
  cardPressed: { backgroundColor: colors.surfaceAlt },
  button: {
    backgroundColor: colors.brand,
    borderRadius: radius.md,
    minHeight: sizes.buttonHeight,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: spacing.md,
  },
  buttonSecondary: {
    backgroundColor: colors.surfaceAlt,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    minHeight: sizes.buttonHeight,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: spacing.md,
  },
  buttonPressed: { opacity: 0.85 },
  buttonLabel: { ...typography.button, color: colors.brandText },
  buttonSecondaryLabel: { ...typography.button, color: colors.text },
  title: { ...typography.screenTitle, color: colors.text },
  sectionHeader: { ...typography.sectionTitle, color: colors.text, marginTop: spacing.sm },
  cardTitle: { ...typography.cardTitle, color: colors.text },
  body: { ...typography.body, color: colors.text },
  muted: { color: colors.textMuted },
  caption: { ...typography.caption, color: colors.textFaint },
  numeric: { ...typography.numeric, color: colors.text },
  badge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    borderWidth: 1,
    borderRadius: radius.pill,
    paddingHorizontal: spacing.sm + 2,
    paddingVertical: 3,
    alignSelf: 'flex-start',
  },
  badgeDot: { width: 6, height: 6, borderRadius: 3 },
  badgeLabel: { ...typography.caption },
  badgeRow: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm },
  empty: {
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.lg,
    alignItems: 'center',
    gap: spacing.xs,
  },
  emptyTitle: { ...typography.cardTitle, color: colors.text, textAlign: 'center' },
  emptyHint: { ...typography.bodySecondary, color: colors.textMuted, textAlign: 'center' },
  settingsRow: {
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.md,
    minHeight: sizes.touchTarget + 8,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  settingsLabel: { ...typography.body, color: colors.text },
  settingsRight: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm },
  settingsDetail: { ...typography.bodySecondary, color: colors.textMuted },
  chevron: { fontSize: 22, color: colors.textFaint, marginTop: -2 },
  destructive: { color: colors.danger },
  wordmark: { ...typography.display, color: colors.text },
  wordmarkCompact: { fontSize: 22, fontWeight: '800' },
  wordmarkDot: { color: colors.brand },
});
