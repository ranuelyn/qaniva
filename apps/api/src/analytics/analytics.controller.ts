import { Body, Controller, Post } from '@nestjs/common';
import { z } from 'zod';
import { analyticsEventSchema } from '@qaniva/analytics-schema';
import { ZodValidationPipe } from '../common/zod-validation.pipe';

const batchSchema = z.object({
  events: z.array(analyticsEventSchema).min(1).max(100),
});
type BatchDto = z.infer<typeof batchSchema>;

/**
 * Accepts the unified RN + Unity analytics event batch (blueprint §21). The MVP
 * foundation just validates and counts; a real sink (PostHog / first-party) is
 * wired later via ANALYTICS_SINK.
 */
@Controller('analytics')
export class AnalyticsController {
  @Post('events')
  ingest(@Body(new ZodValidationPipe(batchSchema)) body: BatchDto) {
    const byType: Record<string, number> = {};
    for (const evt of body.events) {
      byType[evt.event] = (byType[evt.event] ?? 0) + 1;
    }
    return { accepted: body.events.length, byType };
  }
}
