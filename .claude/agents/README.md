# Agent definitions

These files define the specialist development agents from
[`docs/agents/operating-model.md`](../../docs/agents/operating-model.md). Each is a
focused system prompt that points at the relevant skill(s) under `skills/` and
enforces the boundaries in `AGENTS.md`.

Use the smallest agent that fits the task. Combine roles for small changes. Never
give one agent "build Qaniva" scope.

| File | Role |
| --- | --- |
| `director.md` | Director / Orchestrator |
| `product-architect.md` | Product Architect |
| `mobile-agent.md` | Mobile (React Native) |
| `unity-agent.md` | Unity (C# / URP) |
| `clinical-engine-agent.md` | Clinical Engine (pure C#) |
| `backend-agent.md` | Backend (NestJS) |
| `clinical-content-agent.md` | Clinical Content (case authoring) |
| `qa-skeptic-agent.md` | QA / Skeptic |
| `release-agent.md` | Release / CI |
