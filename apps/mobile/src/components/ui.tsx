import type { ReactNode } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, radius, spacing, typography } from '@/theme/tokens';

export function Screen({ children }: { children: ReactNode }) {
  return <View style={styles.screen}>{children}</View>;
}

export function Card({ children, onPress }: { children: ReactNode; onPress?: () => void }) {
  const Wrapper = onPress ? Pressable : View;
  return (
    <Wrapper
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
      style={({ pressed }) => [styles.button, pressed && styles.buttonPressed]}
      onPress={onPress}
    >
      <Text style={styles.buttonLabel}>{label}</Text>
    </Pressable>
  );
}

export function Title({ children }: { children: ReactNode }) {
  return <Text style={styles.title}>{children}</Text>;
}

export function Body({ children, muted }: { children: ReactNode; muted?: boolean }) {
  return <Text style={[styles.body, muted && styles.muted]}>{children}</Text>;
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
    backgroundColor: colors.primary,
    borderRadius: radius.md,
    paddingVertical: spacing.md,
    alignItems: 'center',
  },
  buttonPressed: { opacity: 0.85 },
  buttonLabel: { ...typography.heading, color: colors.primaryText },
  title: { ...typography.title, color: colors.text },
  body: { ...typography.body, color: colors.text },
  muted: { color: colors.textMuted },
});
