import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { ErrorBoundary } from '@/components/ErrorBoundary';
import { RootNavigator } from '@/navigation/RootNavigator';
import { hasCompletedOnboarding } from '@/storage/appPrefs';
import { colors } from '@/theme/tokens';

export default function App() {
  // First-install: Splash -> Onboarding -> Home. Returning: Splash -> Home.
  // The flag read is a few ms; a brand-background view bridges it so there is
  // never a white flash between the native splash and the first screen.
  const [initialRoute, setInitialRoute] = useState<'Onboarding' | 'Tabs' | null>(null);

  useEffect(() => {
    hasCompletedOnboarding().then((done) => setInitialRoute(done ? 'Tabs' : 'Onboarding'));
  }, []);

  return (
    <SafeAreaProvider>
      <ErrorBoundary>
        <StatusBar style="light" />
        {initialRoute === null ? (
          <View style={{ flex: 1, backgroundColor: colors.background }} />
        ) : (
          <RootNavigator initialRouteName={initialRoute} />
        )}
      </ErrorBoundary>
    </SafeAreaProvider>
  );
}
