-- Qaniva backend — target PostgreSQL schema (DESIGN ONLY).
-- Not wired in the MVP foundation: the API currently uses in-memory stores so
-- CI needs no database. Promote to a migration (issue QAN-024) when auth/persistence
-- work begins. Uses JSONB for the flexible case graph and attempt logs (ADR-005).

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- --- users / auth (auth itself is a later module) -------------------------
CREATE TABLE app_user (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email         CITEXT UNIQUE,           -- NULL for guest sessions
    display_name  TEXT,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- --- case content (versioned artifacts) --------------------------------
CREATE TABLE case_version (
    case_id               TEXT        NOT NULL,
    version               INTEGER     NOT NULL,
    schema_version        INTEGER     NOT NULL,
    clinical_review_status TEXT       NOT NULL DEFAULT 'not_reviewed'
        CHECK (clinical_review_status IN ('not_reviewed','in_review','approved')),
    published             BOOLEAN     NOT NULL DEFAULT false,
    document              JSONB       NOT NULL,   -- the full schema-validated case.json
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (case_id, version)
);
CREATE INDEX case_version_published_idx ON case_version (published, case_id);

-- --- attempts + event log --------------------------------------------
CREATE TABLE attempt (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id        UUID REFERENCES app_user (id),
    case_id        TEXT        NOT NULL,
    case_version   INTEGER     NOT NULL,
    difficulty     TEXT        NOT NULL DEFAULT 'standard',
    seed           BIGINT      NOT NULL,          -- required for deterministic replay
    status         TEXT        NOT NULL DEFAULT 'in_progress'
        CHECK (status IN ('in_progress','completed','aborted')),
    summary        JSONB,                         -- AttemptSummary contract
    replay_hash    TEXT,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at   TIMESTAMPTZ,
    FOREIGN KEY (case_id, case_version) REFERENCES case_version (case_id, version)
);
CREATE INDEX attempt_user_idx ON attempt (user_id, created_at DESC);

CREATE TABLE attempt_event (
    attempt_id  UUID     NOT NULL REFERENCES attempt (id) ON DELETE CASCADE,
    seq         INTEGER  NOT NULL,
    payload     JSONB    NOT NULL,   -- AttemptEvent: simTime, actionId, hashes, triggeredRules, scoreDelta
    PRIMARY KEY (attempt_id, seq)
);

-- --- analytics events (unified RN + Unity) ---------------------------
CREATE TABLE analytics_event (
    id           BIGSERIAL PRIMARY KEY,
    event        TEXT        NOT NULL,
    attempt_id   UUID,
    session_id   TEXT        NOT NULL,
    source       TEXT        NOT NULL CHECK (source IN ('mobile','unity','backend')),
    occurred_at  TIMESTAMPTZ NOT NULL,
    received_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    payload      JSONB       NOT NULL
);
CREATE INDEX analytics_event_attempt_idx ON analytics_event (attempt_id);
CREATE INDEX analytics_event_name_time_idx ON analytics_event (event, occurred_at);

-- --- AI call audit (prompt/model/latency/safety) -------------------
CREATE TABLE ai_call (
    id                   BIGSERIAL PRIMARY KEY,
    attempt_id           UUID REFERENCES attempt (id) ON DELETE SET NULL,
    kind                 TEXT NOT NULL CHECK (kind IN ('patient','debrief')),
    provider             TEXT NOT NULL,
    model                TEXT,
    prompt_version       TEXT,
    latency_ms           INTEGER,
    schema_valid         BOOLEAN NOT NULL,
    used_fallback        BOOLEAN NOT NULL,
    safety_flag          TEXT,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT now()
);
