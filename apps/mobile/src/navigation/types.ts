import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import type { CompositeScreenProps, NavigatorScreenParams } from '@react-navigation/native';
import type { AttemptSummary } from '@qaniva/contracts';

/**
 * Navigation architecture:
 *   root stack — task flows (Onboarding, Briefing, Simulation, Results,
 *                About, Disclaimer) on top of
 *   bottom tabs — the four product surfaces (Home / Cases / Progress / Settings).
 * Simulation is a task flow launched from a case, never a tab.
 */
export type TabParamList = {
  Home: undefined;
  Cases: undefined;
  Progress: undefined;
  Settings: undefined;
};

export type RootStackParamList = {
  Onboarding: { page?: number } | undefined;
  Tabs: NavigatorScreenParams<TabParamList> | undefined;
  CaseDetail: { caseId: string; caseVersion: number; title: string };
  Simulation: {
    caseId: string;
    caseVersion: number;
    attemptId: string;
    seed: number;
    title: string;
    /** Runtime mode; omitted (= 'interactive') for every user launch. */
    mode?: string;
  };
  Results: { caseId: string; title: string; summary: AttemptSummary };
  About: undefined;
  Disclaimer: undefined;
};

export type ScreenProps<T extends keyof RootStackParamList> = NativeStackScreenProps<
  RootStackParamList,
  T
>;

export type TabScreenProps<T extends keyof TabParamList> = CompositeScreenProps<
  BottomTabScreenProps<TabParamList, T>,
  NativeStackScreenProps<RootStackParamList>
>;
