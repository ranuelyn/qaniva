import { Controller, Get } from '@nestjs/common';

@Controller()
export class HealthController {
  private readonly startedAt = Date.now();

  @Get('health')
  health(): { status: 'ok'; uptimeSec: number; service: string } {
    return {
      status: 'ok',
      uptimeSec: Math.round((Date.now() - this.startedAt) / 1000),
      service: 'qaniva-api',
    };
  }
}
