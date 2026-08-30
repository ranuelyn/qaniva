# Git workflow

## Branches

- `main` is protected and always green. Do not commit directly to it for feature
  work; branch off it.
- Branch names: `feat/<area>-<short>`, `fix/<area>-<short>`, `docs/<short>`,
  `chore/<short>`. Example: `feat/engine-scoring-timing-window`.
- Do not force-push shared branches. If `main` moved, rebase your branch.

## Commits

**Conventional Commits.** Type + optional scope + imperative summary:

```
feat(engine): add timing-window multiplier to scoring
fix(bridge): reject payloads with an unknown protocol version
docs(adr): record modular-monolith backend decision
test(api): cover attempt-summary validation failure
chore(ci): add dotnet job for the clinical engine
```

Keep commits **small and atomic** — one concern each. Don't mix an engine change,
a UI change, and a backend change in one commit.

## Pull requests

A PR includes:

- what changed and why (link the relevant ADR/skill);
- the command output for the checks in its area (see `testing.md`);
- for a clinical change: the `metadata.clinicalReview` update / reviewer note;
- doc/skill/ADR updates in the **same** PR.

Merge only on green CI + review. Squash or rebase-merge to keep `main` linear.

## What never gets committed

Secrets, `.env`, generated output (`node_modules/`, `dist/` except intentional
package builds, `bin/`, `obj/`, Unity `Library/Temp/Logs`, built DLLs), or
placeholder code pretending to be real. See `AGENTS.md` §5.

## Releases (later)

Tag `vMAJOR.MINOR.PATCH`. The Release Agent owns the TestFlight / Play testing
checklist and crash-symbol upload (backlog QAN-030).
