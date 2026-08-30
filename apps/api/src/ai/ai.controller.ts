import { Body, Controller, Post } from '@nestjs/common';
import { ZodValidationPipe } from '../common/zod-validation.pipe';
import { AiGatewayService } from './ai-gateway.service';
import {
  patientTurnContextSchema,
  debriefContextSchema,
  type DebriefContext,
  type PatientTurnContext,
} from './ai.types';

@Controller('ai')
export class AiController {
  constructor(private readonly gateway: AiGatewayService) {}

  @Post('patient')
  patient(@Body(new ZodValidationPipe(patientTurnContextSchema)) ctx: PatientTurnContext) {
    return this.gateway.patientReply(ctx);
  }

  @Post('debrief')
  debrief(@Body(new ZodValidationPipe(debriefContextSchema)) ctx: DebriefContext) {
    return this.gateway.debriefNarrative(ctx);
  }
}
