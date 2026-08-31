import { useCallback, useState } from 'react';
import { FlatList } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { apiClient, type CaseManifestEntry } from '@/api/client';
import { Body, Card, Screen, Title } from '@/components/ui';
import { CASE_CATALOG } from '@/cases/catalog';
import { attemptStore } from '@/storage/asyncStorageKv';
import type { CaseProgress } from '@/storage/attemptStore';
import type { ScreenProps } from '@/navigation/types';

const LOCAL_CASES = CASE_CATALOG.map((c) => c.manifest);

export function CasesScreen({ navigation }: ScreenProps<'Cases'>) {
  const [cases, setCases] = useState<CaseManifestEntry[]>(LOCAL_CASES);
  const [progress, setProgress] = useState<Record<string, CaseProgress>>({});

  // Refresh the backend manifest (offline keeps the bundled catalog) and the
  // locally persisted per-case progress every time this screen gains focus.
  useFocusEffect(
    useCallback(() => {
      let active = true;
      apiClient
        .listCases()
        .then((res) => {
          if (active && res.cases.length > 0) setCases(res.cases);
        })
        .catch(() => {
          /* offline: keep the bundled catalog */
        });
      Promise.all(LOCAL_CASES.map((c) => attemptStore.progressForCase(c.id))).then((all) => {
        if (!active) return;
        setProgress(Object.fromEntries(all.map((p) => [p.caseId, p])));
      });
      return () => {
        active = false;
      };
    }, []),
  );

  return (
    <Screen>
      <Title>Cases</Title>
      <FlatList
        data={cases}
        keyExtractor={(item) => `${item.id}@${item.version}`}
        contentContainerStyle={{ gap: 12 }}
        renderItem={({ item }) => {
          const p = progress[item.id];
          return (
            <Card
              onPress={() =>
                navigation.navigate('CaseDetail', {
                  caseId: item.id,
                  caseVersion: item.version,
                  title: item.title,
                })
              }
            >
              <Body>{item.title}</Body>
              <Body muted>
                {item.specialty} · ~{item.estimatedMinutes} min
              </Body>
              {p && p.attempts > 0 ? (
                <Body muted>
                  {p.completed ? '✓ completed' : 'attempted'} · best {p.bestScore} pts ·{' '}
                  {p.attempts} {p.attempts === 1 ? 'attempt' : 'attempts'}
                </Body>
              ) : (
                <Body muted>not attempted yet</Body>
              )}
            </Card>
          );
        }}
      />
    </Screen>
  );
}
