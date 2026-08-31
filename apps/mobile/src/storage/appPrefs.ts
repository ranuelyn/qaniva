import AsyncStorage from '@react-native-async-storage/async-storage';

/**
 * Tiny local app preferences (no accounts in MVP). Failure-tolerant: a broken
 * store degrades to first-run defaults rather than blocking the app.
 */
const ONBOARDING_KEY = 'qaniva.onboarding.v1';

export async function hasCompletedOnboarding(): Promise<boolean> {
  try {
    return (await AsyncStorage.getItem(ONBOARDING_KEY)) === 'done';
  } catch {
    return false;
  }
}

export async function markOnboardingCompleted(): Promise<void> {
  try {
    await AsyncStorage.setItem(ONBOARDING_KEY, 'done');
  } catch {
    /* non-fatal */
  }
}

/** Reset local progress: attempts + onboarding stay independent — this clears BOTH stores' keys explicitly. */
export async function resetOnboarding(): Promise<void> {
  try {
    await AsyncStorage.removeItem(ONBOARDING_KEY);
  } catch {
    /* non-fatal */
  }
}
