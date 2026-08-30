import { defineConfig } from 'vitest/config';

// Only pure, RN-free modules are unit-tested here (bridge protocol handling,
// analytics contract). Screen/component testing needs jest-expo and is a later
// task; keeping this lean keeps CI fast and reliable.
export default defineConfig({
  test: {
    include: ['src/**/*.test.ts'],
    environment: 'node',
  },
});
