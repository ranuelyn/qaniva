import { NavigationContainer, DefaultTheme } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { colors } from '@/theme/tokens';
import { HomeScreen } from '@/screens/HomeScreen';
import { CasesScreen } from '@/screens/CasesScreen';
import { CaseDetailScreen } from '@/screens/CaseDetailScreen';
import { SimulationScreen } from '@/screens/SimulationScreen';
import { ResultsScreen } from '@/screens/ResultsScreen';
import type { RootStackParamList } from './types';

const Stack = createNativeStackNavigator<RootStackParamList>();

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
    <NavigationContainer theme={navTheme}>
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
