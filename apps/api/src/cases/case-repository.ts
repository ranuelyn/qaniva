import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { Injectable } from '@nestjs/common';
import { validateCase } from '@qaniva/case-schema';

export interface CaseManifestEntry {
  id: string;
  version: number;
  title: string;
  chiefComplaint: string;
  specialty: string;
  estimatedMinutes: number;
  clinicalReviewStatus: string;
}

/**
 * Loads schema-validated case fixtures from `@qaniva/case-schema`. In the MVP
 * foundation this is the case store; a Postgres-backed store with a publishing
 * workflow replaces it later (see db/schema.sql and the backlog).
 */
@Injectable()
export class CaseRepository {
  private readonly byKey = new Map<string, unknown>();
  private readonly manifest: CaseManifestEntry[] = [];

  constructor() {
    this.load();
  }

  list(): CaseManifestEntry[] {
    return [...this.manifest];
  }

  get(id: string, version: number): unknown | undefined {
    return this.byKey.get(`${id}@${version}`);
  }

  getLatest(id: string): unknown | undefined {
    const latest = this.manifest
      .filter((m) => m.id === id)
      .sort((a, b) => b.version - a.version)[0];
    return latest ? this.get(latest.id, latest.version) : undefined;
  }

  private load(): void {
    const fixturesDir = this.resolveFixturesDir();
    if (!fixturesDir || !existsSync(fixturesDir)) {
      return;
    }
    for (const caseDir of readdirSync(fixturesDir, { withFileTypes: true })) {
      if (!caseDir.isDirectory()) continue;
      const caseRoot = join(fixturesDir, caseDir.name);
      for (const versionDir of readdirSync(caseRoot, { withFileTypes: true })) {
        if (!versionDir.isDirectory()) continue;
        const file = join(caseRoot, versionDir.name, 'case.json');
        if (!existsSync(file)) continue;
        const data = JSON.parse(readFileSync(file, 'utf8')) as Record<string, unknown>;
        const result = validateCase(data);
        if (!result.valid) {
          throw new Error(
            `Refusing to serve invalid case ${file}: ${JSON.stringify(result.issues)}`,
          );
        }
        this.register(data);
      }
    }
  }

  private register(data: Record<string, unknown>): void {
    const id = data.id as string;
    const version = data.version as number;
    const metadata = data.metadata as Record<string, unknown>;
    const review = metadata.clinicalReview as Record<string, unknown>;
    this.byKey.set(`${id}@${version}`, data);
    this.manifest.push({
      id,
      version,
      title: metadata.title as string,
      chiefComplaint: metadata.chiefComplaint as string,
      specialty: metadata.specialty as string,
      estimatedMinutes: metadata.estimatedMinutes as number,
      clinicalReviewStatus: review.status as string,
    });
  }

  private resolveFixturesDir(): string | undefined {
    try {
      const pkg = require.resolve('@qaniva/case-schema/package.json');
      return join(dirname(pkg), 'fixtures');
    } catch {
      return undefined;
    }
  }
}
