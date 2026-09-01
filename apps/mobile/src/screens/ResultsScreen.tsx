import { useEffect, useRef, useState } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import type { AttemptSummary, CriterionResult } from '@qaniva/contracts';
import {
  Body,
  Card,
  Caption,
  Eyebrow,
  Numeric,
  PrimaryButton,
  Screen,
  SecondaryButton,
  SectionHeader,
} from '@/components/ui';
import { colors, radius, spacing, typography } from '@/theme/tokens';
import { attemptStore } from '@/storage/asyncStorageKv';
import { analytics } from '@/analytics';
import { cryptoRandomId, randomSeed } from '@/lib/ids';
import type { ScreenProps } from '@/navigation/types';

/** E2E mode (see HomeScreen): auto-return to Home so the lifecycle loop can run. */
const E2E_MODE = Boolean(process.env.EXPO_PUBLIC_E2E_AUTOSTART);

/**
 * Deterministic results + debrief. Every fact on this screen comes from the
 * engine's AttemptSummary (rubric outcomes, timeline incl. authored state-change
 * causality, case-authored debrief metadata and references) — nothing is
 * invented here or by an LLM. The section taxonomy renders the RUBRIC's own
 * semantics: safety-harmful (harmful, non-efficiency category) is never mixed
 * with unnecessary/efficiency penalties, and critical criteria are surfaced
 * separately from recommended ones.
 */
const OUTCOME_LABELS: Record<string, string> = {
  complete: 'Case complete',
  partial: 'Completed — after avoidable deterioration',
  deteriorated: 'The patient deteriorated before definitive care',
  discharge: 'Ended by discharge',
  admit: 'Completed — patient admitted',
  death: 'The patient died',
  aborted: 'Attempt aborted',
};

function clock(sec: number): string {
  const m = Math.floor(sec / 60)
    .toString()
    .padStart(2, '0');
  const s = (sec % 60).toString().padStart(2, '0');
  return `${m}:${s}`;
}

function durationMin(summary: AttemptSummary): string {
  const ms = Date.parse(summary.completedAt) - Date.parse(summary.startedAt);
  if (!Number.isFinite(ms) || ms <= 0) return '';
  return `${Math.max(1, Math.round(ms / 60000))} min`;
}

function evidenceSuffix(c: CriterionResult): string {
  return c.evidenceRefs.length ? `  ·  Evidence: ${c.evidenceRefs.join(', ')}` : '';
}

function criterionColor(c: CriterionResult): string {
  if (c.classification === 'harmful') return colors.harmful;
  if (c.classification === 'delayed') return colors.warning;
  if (c.classification === 'missed') return colors.textMuted;
  return colors.success;
}

function criterionStatus(c: CriterionResult): string {
  if (c.classification === 'missed') return `Missed · 0/${c.maxPoints} pts`;
  if (c.classification === 'delayed') {
    return `Delayed ${clock(c.creditedAtSec)} · ${c.awardedPoints}/${c.maxPoints} pts`;
  }
  if (c.classification === 'harmful') return `${c.awardedPoints} pts`;
  return `On time ${clock(c.creditedAtSec)} · ${c.awardedPoints}/${c.maxPoints} pts`;
}

function CriterionRow({ criterion }: { criterion: CriterionResult }) {
  const accent = criterionColor(criterion);
  return (
    <View style={styles.criterionRow}>
      <View style={[styles.criterionMarker, { backgroundColor: accent }]} />
      <View style={styles.criterionCopy}>
        <Body>{criterion.label}</Body>
        <Text style={[styles.criterionStatus, { color: accent }]}>
          {criterionStatus(criterion)}
        </Text>
        {criterion.evidenceRefs.length > 0 ? (
          <Text style={styles.evidenceLabel}>Evidence · {criterion.evidenceRefs.join(' · ')}</Text>
        ) : null}
      </View>
    </View>
  );
}

export function ResultsScreen({ navigation, route }: ScreenProps<'Results'>) {
  const { caseId, title, summary } = route.params;
  const [saveNote, setSaveNote] = useState<string | null>(null);

  useEffect(() => {
    analytics.track({ event: 'debrief_viewed', caseId, attemptId: summary.attemptId });
    let active = true;
    attemptStore.save(summary).then((r) => {
      if (active && !r.ok) setSaveNote('Note: this attempt could not be saved on this device.');
    });
    return () => {
      active = false;
    };
  }, [caseId, summary]);

  const scrollRef = useRef<ScrollView>(null);
  useEffect(() => {
    if (!E2E_MODE) return;
    // Capture aid: scroll the REAL ScrollView through the debrief before
    // returning home, so the screenshot series records every section.
    const timers = [
      setTimeout(() => scrollRef.current?.scrollTo({ y: 900, animated: true }), 4000),
      setTimeout(() => scrollRef.current?.scrollTo({ y: 2000, animated: true }), 8000),
      setTimeout(() => scrollRef.current?.scrollToEnd({ animated: true }), 12000),
      setTimeout(() => navigation.popToTop(), 16000),
    ];
    return () => timers.forEach(clearTimeout);
  }, [navigation]);

  function replay() {
    analytics.track({
      event: 'replay_start',
      caseId,
      previousAttemptId: summary.attemptId,
    });
    // A fresh attempt: new attemptId + seed. Prior attempts stay persisted
    // (the store is keyed by attemptId); Unity reloads the case cleanly on the
    // warm runtime (verified by the relaunch PlayMode tests).
    navigation.replace('Simulation', {
      caseId,
      caseVersion: summary.caseVersion,
      attemptId: cryptoRandomId(),
      seed: randomSeed(),
      title,
    });
  }

  const criteria = summary.criteria ?? [];
  const critical = criteria.filter((c) => c.criticality === 'critical' && !c.harmful);
  const safetyHarm = criteria.filter(
    (c) => c.classification === 'harmful' && c.category !== 'efficiency',
  );
  const unnecessary = criteria.filter(
    (c) => c.classification === 'harmful' && c.category === 'efficiency',
  );
  const delayed = criteria.filter((c) => c.classification === 'delayed');
  const missedOther = criteria.filter(
    (c) => c.classification === 'missed' && c.criticality !== 'critical',
  );
  const doneWell = criteria.filter((c) => c.classification === 'correct');
  const alternatives = criteria.filter((c) => c.acceptedActionLabels.length > 1);
  const stateEvents = summary.timeline.filter((e) => e.stateChanges.length > 0);

  return (
    <Screen>
      <Eyebrow>Deterministic debrief</Eyebrow>
      <Body muted>{title}</Body>
      <ScrollView ref={scrollRef} contentContainerStyle={styles.content}>
        <View style={styles.hero}>
          <View style={styles.heroTop}>
            <View style={styles.heroCopy}>
              <Eyebrow>Outcome</Eyebrow>
              <Text style={styles.outcome}>
                {OUTCOME_LABELS[summary.terminalState] ?? summary.terminalState}
              </Text>
            </View>
            <Numeric>{summary.totalScore}</Numeric>
          </View>
          <View style={styles.scoreGrid}>
            {Object.entries(summary.scoreBreakdown).map(([label, value]) => (
              <View key={label} style={styles.scoreItem}>
                <Text style={styles.scoreValue}>{value}</Text>
                <Text style={styles.scoreLabel}>{label}</Text>
              </View>
            ))}
          </View>
          {durationMin(summary) ? <Caption>Attempt time · {durationMin(summary)}</Caption> : null}
          {saveNote ? <Body muted>{saveNote}</Body> : null}
        </View>

        {summary.debrief?.summary ? (
          <View style={styles.summary}>
            <Eyebrow>Case focus</Eyebrow>
            <Body muted>{summary.debrief.summary}</Body>
          </View>
        ) : null}

        {stateEvents.length > 0 && (
          <>
            <SectionHeader>What happened to the patient</SectionHeader>
            <View style={styles.causalRail}>
              {stateEvents.map((e) =>
                e.stateChanges.map((text, i) => (
                  <View key={`${e.seq}-${i}`} style={styles.causalRow}>
                    <Text style={styles.causalTime}>{clock(e.simTimeSec)}</Text>
                    <View style={styles.causalDot} />
                    <View style={styles.causalCopy}>
                      <Body>{text}</Body>
                    </View>
                  </View>
                )),
              )}
            </View>
          </>
        )}

        <SectionHeader>Critical decisions</SectionHeader>
        <View style={styles.debriefGroup}>
          {critical.map((c) => (
            <CriterionRow key={c.id} criterion={c} />
          ))}
        </View>

        {safetyHarm.length > 0 && (
          <>
            <SectionHeader>Harmful actions</SectionHeader>
            {safetyHarm.map((c) => (
              <Card key={c.id}>
                <Body>
                  {c.label} — at {clock(c.creditedAtSec)} ({c.awardedPoints} pts)
                </Body>
                <Body muted>{`Safety-relevant penalty${evidenceSuffix(c)}`}</Body>
              </Card>
            ))}
          </>
        )}

        {unnecessary.length > 0 && (
          <>
            <SectionHeader>Unnecessary (efficiency)</SectionHeader>
            {unnecessary.map((c) => (
              <Card key={c.id}>
                <Body>
                  {c.label} ({c.awardedPoints} pts)
                </Body>
                <Body muted>{`Efficiency penalty — not patient harm${evidenceSuffix(c)}`}</Body>
              </Card>
            ))}
          </>
        )}

        {delayed.length > 0 && (
          <>
            <SectionHeader>Correct but delayed</SectionHeader>
            {delayed.map((c) => (
              <Card key={c.id}>
                <Body>
                  {c.label} — done at {clock(c.creditedAtSec)}, {c.awardedPoints}/{c.maxPoints} pts
                </Body>
                <Body muted>
                  {`Performed after the full-credit window — timing credit reduced by ${
                    Math.round((c.maxPoints - c.awardedPoints) * 10) / 10
                  } pts${evidenceSuffix(c)}`}
                </Body>
              </Card>
            ))}
          </>
        )}

        {missedOther.length > 0 && (
          <>
            <SectionHeader>Missed</SectionHeader>
            {missedOther.map((c) => (
              <Card key={c.id}>
                <Body>
                  {c.label} — missed (0/{c.maxPoints} pts)
                </Body>
                <Body muted>{`${c.criticality} criterion${evidenceSuffix(c)}`}</Body>
              </Card>
            ))}
          </>
        )}

        {alternatives.length > 0 && (
          <>
            <SectionHeader>Accepted alternatives</SectionHeader>
            {alternatives.map((c) => (
              <Card key={`alt-${c.id}`}>
                <Body muted>
                  {c.label}: {c.acceptedActionLabels.join('  —or—  ')} (either earns the same
                  credit)
                </Body>
              </Card>
            ))}
          </>
        )}

        {doneWell.filter((c) => c.criticality !== 'critical').length > 0 && (
          <>
            <SectionHeader>Done well</SectionHeader>
            {doneWell
              .filter((c) => c.criticality !== 'critical')
              .map((c) => (
                <Card key={c.id}>
                  <Body muted>
                    {c.label} — on time ({clock(c.creditedAtSec)}), {c.awardedPoints}/{c.maxPoints}{' '}
                    pts{evidenceSuffix(c)}
                  </Body>
                </Card>
              ))}
          </>
        )}

        {(summary.debrief?.keyTeachingPoints?.length ?? 0) > 0 && (
          <>
            <SectionHeader>Key teaching points</SectionHeader>
            <Card>
              {summary.debrief.keyTeachingPoints.map((p, i) => (
                <Body key={i} muted>
                  • {p}
                </Body>
              ))}
            </Card>
          </>
        )}

        {(summary.debrief?.commonErrors?.length ?? 0) > 0 && (
          <>
            <SectionHeader>Common errors in this case</SectionHeader>
            <Card>
              {summary.debrief.commonErrors.map((p, i) => (
                <Body key={i} muted>
                  • {p}
                </Body>
              ))}
            </Card>
          </>
        )}

        <SectionHeader>Clinical timeline</SectionHeader>
        <View style={styles.timelineGroup}>
          {summary.timeline.map((entry) => (
            <View key={entry.seq} style={styles.timelineRow}>
              <Text style={styles.timelineTime}>{clock(entry.simTimeSec)}</Text>
              <View style={styles.timelineCopy}>
                <Body>{entry.label}</Body>
                <Text style={styles.timelineClassification}>{entry.classification}</Text>
                {entry.stateChanges.map((text, i) => (
                  <Body key={i} muted>
                    Patient response · {text}
                  </Body>
                ))}
              </View>
            </View>
          ))}
        </View>

        {(summary.references?.length ?? 0) > 0 && (
          <>
            <SectionHeader>References</SectionHeader>
            <View style={styles.references}>
              {summary.references.map((r, i) => (
                <View key={i} style={styles.referenceRow}>
                  <Body>{r.label}</Body>
                  <Body muted>{r.citation}</Body>
                </View>
              ))}
            </View>
          </>
        )}

        <Caption>replay hash: {summary.replayHash}</Caption>
      </ScrollView>

      <PrimaryButton label="Replay this case" onPress={replay} />
      <SecondaryButton label="Back to home" onPress={() => navigation.popToTop()} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { gap: spacing.md, paddingBottom: spacing.md },
  hero: {
    backgroundColor: colors.surface,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    gap: spacing.md,
  },
  heroTop: { flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between' },
  heroCopy: { flex: 1, gap: spacing.xs, paddingRight: spacing.md },
  outcome: { ...typography.cardTitle, color: colors.text },
  scoreGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm },
  scoreItem: {
    minWidth: '30%',
    flexGrow: 1,
    backgroundColor: colors.surfaceAlt,
    borderRadius: radius.sm,
    padding: spacing.sm,
  },
  scoreValue: { ...typography.cardTitle, color: colors.text },
  scoreLabel: { ...typography.caption, color: colors.textFaint, textTransform: 'capitalize' },
  summary: { gap: spacing.sm, paddingHorizontal: spacing.xs },
  causalRail: { gap: 0 },
  causalRow: { flexDirection: 'row', alignItems: 'stretch', minHeight: 64 },
  causalTime: { ...typography.caption, color: colors.brand, width: 48, paddingTop: spacing.sm },
  causalDot: {
    width: 10,
    height: 10,
    borderRadius: 5,
    backgroundColor: colors.brand,
    marginTop: spacing.sm + 2,
    marginRight: spacing.md,
  },
  causalCopy: {
    flex: 1,
    borderLeftWidth: 1,
    borderLeftColor: colors.brandDim,
    paddingLeft: spacing.md,
    paddingBottom: spacing.md,
  },
  debriefGroup: {
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    overflow: 'hidden',
  },
  criterionRow: {
    flexDirection: 'row',
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border,
  },
  criterionMarker: { width: 3 },
  criterionCopy: { flex: 1, gap: spacing.xs, padding: spacing.md },
  criterionStatus: { ...typography.caption },
  evidenceLabel: { ...typography.caption, color: colors.textFaint },
  timelineGroup: { borderTopWidth: 1, borderTopColor: colors.border },
  timelineRow: {
    flexDirection: 'row',
    gap: spacing.md,
    paddingVertical: spacing.md,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border,
  },
  timelineTime: { ...typography.caption, color: colors.brand, width: 48 },
  timelineCopy: { flex: 1, gap: spacing.xs },
  timelineClassification: {
    ...typography.caption,
    color: colors.textFaint,
    textTransform: 'capitalize',
  },
  references: { gap: spacing.md },
  referenceRow: {
    borderLeftWidth: 2,
    borderLeftColor: colors.border,
    paddingLeft: spacing.md,
    gap: spacing.xs,
  },
});
