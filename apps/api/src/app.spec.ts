import type { INestApplication } from '@nestjs/common';
import { Test } from '@nestjs/testing';
import request from 'supertest';
import { AppModule } from './app.module';

describe('Qaniva API (integration)', () => {
  let app: INestApplication;

  beforeAll(async () => {
    const moduleRef = await Test.createTestingModule({ imports: [AppModule] }).compile();
    app = moduleRef.createNestApplication();
    await app.init();
  });

  afterAll(async () => {
    await app.close();
  });

  it('GET /health -> ok', async () => {
    const res = await request(app.getHttpServer()).get('/health').expect(200);
    expect(res.body.status).toBe('ok');
    expect(res.body.service).toBe('qaniva-api');
  });

  it('GET /cases -> manifest includes the demo case', async () => {
    const res = await request(app.getHttpServer()).get('/cases').expect(200);
    const ids = res.body.cases.map((c: { id: string }) => c.id);
    expect(ids).toContain('demo_sync_bradycardia_001');
  });

  it('GET /cases/:id -> full validated case document', async () => {
    const res = await request(app.getHttpServer())
      .get('/cases/demo_sync_bradycardia_001')
      .expect(200);
    expect(res.body.schemaVersion).toBe(1);
    expect(res.body.metadata.fictional).toBe(true);
    expect(Array.isArray(res.body.availableActions)).toBe(true);
  });

  it('GET /cases/unknown -> 404', async () => {
    await request(app.getHttpServer()).get('/cases/nope_999').expect(404);
  });

  it('attempt lifecycle: start -> complete', async () => {
    const start = await request(app.getHttpServer())
      .post('/attempts')
      .send({ caseId: 'demo_sync_bradycardia_001', caseVersion: 1, difficulty: 'standard' })
      .expect(201);

    const attemptId: string = start.body.attemptId;
    expect(typeof start.body.seed).toBe('number');

    const summary = {
      attemptId,
      caseId: 'demo_sync_bradycardia_001',
      caseVersion: 1,
      seed: start.body.seed,
      startedAt: '2026-08-30T10:00:00.000Z',
      completedAt: '2026-08-30T10:08:00.000Z',
      terminalState: 'complete',
      totalScore: 80,
      scoreBreakdown: { critical: 40, timing: 20, efficiency: 0, treatment: 5, disposition: 15 },
      timeline: [
        {
          seq: 0,
          simTimeSec: 20,
          actionId: 'attach_monitor',
          label: 'Attach cardiac monitor',
          classification: 'correct',
        },
      ],
      replayHash: 'abcabcabc123',
    };

    await request(app.getHttpServer())
      .post(`/attempts/${attemptId}/complete`)
      .send(summary)
      .expect(201);

    const fetched = await request(app.getHttpServer()).get(`/attempts/${attemptId}`).expect(200);
    expect(fetched.body.status).toBe('completed');
    expect(fetched.body.summary.totalScore).toBe(80);
  });

  it('rejects a malformed attempt summary (400)', async () => {
    const start = await request(app.getHttpServer())
      .post('/attempts')
      .send({ caseId: 'demo_sync_bradycardia_001', caseVersion: 1 })
      .expect(201);
    await request(app.getHttpServer())
      .post(`/attempts/${start.body.attemptId}/complete`)
      .send({ totalScore: 'not a number' })
      .expect(400);
  });

  it('POST /analytics/events validates and counts', async () => {
    const res = await request(app.getHttpServer())
      .post('/analytics/events')
      .send({
        events: [
          {
            event: 'case_start',
            occurredAt: '2026-08-30T10:00:00.000Z',
            sessionId: 's1',
            source: 'mobile',
            appVersion: '0.1.0',
            caseId: 'demo_sync_bradycardia_001',
            caseVersion: 1,
          },
        ],
      })
      .expect(201);
    expect(res.body.accepted).toBe(1);
    expect(res.body.byType.case_start).toBe(1);
  });

  it('POST /ai/patient stays within disclosed facts', async () => {
    const res = await request(app.getHttpServer())
      .post('/ai/patient')
      .send({
        attemptId: '22222222-2222-4222-8222-222222222222',
        persona: 'anxious but cooperative',
        allowedFactIds: ['onset_1h'],
        disclosedFacts: [{ id: 'onset_1h', text: 'Symptoms started about an hour ago.' }],
        currentStateSummary: 'looks unwell',
        userMessage: 'When did this start?',
      })
      .expect(201);
    expect(res.body.reply.reply).toContain('hour');
    expect(res.body.reply.usedFactIds).toEqual(['onset_1h']);
  });
});
