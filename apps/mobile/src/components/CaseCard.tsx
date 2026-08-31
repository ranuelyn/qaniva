import { StyleSheet, Text, View } from 'react-native';
import { BadgeRow, Card, CardTitle, StatusBadge } from '@/components/ui';
import { colors, typography } from '@/theme/tokens';
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
}: {
  title: string;
  teaser: string;
  specialty: string;
  minutes: number;
  progress?: CaseProgress;
  onPress: () => void;
}) {
  return (
    <Card onPress={onPress}>
      <CardTitle>{title}</CardTitle>
      <Text style={styles.teaser}>“{teaser}”</Text>
      <Text style={styles.meta}>
        {specialty} · ~{minutes} min
      </Text>
      <BadgeRow>
        {progress?.completed ? (
          <StatusBadge label={`Completed · best ${progress.bestScore} pts`} tone="success" />
        ) : progress && progress.attempts > 0 ? (
          <StatusBadge label={`Attempted · ${progress.attempts}×`} tone="warning" />
        ) : (
          <StatusBadge label="Not attempted" tone="neutral" />
        )}
      </BadgeRow>
      <View style={styles.ctaRow}>
        <Text style={styles.cta}>
          {progress && progress.attempts > 0 ? 'Play again ›' : 'Start ›'}
        </Text>
      </View>
    </Card>
  );
}

const styles = StyleSheet.create({
  teaser: { ...typography.bodySecondary, color: colors.textMuted, fontStyle: 'italic' },
  meta: { ...typography.caption, color: colors.textFaint },
  ctaRow: { alignItems: 'flex-end' },
  cta: { ...typography.button, fontSize: 14, color: colors.brand },
});
