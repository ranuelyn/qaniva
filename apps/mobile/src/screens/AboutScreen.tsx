import { useEffect } from 'react';
import { ScrollView, StyleSheet, View } from 'react-native';
import Constants from 'expo-constants';
import { Body, Caption, Eyebrow, Screen, SectionHeader, Wordmark } from '@/components/ui';
import { analytics } from '@/analytics';
import { colors, spacing } from '@/theme/tokens';
import type { ScreenProps } from '@/navigation/types';

export function AboutScreen(_props: ScreenProps<'About'>) {
  useEffect(() => {
    analytics.track({ event: 'surface_viewed', surface: 'about' });
  }, []);

  const version = Constants.expoConfig?.version ?? '0.0.0';

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <Wordmark />
        <Body>
          Qaniva is an interactive clinical decision simulation platform. Learners practice
          assessment, investigation, treatment and clinical reasoning on dynamic 3D patient cases —
          where timing, order and patient state shape the outcome, and every decision is reviewed in
          a deterministic, evidence-referenced debrief.
        </Body>

        <SectionHeader>How it works</SectionHeader>
        <View style={styles.callout}>
          <Eyebrow>Engine-owned truth</Eyebrow>
          <Body muted>
            Every case is a versioned, evidence-referenced definition executed by a deterministic
            clinical engine: the same decisions always produce the same outcome, timeline and score.
            Nothing in the simulation is improvised by AI.
          </Body>
        </View>

        <SectionHeader>Status</SectionHeader>
        <View style={styles.copyBlock}>
          <Body muted>
            This is an MVP build for internal and demonstration use. The clinical content is
            evidence-based and fictional, and is awaiting formal physician validation — see
            “Educational use & clinical status”.
          </Body>
          <Body muted>
            Privacy policy and terms of use are in preparation for the test-distribution release and
            are not yet published.
          </Body>
        </View>

        <Caption>Version {version} (MVP) · Brand assets provisional</Caption>
        <Caption>© 2026 Qaniva project</Caption>
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { gap: spacing.md, paddingBottom: spacing.xl },
  callout: {
    borderLeftWidth: 3,
    borderLeftColor: colors.brand,
    paddingLeft: spacing.md,
    gap: spacing.sm,
  },
  copyBlock: { gap: spacing.md },
});
