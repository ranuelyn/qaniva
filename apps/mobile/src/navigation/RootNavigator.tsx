import { NavigationContainer, DefaultTheme, type LinkingOptions } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { colors } from '@/theme/tokens';
import { HomeScreen } from '@/screens/HomeScreen';
import { CasesScreen } from '@/screens/CasesScreen';
import { CaseDetailScreen } from '@/screens/CaseDetailScreen';
import { SimulationScreen } from '@/screens/SimulationScreen';
import { ResultsScreen } from '@/screens/ResultsScreen';
import type { RootStackParamList } from './types';

const Stack = createNativeStackNavigator<RootStackParamList>();

/**
 * Deep links (app.json scheme "qaniva"). Also used by the scripted integration
 * proof to launch a simulation headlessly, e.g.:
 *   xcrun simctl openurl booted "qaniva://simulate/demo_sync_bradycardia_001?caseVersion=1&seed=20260830&attemptId=<uuid>&title=Demo"
 */
const linking: LinkingOptions<RootStackParamList> = {
  prefixes: ['qaniva://'],
  config: {
    screens: {
      Home: 'home',
      Cases: 'cases',
      Simulation: {
        path: 'simulate/:caseId',
        parse: { caseVersion: Number, seed: Number },
      },
    },
  },
};

const navTheme = {
  ...DefaultTheme,
  dark: true,
  colors: {
    ...DefaultTheme.colors,
    background: colors.background,
    card: colors.surface,
    text: colors.text,
    border: colors.border,
    primary: colors.primary,
  },
};

export function RootNavigator() {
  return (
    <NavigationContainer theme={navTheme} linking={linking}>
      <Stack.Navigator initialRouteName="Home">
        <Stack.Screen name="Home" component={HomeScreen} options={{ title: 'Qaniva' }} />
        <Stack.Screen name="Cases" component={CasesScreen} />
        <Stack.Screen
          name="CaseDetail"
          component={CaseDetailScreen}
          options={{ title: 'Briefing' }}
        />
        <Stack.Screen
          name="Simulation"
          component={SimulationScreen}
          options={{ headerShown: false, gestureEnabled: false }}
        />
        <Stack.Screen name="Results" component={ResultsScreen} />
      </Stack.Navigator>
    </NavigationContainer>
  );
}
