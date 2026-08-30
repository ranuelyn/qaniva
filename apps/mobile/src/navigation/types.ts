import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AttemptSummary } from '@qaniva/contracts';

/**
 * First vertical slice (blueprint §19):
 * Home -> Cases -> Case Detail/Briefing -> Simulation (Unity host) -> Results
 */
export type RootStackParamList = {
  Home: undefined;
  Cases: undefined;
  CaseDetail: { caseId: string; caseVersion: number; title: string };
  Simulation: {
    caseId: string;
    caseVersion: number;
    attemptId: string;
    seed: number;
    title: string;
  };
  Results: { caseId: string; title: string; summary: AttemptSummary };
};

export type ScreenProps<T extends keyof RootStackParamList> = NativeStackScreenProps<
  RootStackParamList,
  T
>;
