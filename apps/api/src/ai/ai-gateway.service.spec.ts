import { AiGatewayService } from './ai-gateway.service';
import type { AiProvider, DebriefContext, PatientTurnContext } from './ai.types';

const patientCtx: PatientTurnContext = {
  attemptId: '22222222-2222-4222-8222-222222222222',
  persona: 'test',
  allowedFactIds: ['onset_1h'],
  disclosedFacts: [{ id: 'onset_1h', text: 'Started an hour ago.' }],
  currentStateSummary: '',
  userMessage: 'when did it start?',
  safetyPolicyVersion: 'v1',
};

const debriefCtx: DebriefContext = {
  attemptId: '22222222-2222-4222-8222-222222222222',
  timeline: [{ simTimeSec: 10, actionId: 'a', classification: 'correct' }],
  totalScore: 42,
  missedCriterionIds: [],
  approvedEvidenceNotes: [],
};

function gatewayWith(provider: AiProvider): AiGatewayService {
  return new AiGatewayService(provider, 2000);
}

describe('AiGatewayService safety boundary', () => {
  it('falls back when the provider cites a fact id outside the allowed set', async () => {
    const rogue: AiProvider = {
      name: 'rogue',
      patientReply: async () => ({
        reply: 'You also have crushing chest pain radiating to your arm.',
        usedFactIds: ['invented_symptom'],
        outOfScope: false,
      }),
      debriefNarrative: async () => ({ narrative: 'x', reportedScore: 42 }),
    };
    const { reply, usedFallback } = await gatewayWith(rogue).patientReply(patientCtx);
    expect(usedFallback).toBe(true);
    expect(reply.usedFactIds.every((id) => ['onset_1h'].includes(id))).toBe(true);
  });

  it('falls back when the debrief tries to change the score', async () => {
    const cheater: AiProvider = {
      name: 'cheater',
      patientReply: async () => ({ reply: 'ok', usedFactIds: [], outOfScope: false }),
      debriefNarrative: async () => ({ narrative: 'You actually scored 100.', reportedScore: 100 }),
    };
    const { narrative, usedFallback } = await gatewayWith(cheater).debriefNarrative(debriefCtx);
    expect(usedFallback).toBe(true);
    expect(narrative.reportedScore).toBe(42);
  });

  it('falls back on provider timeout', async () => {
    const slow: AiProvider = {
      name: 'slow',
      patientReply: () => new Promise(() => {}),
      debriefNarrative: () => new Promise(() => {}),
    };
    const gw = new AiGatewayService(slow, 20);
    const { usedFallback } = await gw.patientReply(patientCtx);
    expect(usedFallback).toBe(true);
  });

  it('passes through a well-behaved provider response', async () => {
    const good: AiProvider = {
      name: 'good',
      patientReply: async () => ({
        reply: 'It started about an hour ago.',
        usedFactIds: ['onset_1h'],
        outOfScope: false,
      }),
      debriefNarrative: async () => ({ narrative: 'Solid run.', reportedScore: 42 }),
    };
    const gw = gatewayWith(good);
    expect((await gw.patientReply(patientCtx)).usedFallback).toBe(false);
    expect((await gw.debriefNarrative(debriefCtx)).usedFallback).toBe(false);
  });
});
