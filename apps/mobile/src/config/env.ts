import Constants from 'expo-constants';

/**
 * Runtime configuration. Public values only — anything sensitive stays on the
 * backend (ADR-005 / blueprint §8). Expo exposes `EXPO_PUBLIC_*` vars at build time.
 */
export type AppEnv = 'development' | 'staging' | 'production';

interface AppConfig {
  env: AppEnv;
  apiBaseUrl: string;
  /** Bridge protocol version this build speaks (from @qaniva/contracts). */
  analyticsEnabled: boolean;
}

function readExtra(key: string, fallback: string): string {
  const fromProcess = process.env[key];
  if (fromProcess && fromProcess.length > 0) return fromProcess;
  const extra = (Constants.expoConfig?.extra ?? {}) as Record<string, unknown>;
  const val = extra[key];
  return typeof val === 'string' && val.length > 0 ? val : fallback;
}

export const config: AppConfig = {
  env: (readExtra('EXPO_PUBLIC_ENV', 'development') as AppEnv) ?? 'development',
  apiBaseUrl: readExtra('EXPO_PUBLIC_API_BASE_URL', 'http://localhost:3000'),
  analyticsEnabled: readExtra('EXPO_PUBLIC_ANALYTICS_ENABLED', 'false') === 'true',
};
