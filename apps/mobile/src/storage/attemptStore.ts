import type { AttemptSummary } from '@qaniva/contracts';

/**
 * Local MVP persistence for completed attempts (no backend/auth in scope).
 * The store is deliberately abstracted over a tiny key-value interface so unit
 * tests inject an in-memory map while the app binds AsyncStorage
 * (see asyncStorageKv.ts). All failures are non-fatal: gameplay never blocks
 * on persistence — callers receive `{ ok: false }` and surface a small notice.
 */

export interface KeyValueStore {
  getItem(key: string): Promise<string | null>;
  setItem(key: string, value: string): Promise<void>;
}

/** One persisted attempt: the full deterministic summary + bookkeeping. */
export interface StoredAttempt {
  summary: AttemptSummary;
  /** ISO timestamp when the attempt was persisted locally. */
  savedAt: string;
}

export interface CaseProgress {
  caseId: string;
  attempts: number;
  completed: boolean;
  bestScore: number | null;
  lastScore: number | null;
  lastOutcome: string | null;
  lastAttemptedAt: string | null;
}

const STORE_KEY = 'qaniva.attempts.v1';
/** Cap so local storage cannot grow unbounded; oldest attempts are dropped. */
const MAX_ATTEMPTS = 100;

/** Outcomes that count as "completed the case" for progress purposes. */
const COMPLETED_OUTCOMES = new Set(['complete', 'partial', 'admit', 'discharge']);

export interface SaveResult {
  ok: boolean;
  error?: string;
}

export class AttemptStore {
  constructor(private readonly kv: KeyValueStore) {}

  async listAll(): Promise<StoredAttempt[]> {
    try {
      const raw = await this.kv.getItem(STORE_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as StoredAttempt[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  async listForCase(caseId: string): Promise<StoredAttempt[]> {
    return (await this.listAll()).filter((a) => a.summary.caseId === caseId);
  }

  /**
   * Persist a completed attempt. Idempotent per attemptId (a re-emitted summary
   * updates in place instead of duplicating); never overwrites OTHER attempts —
   * replays always arrive with a fresh attemptId.
   */
  async save(summary: AttemptSummary): Promise<SaveResult> {
    try {
      const all = await this.listAll();
      const withoutSelf = all.filter((a) => a.summary.attemptId !== summary.attemptId);
      withoutSelf.push({ summary, savedAt: new Date().toISOString() });
      const trimmed = withoutSelf.slice(-MAX_ATTEMPTS);
      await this.kv.setItem(STORE_KEY, JSON.stringify(trimmed));
      return { ok: true };
    } catch (e) {
      return { ok: false, error: e instanceof Error ? e.message : 'unknown storage error' };
    }
  }

  async progressForCase(caseId: string): Promise<CaseProgress> {
    const attempts = await this.listForCase(caseId);
    const scores = attempts.map((a) => a.summary.totalScore);
    const last = attempts[attempts.length - 1];
    return {
      caseId,
      attempts: attempts.length,
      completed: attempts.some((a) => COMPLETED_OUTCOMES.has(a.summary.terminalState)),
      bestScore: scores.length ? Math.max(...scores) : null,
      lastScore: last ? last.summary.totalScore : null,
      lastOutcome: last ? last.summary.terminalState : null,
      lastAttemptedAt: last ? last.summary.completedAt : null,
    };
  }
}
