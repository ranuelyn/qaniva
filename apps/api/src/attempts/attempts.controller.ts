import { Body, Controller, Get, Param, Post } from '@nestjs/common';
import { z } from 'zod';
import { attemptSummarySchema } from '@qaniva/contracts';
import { ZodValidationPipe } from '../common/zod-validation.pipe';
import { AttemptsService } from './attempts.service';

const startAttemptSchema = z.object({
  caseId: z.string().min(1),
  caseVersion: z.number().int().positive(),
  difficulty: z.enum(['standard', 'hard']).default('standard'),
});
type StartAttemptDto = z.infer<typeof startAttemptSchema>;

const appendEventsSchema = z.object({
  // The full event objects are persisted opaquely; we only need the count here.
  events: z.array(z.record(z.unknown())).min(1),
});
type AppendEventsDto = z.infer<typeof appendEventsSchema>;

@Controller('attempts')
export class AttemptsController {
  constructor(private readonly attempts: AttemptsService) {}

  @Post()
  start(@Body(new ZodValidationPipe(startAttemptSchema)) body: StartAttemptDto) {
    const record = this.attempts.start(body);
    return {
      attemptId: record.attemptId,
      seed: record.seed,
      caseId: record.caseId,
      caseVersion: record.caseVersion,
    };
  }

  @Get(':id')
  get(@Param('id') id: string) {
    return this.attempts.get(id);
  }

  @Post(':id/complete')
  complete(
    @Param('id') id: string,
    @Body(new ZodValidationPipe(attemptSummarySchema)) summary: unknown,
  ) {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const record = this.attempts.complete(id, summary as any);
    return { attemptId: record.attemptId, status: record.status };
  }

  @Post(':id/events')
  appendEvents(
    @Param('id') id: string,
    @Body(new ZodValidationPipe(appendEventsSchema)) body: AppendEventsDto,
  ) {
    const record = this.attempts.appendEvents(id, body.events.length);
    return { attemptId: record.attemptId, eventCount: record.eventCount };
  }
}
