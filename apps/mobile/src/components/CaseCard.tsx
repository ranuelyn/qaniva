import { StyleSheet, Text, View } from 'react-native';
import { BadgeRow, Card, CardTitle, StatusBadge } from '@/components/ui';
import { colors, spacing, typography } from '@/theme/tokens';
import type { CaseProgress } from '@/storage/attemptStore';

/**
 * The one case card used everywhere (Home, Cases). Entirely metadata-driven:
 * a third case renders with zero shell changes. Shows the authored teaser
 * (chief complaint) — never a diagnosis.
 */
export function CaseCard({
  title,
  teaser,
  specialty,
  minutes,
  progress,
  onPress,
  index,
  compact,
}: {
  title: string;
  teaser: string;
  specialty: string;
  minutes: number;
  progress?: CaseProgress;
  onPress: () => void;
  index?: number;
  compact?: boolean;
}) {
  return (
    <Card onPress={onPress}>
      <View style={styles.titleRow}>
        {typeof index === 'number' ? (
          <Text style={styles.index}>{String(index + 1).padStart(2, '0')}</Text>
        ) : null}
        <View style={styles.titleCopy}>
          <CardTitle>{title}</CardTitle>
          <Text style={styles.meta}>
            {specialty} · ~{minutes} dk
          </Text>
        </View>
      </View>
      {!compact ? <Text style={styles.teaser}>“{teaser}”</Text> : null}
      <View style={styles.footerRow}>
        <BadgeRow>
          {progress?.completed ? (
            <StatusBadge label={`En iyi ${progress.bestScore} puan`} tone="success" />
          ) : progress && progress.attempts > 0 ? (
            <StatusBadge label={`${progress.attempts} deneme`} tone="warning" />
          ) : (
            <StatusBadge label="Yeni vaka" tone="neutral" />
          )}
        </BadgeRow>
        <Text style={styles.cta}>
          {progress && progress.attempts > 0 ? 'Tekrar oyna ›' : 'Başla ›'}
        </Text>
      </View>
    </Card>
  );
}

const styles = StyleSheet.create({
  titleRow: { flexDirection: 'row', gap: spacing.sm, alignItems: 'flex-start' },
  titleCopy: { flex: 1, gap: spacing.xs },
  index: { ...typography.caption, color: colors.brand, letterSpacing: 1 },
  teaser: { ...typography.bodySecondary, color: colors.textMuted, fontStyle: 'italic' },
  meta: { ...typography.caption, color: colors.textFaint },
  footerRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: spacing.xs,
  },
  cta: { ...typography.button, fontSize: 14, color: colors.brand },
});
