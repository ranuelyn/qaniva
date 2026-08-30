# @qaniva/api

Thin **modular monolith** backend (NestJS + TypeScript). See
[ADR-005](../../docs/adr/ADR-005-modular-monolith-backend.md).

## Scope in the MVP foundation

| Module | Endpoints | Storage |
| --- | --- | --- |
| `health` | `GET /health` | — |
| `cases` | `GET /cases`, `GET /cases/:id?version=` | schema-validated fixtures from `@qaniva/case-schema` |
| `attempts` | `POST /attempts`, `GET /attempts/:id`, `POST /attempts/:id/complete`, `POST /attempts/:id/events` | in-memory |
| `analytics` | `POST /analytics/events` | validate + count (no sink yet) |
| `ai` | `POST /ai/patient`, `POST /ai/debrief` | stub provider + safety gateway |

**Not implemented yet** (by design — see `docs/development/BACKLOG.md`): auth, Postgres
persistence (`db/schema.sql` is the target design), real AI providers, rate limiting.

## Run

```bash
pnpm --filter @qaniva/api start:dev
```

Config comes from environment variables — copy `.env.example` (repo root) to `.env`.
No secrets are committed.

## Safety boundary

`AiGatewayService` is the only place LLM calls happen. It validates structured
output, rejects any patient reply citing a fact id outside the allowed set,
rejects any debrief that changes the score, and falls back to the deterministic
`StubAiProvider` on any failure. See `docs/architecture/ai-boundary.md`.
