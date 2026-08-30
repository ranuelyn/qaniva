import { Controller, Get, Param, Query } from '@nestjs/common';
import { CasesService } from './cases.service';

@Controller('cases')
export class CasesController {
  constructor(private readonly cases: CasesService) {}

  /** Lightweight manifest for the RN case library. */
  @Get()
  list() {
    return { cases: this.cases.listManifest() };
  }

  /** Full case document for the simulation runtime. `?version=` optional (defaults to latest). */
  @Get(':id')
  getOne(@Param('id') id: string, @Query('version') version?: string) {
    return this.cases.getCase(id, version ? Number(version) : undefined);
  }
}
