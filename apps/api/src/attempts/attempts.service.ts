import { randomInt, randomUUID } from 'node:crypto';
import { Injectable, NotFoundException } from '@nestjs/common';
import type { AttemptSummary } from '@qaniva/contracts';

export interface AttemptRecord {
  attemptId: string;
  caseId: string;
  caseVersion: number;
  difficulty: 'standard' | 'hard';
  seed: number;
  status: 'in_progress' | 'completed';
  createdAt: string;
  summary?: AttemptSummary;
  eventCount: number;
}

/**
 * In-memory attempt store for the MVP foundation. Replaced by a Postgres-backed
 * store (see apps/api/db/schema.sql) — the JSONB `summary`/`events` columns are
 * already designed. No schema-breaking change is expected for clients.
 */
@Injectable()
export class AttemptsService {
  private readonly attempts = new Map<string, AttemptRecord>();

  start(input: {
    caseId: string;
    caseVersion: number;
    difficulty: 'standard' | 'hard';
  }): AttemptRecord {
    const record: AttemptRecord = {
      attemptId: randomUUID(),
      caseId: input.caseId,
      caseVersion: input.caseVersion,
      difficulty: input.difficulty,
      // Deterministic replay needs a stored seed; the client echoes it into START_SIMULATION.
      seed: randomInt(0, 2 ** 31 - 1),
      status: 'in_progress',
      createdAt: new Date().toISOString(),
      eventCount: 0,
    };
    this.attempts.set(record.attemptId, record);
    return record;
  }

  get(attemptId: string): AttemptRecord {
    const record = this.attempts.get(attemptId);
    if (!record) {
      throw new NotFoundException(`Attempt "${attemptId}" not found`);
    }
    return record;
  }

  complete(attemptId: string, summary: AttemptSummary): AttemptRecord {
    const record = this.get(attemptId);
    record.summary = summary;
    record.status = 'completed';
    return record;
  }

  appendEvents(attemptId: string, count: number): AttemptRecord {
    const record = this.get(attemptId);
    record.eventCount += count;
    return record;
  }
}
