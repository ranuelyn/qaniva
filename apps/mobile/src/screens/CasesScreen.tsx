import { useEffect, useState } from 'react';
import { FlatList } from 'react-native';
import { apiClient, type CaseManifestEntry } from '@/api/client';
import { Body, Card, Screen, Title } from '@/components/ui';
import type { ScreenProps } from '@/navigation/types';

// Offline fallback so the shell is usable before the backend is running.
// Mirrors packages/case-schema/fixtures metadata (the API serves the same set).
const FALLBACK: CaseManifestEntry[] = [
  {
    id: 'stemi_anterior_001',
    version: 1,
    title: 'Crushing chest pain in a 54-year-old',
    chiefComplaint: 'Severe central chest pain for the last 90 minutes',
    specialty: 'emergency_medicine',
    estimatedMinutes: 10,
    clinicalReviewStatus: 'mvp_demo_approved',
  },
  {
    id: 'demo_sync_bradycardia_001',
    version: 1,
    title: 'FICTIONAL DEMO — Adult with a slow pulse and dizziness',
    chiefComplaint: 'Feeling dizzy and weak for the last hour',
    specialty: 'emergency_medicine',
    estimatedMinutes: 8,
    clinicalReviewStatus: 'not_reviewed',
  },
];

export function CasesScreen({ navigation }: ScreenProps<'Cases'>) {
  const [cases, setCases] = useState<CaseManifestEntry[]>(FALLBACK);

  useEffect(() => {
    let active = true;
    apiClient
      .listCases()
      .then((res) => {
        if (active && res.cases.length > 0) setCases(res.cases);
      })
      .catch(() => {
        /* keep fallback */
      });
    return () => {
      active = false;
    };
  }, []);

  return (
    <Screen>
      <Title>Cases</Title>
      <FlatList
        data={cases}
        keyExtractor={(item) => `${item.id}@${item.version}`}
        contentContainerStyle={{ gap: 12 }}
        renderItem={({ item }) => (
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
              {item.specialty} · ~{item.estimatedMinutes} min · review: {item.clinicalReviewStatus}
            </Body>
          </Card>
        )}
      />
    </Screen>
  );
}
