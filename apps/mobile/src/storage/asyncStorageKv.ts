import AsyncStorage from '@react-native-async-storage/async-storage';
import { AttemptStore, type KeyValueStore } from './attemptStore';

/** App-side binding: AttemptStore over the device's AsyncStorage. */
const asyncStorageKv: KeyValueStore = {
  getItem: (key) => AsyncStorage.getItem(key),
  setItem: (key, value) => AsyncStorage.setItem(key, value),
  removeItem: (key) => AsyncStorage.removeItem(key),
};

export const attemptStore = new AttemptStore(asyncStorageKv);
