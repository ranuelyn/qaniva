import { beforeEach, describe, expect, it, vi } from 'vitest';

const memory = new Map<string, string>();
vi.mock('@react-native-async-storage/async-storage', () => ({
  default: {
    getItem: async (k: string) => memory.get(k) ?? null,
    setItem: async (k: string, v: string) => void memory.set(k, v),
    removeItem: async (k: string) => void memory.delete(k),
  },
}));

import { hasCompletedOnboarding, markOnboardingCompleted, resetOnboarding } from './appPrefs';

describe('appPrefs onboarding flag', () => {
  beforeEach(() => memory.clear());

  it('first run: onboarding not completed', async () => {
    expect(await hasCompletedOnboarding()).toBe(false);
  });

  it('completion persists (returning user routes straight to Tabs)', async () => {
    await markOnboardingCompleted();
    expect(await hasCompletedOnboarding()).toBe(true);
  });

  it('reset returns the app to first-run behavior', async () => {
    await markOnboardingCompleted();
    await resetOnboarding();
    expect(await hasCompletedOnboarding()).toBe(false);
  });
});
