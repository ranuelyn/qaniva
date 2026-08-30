# AGENTS.md — Qaniva working contract

**Read this before touching the repo.** It is the operating contract for every
human and AI coding agent. It is short on purpose. Details live in `docs/`.

---

## 1. What Qaniva is

A **mobile-first, 3D clinical decision simulation platform**. A learner picks a
fictional case, works the patient in a full-screen 3D scene, and reviews a timeline
of every decision. Canonical product spec: [`docs/QANIVA_MVP_BLUEPRINT.md`](docs/QANIVA_MVP_BLUEPRINT.md).

## 2. Current MVP scope

Bootstrapping the foundation. In scope now: the architecture, the deterministic
engine skeleton + tests, the versioned case schema + one fictional demo case, the
RN↔Unity typed bridge contract, a thin backend, the AI boundary, docs, skills, CI.
Out of scope now: production 3D assets, real medical cases, educator dashboard,
payments, real LLM providers, multiplayer, offline sync. See
[`docs/development/BACKLOG.md`](docs/development/BACKLOG.md) §"NOT v1".

## 3. Architecture boundaries (do not blur these)

```
Clinical Engine (pure C#)  = the single source of clinical truth
Unity 6 + URP              = presentation / simulation renderer
React Native + TypeScript  = product / mobile shell
AI (backend gateway)       = constrained language / tutoring layer
```

- **`clinical-core/`** — pure C# (`netstandard2.1`), zero Unity references. Owns
  `CaseDefinition`, `PatientState`, rules, timeline, scoring, replay. Deterministic:
  same case + same actions + same seed ⇒ same timeline, state, and score.
- **`unity/QanivaSimulation/`** — renders snapshots the engine produces. Consumes
  the engine as a compiled DLL via `IClinicalRuntime`. No clinical logic in
  `MonoBehaviour`, `Animator` callbacks, or scene objects.
- **`apps/mobile/`** — navigation, screens, product state, the Unity host screen,
  the typed bridge client. Produces no clinical rules; contains no Unity scene logic.
- **`apps/api/`** — thin modular monolith: cases, attempts, analytics, AI gateway.
  Carries no client secrets.
- **`packages/contracts`** — the RN↔Unity bridge protocol, single source of truth.
  `unity/.../Bridge/BridgeProtocol.cs` mirrors it and a test enforces parity.

## 4. Non-negotiable rules

1. **Never allow an LLM response to mutate canonical simulation state.** Vitals,
   state transitions, drug effects, diagnoses, and scores come only from the
   deterministic engine. The AI gateway validates structured output and falls back
   to a deterministic template on any violation.
2. **Never change a clinical rule merely to make a UI or a test easier.** If a test
   is inconvenient, fix the test or the design, not the rule. Clinical-number
   changes need a clinician review sign-off (`metadata.clinicalReview.status`).
3. **The engine stays Unity-free.** No `UnityEngine` import in `clinical-core/`.
4. **Determinism is a release gate.** Golden replay tests must pass. Regenerating a
   golden file (`UPDATE_GOLDEN=1`) requires a human to review the diff.
5. **Cases are data, not code.** Author cases as schema-validated JSON. Do not hard-
   code case logic in C# classes.
6. **The bridge is versioned and typed.** No ad-hoc string messages between RN and
   Unity. Change `packages/contracts` and the C# mirror together; bump
   `PROTOCOL_VERSION` on any breaking change.
7. **No secrets in the repo.** Use `.env.example`. Synthetic/fictional case data only.

## 5. Forbidden shortcuts

- Placeholder code that pretends to work ("returns a fake success"). If something is
  a stub, name it a stub and make its limits obvious.
- Deleting or `.skip`-ing a failing test to get green.
- Committing generated files (`Library/`, `node_modules/`, `bin/`, `obj/`, built
  DLLs, `dist/` — except where a package intentionally ships `dist`).
- Copying a contract into a second place instead of importing the shared one.
- Widening `additionalProperties` in the case schema to sneak a field through.

## 6. Test expectations

Every change ships with the checks for its area green:

| Area | Commands |
| --- | --- |
| TS workspace | `pnpm run ci` (format:check, lint, typecheck, test) |
| Case schema | `pnpm run validate:cases` |
| Clinical engine | `cd clinical-core && dotnet test` |
| Unity C# | EditMode Test Runner (needs the Editor — see `unity/QanivaSimulation/README.md`) |

CI must be green before a change is "done". See `docs/development/testing.md`.

## 7. Repo conventions

- Package manager: **pnpm** (workspaces). Node ≥ 20.18.
- TypeScript strict. Prettier + ESLint (flat config at the root).
- C#: `dotnet format`; warnings are errors in `clinical-core/`.
- Commits: **Conventional Commits** (`feat:`, `fix:`, `docs:`, `chore:`, `test:`,
  `refactor:`). Small, atomic commits. Branch off `main`; do not force-push shared
  history.
- Keep `docs/` and `skills/` updated in the same PR as the change they describe.

## 8. Definition of Done

- The change is scoped to one area; the engine, UI, and backend are not all
  rewritten in one commit.
- The relevant checks above are green, and the command output is in the PR.
- New behaviour has a test. New clinical numbers have a review note.
- Docs/ADR/skill updated if the change affects them.
- No secret, no generated junk, no placeholder masquerading as real.

## 9. Where to look

| You are working on… | Start here |
| --- | --- |
| Architecture / a new ADR | `docs/architecture/`, `docs/adr/` |
| The engine | `docs/architecture/clinical-engine.md`, `skills/deterministic-clinical-engine/SKILL.md` |
| A case | `docs/clinical/case-authoring-guide.md`, `skills/case-authoring/SKILL.md` |
| The bridge | `docs/architecture/rn-unity-boundary.md`, `skills/unity-rn-bridge/SKILL.md` |
| Mobile | `skills/react-native-mobile/SKILL.md` |
| Unity | `skills/unity-mobile/SKILL.md`, `unity/QanivaSimulation/README.md` |
| The AI layer | `docs/architecture/ai-boundary.md`, `skills/clinical-safety/SKILL.md` |
| How agents divide work | `docs/agents/operating-model.md` |
