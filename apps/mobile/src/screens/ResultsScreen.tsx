import { useEffect } from 'react';
import { ScrollView } from 'react-native';
import type { CriterionResult } from '@qaniva/contracts';
import { Body, Card, PrimaryButton, Screen, Title } from '@/components/ui';
import type { ScreenProps } from '@/navigation/types';

/** E2E mode (see HomeScreen): auto-return to Home so the lifecycle loop can run. */
const E2E_MODE = Boolean(process.env.EXPO_PUBLIC_E2E_AUTOSTART);

/**
 * Deterministic results + debrief. Every fact on this screen comes from the
 * engine's AttemptSummary (rubric outcomes, timeline, case-authored debrief
 * metadata) — nothing is invented here or by an LLM.
 */
const OUTCOME_LABELS: Record<string, string> = {
  complete: 'Case complete',
  partial: 'Completed — after avoidable deterioration',
  deteriorated: 'The patient deteriorated before definitive care',
  discharge: 'Ended by discharge',
  admit: 'Ended by admission',
  death: 'The patient died',
  aborted: 'Attempt aborted',
};

/** Timing-aware wording — Qaniva's differentiator must be visible post-case. */
function criterionLine(c: CriterionResult): string {
  switch (c.classification) {
    case 'correct':
      return `${c.label} — on time (${formatClock(c.creditedAtSec)}), ${c.awardedPoints}/${c.maxPoints} pts`;
    case 'delayed':
      return `${c.label} — correct but delayed (${formatClock(c.creditedAtSec)}), ${c.awardedPoints}/${c.maxPoints} pts`;
    case 'missed':
      return `${c.label} — missed (0/${c.maxPoints} pts)`;
    case 'harmful':
      return `${c.label} — at ${formatClock(c.creditedAtSec)} (${c.awardedPoints} pts)`;
    case 'avoided':
      return c.label;
  }
}

export function ResultsScreen({ navigation, route }: ScreenProps<'Results'>) {
  const { title, summary } = route.params;

  useEffect(() => {
    if (!E2E_MODE) return;
    const timer = setTimeout(() => navigation.popToTop(), 6000);
    return () => clearTimeout(timer);
  }, [navigation]);

  const criteria = summary.criteria ?? [];
  const byClass = (...cls: CriterionResult['classification'][]) =>
    criteria.filter((c) => cls.includes(c.classification));
  const correct = byClass('correct');
  const delayed = byClass('delayed');
  const missed = byClass('missed');
  const harmful = byClass('harmful');
  // Only surface "safety penalty avoided" rows in debrief when something else
  // went wrong — listing every avoided trap on a perfect run is noise.

  return (
    <Screen>
      <Title>Results</Title>
      <Body muted>{title}</Body>
      <ScrollView contentContainerStyle={{ gap: 12 }}>
        <Card>
          <Body>{OUTCOME_LABELS[summary.terminalState] ?? summary.terminalState}</Body>
          <Body>Score: {summary.totalScore}</Body>
          <Body muted>
            critical {summary.scoreBreakdown.critical} · timing {summary.scoreBreakdown.timing} ·
            treatment {summary.scoreBreakdown.treatment} · disposition{' '}
            {summary.scoreBreakdown.disposition} · efficiency {summary.scoreBreakdown.efficiency}
          </Body>
          <Body muted>replay: {summary.replayHash}</Body>
        </Card>

        {summary.debrief?.summary ? (
          <Card>
            <Body>{summary.debrief.summary}</Body>
          </Card>
        ) : null}

        {harmful.length > 0 && (
          <>
            <Title>Harmful actions</Title>
            {harmful.map((c) => (
              <Card key={c.id}>
                <Body>{criterionLine(c)}</Body>
              </Card>
            ))}
          </>
        )}

        {missed.length > 0 && (
          <>
            <Title>Missed</Title>
            {missed.map((c) => (
              <Card key={c.id}>
                <Body>{criterionLine(c)}</Body>
              </Card>
            ))}
          </>
        )}

        {delayed.length > 0 && (
          <>
            <Title>Correct but delayed</Title>
            {delayed.map((c) => (
              <Card key={c.id}>
                <Body>{criterionLine(c)}</Body>
              </Card>
            ))}
          </>
        )}

        {correct.length > 0 && (
          <>
            <Title>Done well</Title>
            {correct.map((c) => (
              <Card key={c.id}>
                <Body muted>{criterionLine(c)}</Body>
              </Card>
            ))}
          </>
        )}

        {(summary.debrief?.keyTeachingPoints?.length ?? 0) > 0 && (
          <>
            <Title>Key teaching points</Title>
            <Card>
              {summary.debrief.keyTeachingPoints.map((p, i) => (
                <Body key={i} muted>
                  • {p}
                </Body>
              ))}
            </Card>
          </>
        )}

        <Title>Clinical timeline</Title>
        {summary.timeline.map((entry) => (
          <Card key={entry.seq}>
            <Body>
              {formatClock(entry.simTimeSec)} · {entry.label}
            </Body>
            <Body muted>{entry.classification}</Body>
          </Card>
        ))}
      </ScrollView>

      <PrimaryButton label="Back to home" onPress={() => navigation.popToTop()} />
    </Screen>
  );
}

function formatClock(sec: number): string {
  const m = Math.floor(sec / 60)
    .toString()
    .padStart(2, '0');
  const s = (sec % 60).toString().padStart(2, '0');
  return `${m}:${s}`;
}
