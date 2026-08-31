import { useEffect } from 'react';
import { ScrollView, StyleSheet } from 'react-native';
import { Body, Card, Screen, SectionHeader } from '@/components/ui';
import { analytics } from '@/analytics';
import { spacing } from '@/theme/tokens';
import type { ScreenProps } from '@/navigation/types';

/** Readable, non-buried educational boundary. */
export function DisclaimerScreen(_props: ScreenProps<'Disclaimer'>) {
  useEffect(() => {
    analytics.track({ event: 'surface_viewed', surface: 'disclaimer' });
  }, []);

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <SectionHeader>Educational simulation only</SectionHeader>
        <Card>
          <Body>
            Qaniva is an educational simulation. It is not intended for the diagnosis or treatment
            of real patients, and it must not be used to guide real patient care.
          </Body>
        </Card>

        <Card>
          <Body>
            Nothing in Qaniva replaces clinical judgment, supervision, or your institution's
            protocols and guidelines. Where Qaniva and your local protocol differ, follow your
            protocol.
          </Body>
        </Card>

        <SectionHeader>Clinical content status</SectionHeader>
        <Card>
          <Body>
            Cases in this MVP build are fictional, evidence-referenced teaching scenarios. Their
            clinical content is currently awaiting formal physician review — it should be treated as
            draft educational material, not validated clinical guidance.
          </Body>
        </Card>

        <SectionHeader>Your data</SectionHeader>
        <Card>
          <Body>
            All patients in Qaniva are fictional. Do not enter real patient data anywhere in the
            app. Your attempts and scores are stored only on this device in the MVP.
          </Body>
        </Card>
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { gap: spacing.md, paddingBottom: spacing.xl },
});
