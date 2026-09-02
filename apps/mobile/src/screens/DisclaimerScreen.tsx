import { useEffect } from 'react';
import { ScrollView, StyleSheet, View } from 'react-native';
import { Body, Eyebrow, Screen, SectionHeader } from '@/components/ui';
import { analytics } from '@/analytics';
import { colors, spacing } from '@/theme/tokens';
import type { ScreenProps } from '@/navigation/types';

/** Readable, non-buried educational boundary. */
export function DisclaimerScreen(_props: ScreenProps<'Disclaimer'>) {
  useEffect(() => {
    analytics.track({ event: 'surface_viewed', surface: 'disclaimer' });
  }, []);

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <View style={styles.primaryNotice}>
          <Eyebrow>Yalnızca eğitim amaçlı simülasyon</Eyebrow>
          <Body>
            Qaniva bir eğitim simülasyonudur. Gerçek hastaların tanı veya tedavisi için
            tasarlanmamıştır ve gerçek hasta bakımına yön vermek için kullanılmamalıdır.
          </Body>
        </View>

        <View style={styles.copyBlock}>
          <Body>
            Qaniva&apos;daki hiçbir şey klinik muhakemenin, süpervizyonun ya da kurumunuzun protokol
            ve kılavuzlarının yerini almaz. Qaniva ile yerel protokolünüz farklıysa protokolünüzü
            izleyin.
          </Body>
        </View>

        <SectionHeader>Klinik içeriğin durumu</SectionHeader>
        <View style={styles.copyBlock}>
          <Body>
            Bu MVP sürümündeki vakalar kurgusal, kanıta referanslı öğretim senaryolarıdır. Klinik
            içerikleri şu anda resmî hekim incelemesini beklemektedir — doğrulanmış klinik rehber
            değil, taslak eğitim materyali olarak ele alınmalıdır.
          </Body>
        </View>

        <SectionHeader>Verileriniz</SectionHeader>
        <View style={styles.copyBlock}>
          <Body>
            Qaniva&apos;daki tüm hastalar kurgusaldır. Uygulamanın hiçbir yerine gerçek hasta verisi
            girmeyin. MVP&apos;de denemeleriniz ve skorlarınız yalnızca bu cihazda saklanır.
          </Body>
        </View>
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { gap: spacing.md, paddingBottom: spacing.xl },
  primaryNotice: {
    borderLeftWidth: 3,
    borderLeftColor: colors.warning,
    paddingLeft: spacing.md,
    gap: spacing.sm,
  },
  copyBlock: { paddingLeft: spacing.md, borderLeftWidth: 1, borderLeftColor: colors.border },
});
