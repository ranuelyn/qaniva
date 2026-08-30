import { Injectable, NotFoundException } from '@nestjs/common';
import { CaseRepository, type CaseManifestEntry } from './case-repository';

@Injectable()
export class CasesService {
  constructor(private readonly repo: CaseRepository) {}

  listManifest(): CaseManifestEntry[] {
    return this.repo.list();
  }

  getCase(id: string, version?: number): unknown {
    const found = version ? this.repo.get(id, version) : this.repo.getLatest(id);
    if (!found) {
      throw new NotFoundException(`Case "${id}"${version ? ` v${version}` : ''} not found`);
    }
    return found;
  }
}
