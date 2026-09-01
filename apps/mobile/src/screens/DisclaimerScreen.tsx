import { useEffect } from 'react';
import { ScrollView, StyleSheet, View } from 'react-native';
import { Body, Eyebrow, Screen, SectionHeader } from '@/components/ui';
import { analytics } from '@/analytics';
import { colors, spacing } from '@/theme/tokens';
import type { ScreenProps } from '@/navigation/types';

/** Readable, non-buried educational boundary. */
export function DisclaimerScreen(_props: ScreenProps<'Disclaimer'>) {
  useEffect(() => {
    analytics.track({ event: 'surface_viewed', surface: 'disclaimer' });
  }, []);

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <View style={styles.primaryNotice}>
          <Eyebrow>Educational simulation only</Eyebrow>
          <Body>
            Qaniva is an educational simulation. It is not intended for the diagnosis or treatment
            of real patients, and it must not be used to guide real patient care.
          </Body>
        </View>

        <View style={styles.copyBlock}>
          <Body>
            Nothing in Qaniva replaces clinical judgment, supervision, or your institution's
            protocols and guidelines. Where Qaniva and your local protocol differ, follow your
            protocol.
          </Body>
        </View>

        <SectionHeader>Clinical content status</SectionHeader>
        <View style={styles.copyBlock}>
          <Body>
            Cases in this MVP build are fictional, evidence-referenced teaching scenarios. Their
            clinical content is currently awaiting formal physician review — it should be treated as
            draft educational material, not validated clinical guidance.
          </Body>
        </View>

        <SectionHeader>Your data</SectionHeader>
        <View style={styles.copyBlock}>
          <Body>
            All patients in Qaniva are fictional. Do not enter real patient data anywhere in the
            app. Your attempts and scores are stored only on this device in the MVP.
          </Body>
        </View>
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { gap: spacing.md, paddingBottom: spacing.xl },
  primaryNotice: {
    borderLeftWidth: 3,
    borderLeftColor: colors.warning,
    paddingLeft: spacing.md,
    gap: spacing.sm,
  },
  copyBlock: { paddingLeft: spacing.md, borderLeftWidth: 1, borderLeftColor: colors.border },
});
