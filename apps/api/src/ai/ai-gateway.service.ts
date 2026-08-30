import { Inject, Injectable, Logger } from '@nestjs/common';
import {
  AI_PROVIDER,
  patientReplySchema,
  debriefNarrativeSchema,
  type AiProvider,
  type DebriefContext,
  type DebriefNarrative,
  type PatientReply,
  type PatientTurnContext,
} from './ai.types';
import { StubAiProvider } from './providers/stub-provider';

/**
 * The one place LLM calls are allowed. It:
 *  - calls the configured provider with a timeout,
 *  - validates the structured output,
 *  - rejects any patient reply that cites a fact id outside the allowed set,
 *  - rejects any debrief whose reportedScore != the score it was given,
 *  - falls back to the deterministic StubAiProvider on any failure so the
 *    simulation never stalls.
 */
@Injectable()
export class AiGatewayService {
  private readonly logger = new Logger(AiGatewayService.name);
  private readonly fallback = new StubAiProvider();

  constructor(
    @Inject(AI_PROVIDER) private readonly provider: AiProvider,
    @Inject('AI_TIMEOUT_MS') private readonly timeoutMs: number,
  ) {}

  async patientReply(
    ctx: PatientTurnContext,
  ): Promise<{ reply: PatientReply; usedFallback: boolean }> {
    const allowed = new Set([...ctx.allowedFactIds, ...ctx.disclosedFacts.map((f) => f.id)]);
    try {
      const raw = await this.withTimeout(this.provider.patientReply(ctx));
      const parsed = patientReplySchema.parse(raw);
      const leaked = parsed.usedFactIds.filter((id) => !allowed.has(id));
      if (leaked.length > 0) {
        throw new Error(`patient reply cited disallowed fact ids: ${leaked.join(', ')}`);
      }
      return { reply: parsed, usedFallback: false };
    } catch (err) {
      this.logger.warn(`patient reply fallback: ${(err as Error).message}`);
      return { reply: await this.fallback.patientReply(ctx), usedFallback: true };
    }
  }

  async debriefNarrative(
    ctx: DebriefContext,
  ): Promise<{ narrative: DebriefNarrative; usedFallback: boolean }> {
    try {
      const raw = await this.withTimeout(this.provider.debriefNarrative(ctx));
      const parsed = debriefNarrativeSchema.parse(raw);
      if (parsed.reportedScore !== ctx.totalScore) {
        throw new Error('debrief attempted to change the score');
      }
      return { narrative: parsed, usedFallback: false };
    } catch (err) {
      this.logger.warn(`debrief fallback: ${(err as Error).message}`);
      return { narrative: await this.fallback.debriefNarrative(ctx), usedFallback: true };
    }
  }

  private async withTimeout<T>(p: Promise<T>): Promise<T> {
    let timer: NodeJS.Timeout | undefined;
    const timeout = new Promise<T>((_, reject) => {
      timer = setTimeout(() => reject(new Error('AI provider timed out')), this.timeoutMs);
      timer.unref?.();
    });
    try {
      return await Promise.race([p, timeout]);
    } finally {
      if (timer) {
        clearTimeout(timer);
      }
    }
  }
}
