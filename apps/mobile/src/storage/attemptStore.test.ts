import { describe, expect, it } from 'vitest';
import type { AttemptSummary } from '@qaniva/contracts';
import { AttemptStore, type KeyValueStore } from './attemptStore';

function memoryKv(opts?: { failWrites?: boolean }): KeyValueStore {
  const map = new Map<string, string>();
  return {
    getItem: async (k) => map.get(k) ?? null,
    setItem: async (k, v) => {
      if (opts?.failWrites) throw new Error('disk full');
      map.set(k, v);
    },
  };
}

function summary(over: Partial<AttemptSummary>): AttemptSummary {
  return {
    attemptId: '11111111-1111-4111-8111-111111111111',
    caseId: 'stemi_anterior_001',
    caseVersion: 1,
    seed: 1,
    startedAt: '2026-08-31T10:00:00.000Z',
    completedAt: '2026-08-31T10:08:00.000Z',
    terminalState: 'complete',
    totalScore: 88,
    scoreBreakdown: { critical: 50, timing: 10, efficiency: 0, treatment: 18, disposition: 10 },
    timeline: [],
    criteria: [],
    debrief: { summary: '', keyTeachingPoints: [], commonErrors: [] },
    references: [],
    replayHash: 'abcdef123456',
    ...over,
  };
}

describe('AttemptStore', () => {
  it('persists attempts and computes progress', async () => {
    const store = new AttemptStore(memoryKv());
    await store.save(summary({ totalScore: 60, terminalState: 'deteriorated' }));
    await store.save(
      summary({ attemptId: '22222222-2222-4222-8222-222222222222', totalScore: 88 }),
    );

    const progress = await store.progressForCase('stemi_anterior_001');
    expect(progress.attempts).toBe(2);
    expect(progress.completed).toBe(true); // second attempt completed
    expect(progress.bestScore).toBe(88);
    expect(progress.lastScore).toBe(88);
  });

  it('a replay with a new attemptId never overwrites prior history', async () => {
    const store = new AttemptStore(memoryKv());
    await store.save(summary({ totalScore: 50 }));
    await store.save(
      summary({ attemptId: '22222222-2222-4222-8222-222222222222', totalScore: 88 }),
    );
    expect((await store.listForCase('stemi_anterior_001')).length).toBe(2);
  });

  it('re-emitting the SAME attemptId updates in place (idempotent completion)', async () => {
    const store = new AttemptStore(memoryKv());
    await store.save(summary({ totalScore: 80 }));
    await store.save(summary({ totalScore: 80 }));
    expect((await store.listAll()).length).toBe(1);
  });

  it('storage failure is reported, not thrown', async () => {
    const store = new AttemptStore(memoryKv({ failWrites: true }));
    const result = await store.save(summary({}));
    expect(result.ok).toBe(false);
    expect(result.error).toContain('disk full');
  });

  it('deteriorated-only attempts count as attempted but not completed', async () => {
    const store = new AttemptStore(memoryKv());
    await store.save(summary({ terminalState: 'deteriorated', totalScore: 17 }));
    const progress = await store.progressForCase('stemi_anterior_001');
    expect(progress.attempts).toBe(1);
    expect(progress.completed).toBe(false);
  });
});
