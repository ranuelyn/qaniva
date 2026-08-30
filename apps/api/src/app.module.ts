import { Module } from '@nestjs/common';
import { HealthModule } from './health/health.module';
import { CasesModule } from './cases/cases.module';
import { AttemptsModule } from './attempts/attempts.module';
import { AnalyticsModule } from './analytics/analytics.module';
import { AiModule } from './ai/ai.module';

/**
 * Modular monolith (ADR-005). Each domain is a Nest module; they are wired here,
 * not split into services. Auth is a future module (blueprint §19 "auth-ready").
 */
@Module({
  imports: [HealthModule, CasesModule, AttemptsModule, AnalyticsModule, AiModule],
})
export class AppModule {}
