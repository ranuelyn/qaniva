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
        <Body muted>
          Qaniva, etkileşimli bir klinik karar simülasyonu platformudur. Öğrenenler dinamik 3B hasta
          vakalarında değerlendirme, tetkik, tedavi ve klinik akıl yürütme pratiği yapar —
          zamanlama, sıra ve hastanın durumu sonucu, skoru ve zaman çizelgesini belirler. Her karar
          deterministik, kanıta referanslı bir değerlendirmede gözden geçirilir.
        </Body>

        <SectionHeader>Nasıl çalışır</SectionHeader>
        <View style={styles.callout}>
          <Eyebrow>Gerçeğin sahibi motordur</Eyebrow>
          <Body muted>
            Her vaka, deterministik bir klinik motor tarafından çalıştırılan sürümlü ve kanıta
            referanslı bir tanımdır: aynı kararlar her zaman aynı sonucu, zaman çizelgesini ve skoru
            üretir. Simülasyonda hiçbir şey yapay zekâ tarafından uydurulmaz.
          </Body>
        </View>

        <SectionHeader>Durum</SectionHeader>
        <View style={styles.copyBlock}>
          <Body muted>
            Bu, iç kullanım ve tanıtım amaçlı bir MVP sürümüdür. Klinik içerik kanıta dayalı ve
            kurgusaldır; resmî hekim doğrulaması beklemektedir — bkz. &quot;Eğitim amaçlı kullanım
            ve klinik durum&quot;.
          </Body>
          <Body muted>
            Gizlilik politikası ve kullanım koşulları test dağıtımı için hazırlanmaktadır ve henüz
            yayımlanmamıştır.
          </Body>
        </View>

        <Caption>Sürüm {version} (MVP) · Marka varlıkları geçicidir</Caption>
        <Caption>© 2026 Qaniva projesi</Caption>
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
