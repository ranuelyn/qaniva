import { useCallback, useEffect, useState } from 'react';
import { FlatList } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Screen } from '@/components/ui';
import { CaseCard } from '@/components/CaseCard';
import { CASE_CATALOG, specialtyLabel } from '@/cases/catalog';
import { attemptStore } from '@/storage/asyncStorageKv';
import type { CaseProgress } from '@/storage/attemptStore';
import { analytics } from '@/analytics';
import type { TabScreenProps } from '@/navigation/types';

/**
 * The case library. Fully catalog-driven (a third case = one import line in
 * the catalog). No search/filters at this catalog size — deliberate.
 */
export function CasesScreen({ navigation }: TabScreenProps<'Cases'>) {
  const [progress, setProgress] = useState<Record<string, CaseProgress>>({});

  useEffect(() => {
    analytics.track({ event: 'surface_viewed', surface: 'cases' });
  }, []);

  useFocusEffect(
    useCallback(() => {
      let active = true;
      Promise.all(CASE_CATALOG.map((c) => attemptStore.progressForCase(c.manifest.id))).then(
        (all) => {
          if (active) setProgress(Object.fromEntries(all.map((p) => [p.caseId, p])));
        },
      );
      return () => {
        active = false;
      };
    }, []),
  );

  return (
    <Screen>
      <FlatList
        data={CASE_CATALOG}
        keyExtractor={(c) => `${c.manifest.id}@${c.manifest.version}`}
        contentContainerStyle={{ gap: 12 }}
        renderItem={({ item }) => (
          <CaseCard
            title={item.manifest.title}
            teaser={item.teaser}
            specialty={specialtyLabel(item.manifest.specialty)}
            minutes={item.manifest.estimatedMinutes}
            progress={progress[item.manifest.id]}
            onPress={() =>
              navigation.navigate('CaseDetail', {
                caseId: item.manifest.id,
                caseVersion: item.manifest.version,
                title: item.manifest.title,
              })
            }
          />
        )}
      />
    </Screen>
  );
}
