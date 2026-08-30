import { Module } from '@nestjs/common';
import { loadConfig } from '../config';
import { AiController } from './ai.controller';
import { AiGatewayService } from './ai-gateway.service';
import { AI_PROVIDER, type AiProvider } from './ai.types';
import { StubAiProvider } from './providers/stub-provider';

/**
 * Provider selection is config-driven (AI_PROVIDER env). Only the deterministic
 * stub ships in the foundation; real adapters (openai/anthropic) are added here
 * behind the same interface, backend-only, never exposed to clients.
 */
@Module({
  controllers: [AiController],
  providers: [
    AiGatewayService,
    {
      provide: AI_PROVIDER,
      useFactory: (): AiProvider => {
        const { AI_PROVIDER: name } = loadConfig();
        switch (name) {
          case 'stub':
            return new StubAiProvider();
          default:
            // Real adapters not implemented in the foundation — fail safe to stub.
            return new StubAiProvider();
        }
      },
    },
    {
      provide: 'AI_TIMEOUT_MS',
      useFactory: () => loadConfig().AI_REQUEST_TIMEOUT_MS,
    },
  ],
  exports: [AiGatewayService],
})
export class AiModule {}
