import { useCallback, useEffect, useState } from 'react';
import { ScrollView } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Body, Card, PrimaryButton, Screen, Title } from '@/components/ui';
import { apiClient } from '@/api/client';
import { catalogCase, DEFAULT_BRIEFING } from '@/cases/catalog';
import { attemptStore } from '@/storage/asyncStorageKv';
import type { StoredAttempt } from '@/storage/attemptStore';
import { analytics } from '@/analytics';
import { cryptoRandomId, randomSeed } from '@/lib/ids';
import type { ScreenProps } from '@/navigation/types';

export function CaseDetailScreen({ navigation, route }: ScreenProps<'CaseDetail'>) {
  const { caseId, caseVersion, title } = route.params;
  const briefing = catalogCase(caseId)?.briefing ?? DEFAULT_BRIEFING;
  const [history, setHistory] = useState<StoredAttempt[]>([]);

  useEffect(() => {
    analytics.track({ event: 'case_viewed', caseId });
  }, [caseId]);

  useFocusEffect(
    useCallback(() => {
      let active = true;
      attemptStore.listForCase(caseId).then((attempts) => {
        if (active) setHistory(attempts.slice(-3).reverse());
      });
      return () => {
        active = false;
      };
    }, [caseId]),
  );

  async function begin() {
    let seed = randomSeed();
    let attemptId = cryptoRandomId();
    try {
      const started = await apiClient.startAttempt(caseId, caseVersion, 'standard');
      seed = started.seed;
      attemptId = started.attemptId;
    } catch {
      // Offline: use a locally generated attempt id + seed.
    }
    navigation.navigate('Simulation', { caseId, caseVersion, attemptId, seed, title });
  }

  return (
    <Screen>
      <Title>{title}</Title>
      <ScrollView contentContainerStyle={{ gap: 8 }}>
        {briefing.map((line, i) => (
          <Body key={i} muted>
            {line}
          </Body>
        ))}
        {history.length > 0 && (
          <>
            <Body>Your recent attempts</Body>
            {history.map((a) => (
              <Card key={a.summary.attemptId}>
                <Body muted>
                  {a.summary.totalScore} pts · {a.summary.terminalState} ·{' '}
                  {new Date(a.summary.completedAt).toLocaleString()}
                </Body>
              </Card>
            ))}
          </>
        )}
      </ScrollView>
      <PrimaryButton
        label={history.length > 0 ? 'Play again' : 'Enter simulation'}
        onPress={begin}
      />
    </Screen>
  );
}
