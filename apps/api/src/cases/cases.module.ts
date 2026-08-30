import { Module } from '@nestjs/common';
import { CasesController } from './cases.controller';
import { CasesService } from './cases.service';
import { CaseRepository } from './case-repository';

@Module({
  controllers: [CasesController],
  providers: [CasesService, CaseRepository],
  exports: [CasesService],
})
export class CasesModule {}
