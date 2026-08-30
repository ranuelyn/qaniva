import { ScrollView } from 'react-native';
import { Body, Card, PrimaryButton, Screen, Title } from '@/components/ui';
import type { ScreenProps } from '@/navigation/types';

export function ResultsScreen({ navigation, route }: ScreenProps<'Results'>) {
  const { title, summary } = route.params;

  return (
    <Screen>
      <Title>Results</Title>
      <Body muted>{title}</Body>
      <ScrollView contentContainerStyle={{ gap: 12 }}>
        <Card>
          <Body>Outcome: {summary.terminalState}</Body>
          <Body>Score: {summary.totalScore}</Body>
          <Body muted>
            critical {summary.scoreBreakdown.critical} · timing {summary.scoreBreakdown.timing} ·
            treatment {summary.scoreBreakdown.treatment} · disposition{' '}
            {summary.scoreBreakdown.disposition}
          </Body>
          <Body muted>replay: {summary.replayHash}</Body>
        </Card>

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
