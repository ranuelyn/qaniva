import { NavigationContainer, DefaultTheme, type LinkingOptions } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import { colors, typography } from '@/theme/tokens';
import { HomeScreen } from '@/screens/HomeScreen';
import { CasesScreen } from '@/screens/CasesScreen';
import { ProgressScreen } from '@/screens/ProgressScreen';
import { SettingsScreen } from '@/screens/SettingsScreen';
import { CaseDetailScreen } from '@/screens/CaseDetailScreen';
import { SimulationScreen } from '@/screens/SimulationScreen';
import { ResultsScreen } from '@/screens/ResultsScreen';
import { OnboardingScreen } from '@/screens/OnboardingScreen';
import { AboutScreen } from '@/screens/AboutScreen';
import { DisclaimerScreen } from '@/screens/DisclaimerScreen';
import type { RootStackParamList, TabParamList } from './types';

const Stack = createNativeStackNavigator<RootStackParamList>();
const Tab = createBottomTabNavigator<TabParamList>();

/**
 * Deep links (app.json scheme "qaniva"). Production apps use these for real
 * links; the scripted proofs/screenshot capture also drive REAL navigation
 * through them (no internal-state shortcuts), e.g.:
 *   xcrun simctl openurl booted "qaniva://cases"
 *   xcrun simctl openurl booted "qaniva://case/stemi_anterior_001?caseVersion=1&title=..."
 */
const linking: LinkingOptions<RootStackParamList> = {
  prefixes: ['qaniva://'],
  config: {
    screens: {
      Onboarding: 'onboarding',
      Tabs: {
        screens: {
          Home: 'home',
          Cases: 'cases',
          Progress: 'progress',
          Settings: 'settings',
        },
      },
      CaseDetail: {
        path: 'case/:caseId',
        parse: { caseVersion: Number },
      },
      Simulation: {
        path: 'simulate/:caseId',
        parse: { caseVersion: Number, seed: Number },
      },
      About: 'about',
      Disclaimer: 'disclaimer',
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
    primary: colors.brand,
  },
};

const TAB_ICONS: Record<keyof TabParamList, keyof typeof Ionicons.glyphMap> = {
  Home: 'home-outline',
  Cases: 'medkit-outline',
  Progress: 'stats-chart-outline',
  Settings: 'settings-outline',
};

function Tabs() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        headerStyle: { backgroundColor: colors.background },
        headerShadowVisible: false,
        headerTitleStyle: { ...typography.sectionTitle, color: colors.text },
        tabBarStyle: { backgroundColor: colors.surface, borderTopColor: colors.border },
        tabBarActiveTintColor: colors.brand,
        tabBarInactiveTintColor: colors.textFaint,
        tabBarIcon: ({ color, size }) => (
          <Ionicons name={TAB_ICONS[route.name as keyof TabParamList]} size={size} color={color} />
        ),
      })}
    >
      <Tab.Screen
        name="Home"
        component={HomeScreen}
        options={{ headerShown: false, title: 'Ana Sayfa' }}
      />
      <Tab.Screen name="Cases" component={CasesScreen} options={{ title: 'Vakalar' }} />
      <Tab.Screen name="Progress" component={ProgressScreen} options={{ title: 'İlerleme' }} />
      <Tab.Screen name="Settings" component={SettingsScreen} options={{ title: 'Ayarlar' }} />
    </Tab.Navigator>
  );
}

export function RootNavigator({ initialRouteName }: { initialRouteName: 'Onboarding' | 'Tabs' }) {
  return (
    <NavigationContainer theme={navTheme} linking={linking}>
      <Stack.Navigator
        initialRouteName={initialRouteName}
        screenOptions={{
          headerStyle: { backgroundColor: colors.background },
          headerShadowVisible: false,
          headerTintColor: colors.text,
          headerTitleStyle: { ...typography.sectionTitle, color: colors.text },
          headerBackButtonDisplayMode: 'minimal',
        }}
      >
        <Stack.Screen
          name="Onboarding"
          component={OnboardingScreen}
          options={{ headerShown: false }}
        />
        <Stack.Screen name="Tabs" component={Tabs} options={{ headerShown: false }} />
        <Stack.Screen
          name="CaseDetail"
          component={CaseDetailScreen}
          options={{ title: 'Vaka Özeti' }}
        />
        <Stack.Screen
          name="Simulation"
          component={SimulationScreen}
          options={{ headerShown: false, gestureEnabled: false }}
        />
        <Stack.Screen name="Results" component={ResultsScreen} options={{ title: 'Sonuçlar' }} />
        <Stack.Screen name="About" component={AboutScreen} options={{ title: 'Qaniva Hakkında' }} />
        <Stack.Screen
          name="Disclaimer"
          component={DisclaimerScreen}
          options={{ title: 'Eğitim Amaçlı Kullanım' }}
        />
      </Stack.Navigator>
    </NavigationContainer>
  );
}
