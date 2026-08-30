# ADR-005 — Modular monolith backend (NestJS)

## Status

Accepted (2026-08-30).

## Context

The backend for the outreach MVP is thin: serve case metadata + versions, accept
attempt/event uploads, provide an AI gateway, ingest analytics, and be ready for
auth. Traffic is a 50–200-person beta. The team is TypeScript-first.

## Decision

A **modular monolith** using **NestJS + TypeScript**, one deployable. Domains are
Nest modules (`health`, `cases`, `attempts`, `analytics`, `ai`), wired in
`AppModule` — not separate services. PostgreSQL is the target datastore, using
JSONB for the flexible case graph and attempt logs; the foundation ships in-memory
stores and a designed schema (`apps/api/db/schema.sql`) so CI needs no database.

Request validation reuses the shared Zod contracts (`@qaniva/contracts`,
`@qaniva/analytics-schema`) rather than a parallel set of DTOs.

## Alternatives considered

- **FastAPI (Python).** A fine framework, but it splits the stack's language, and
  the shared TS contracts would need a second representation. No concrete
  advantage for this workload.
- **Microservices.** Rejected: operational overhead with no scaling need; the
  blueprint explicitly excludes microservices, Kubernetes, and event streaming for
  the MVP.
- **Serverless functions.** Viable later; premature now, and cold-start/tooling
  friction for a small team.
- **Supabase-only (no BFF).** Rejected as the primary path: Unity/RN should not
  talk to the DB directly; a thin BFF keeps secrets server-side.

## Consequences

- Auth is a future module; the config already parses `JWT_*` (auth-ready).
- Real AI providers plug in behind `AiProvider` without touching controllers.
- Moving to Postgres is a contained change (repositories already abstract storage);
  it is tracked as a backlog item, not a blocker.
