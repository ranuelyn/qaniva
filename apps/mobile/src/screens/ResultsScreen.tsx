import { useEffect, useRef, useState } from 'react';
import { ScrollView, View } from 'react-native';
import type { AttemptSummary, CriterionResult } from '@qaniva/contracts';
import {
  Body,
  Card,
  Caption,
  Numeric,
  PrimaryButton,
  Screen,
  SecondaryButton,
  SectionHeader,
} from '@/components/ui';
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
      <Body muted>{title}</Body>
      <ScrollView ref={scrollRef} contentContainerStyle={{ gap: 10, paddingBottom: 12 }}>
        <Card>
          <Body>{OUTCOME_LABELS[summary.terminalState] ?? summary.terminalState}</Body>
          <Numeric>{summary.totalScore} pts</Numeric>
          <Body muted>
            critical {summary.scoreBreakdown.critical} · timing {summary.scoreBreakdown.timing} ·
            treatment {summary.scoreBreakdown.treatment} · disposition{' '}
            {summary.scoreBreakdown.disposition} · efficiency {summary.scoreBreakdown.efficiency}
          </Body>
          {durationMin(summary) ? <Body muted>Attempt time: {durationMin(summary)}</Body> : null}
          {saveNote ? <Body muted>{saveNote}</Body> : null}
        </Card>

        {summary.debrief?.summary ? (
          <Card>
            <Body muted>{summary.debrief.summary}</Body>
          </Card>
        ) : null}

        {stateEvents.length > 0 && (
          <>
            <SectionHeader>What happened to the patient</SectionHeader>
            {stateEvents.map((e) =>
              e.stateChanges.map((text, i) => (
                <Card key={`${e.seq}-${i}`}>
                  <Body>
                    {clock(e.simTimeSec)} — {text}
                  </Body>
                </Card>
              )),
            )}
          </>
        )}

        <SectionHeader>Critical decisions</SectionHeader>
        {critical.map((c) => (
          <Card key={c.id}>
            <Body>
              {c.classification === 'missed' ? '✗ ' : '✓ '}
              {c.label}
              {c.classification === 'missed'
                ? ` — missed (0/${c.maxPoints} pts)`
                : c.classification === 'delayed'
                  ? ` — delayed (${clock(c.creditedAtSec)}), ${c.awardedPoints}/${c.maxPoints} pts`
                  : ` — on time (${clock(c.creditedAtSec)}), ${c.awardedPoints}/${c.maxPoints} pts`}
            </Body>
            <Body muted>{`${c.criticality} criterion${evidenceSuffix(c)}`}</Body>
          </Card>
        ))}

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
        {summary.timeline.map((entry) => (
          <Card key={entry.seq}>
            <Body>
              {clock(entry.simTimeSec)} · {entry.label}
            </Body>
            <Body muted>{entry.classification}</Body>
            {entry.stateChanges.map((text, i) => (
              <Body key={i} muted>
                ⚠ {text}
              </Body>
            ))}
          </Card>
        ))}

        {(summary.references?.length ?? 0) > 0 && (
          <>
            <SectionHeader>References</SectionHeader>
            <Card>
              {summary.references.map((r, i) => (
                <View key={i} style={{ marginBottom: 4 }}>
                  <Body>{r.label}</Body>
                  <Body muted>{r.citation}</Body>
                </View>
              ))}
            </Card>
          </>
        )}

        <Caption>replay hash: {summary.replayHash}</Caption>
      </ScrollView>

      <PrimaryButton label="Replay this case" onPress={replay} />
      <SecondaryButton label="Back to home" onPress={() => navigation.popToTop()} />
    </Screen>
  );
}
