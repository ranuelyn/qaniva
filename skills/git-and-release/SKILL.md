# Skill: git-and-release

## Purpose

Small, reviewable commits; a green `main`; a repeatable path to a beta.

## When to use

Every commit and PR; CI changes; anything release-related.

## Inputs

- `docs/development/git-workflow.md`
- `.github/workflows/ci.yml`
- `docs/development/BACKLOG.md` (QAN-024, QAN-025, QAN-030, QAN-036 for release)

## Non-negotiable rules

1. **Conventional Commits** (`feat`, `fix`, `docs`, `chore`, `test`, `refactor`,
   optional scope). Imperative summary. One concern per commit.
2. Branch off `main`; never force-push shared history. Rebase if `main` moved.
3. Merge only on green CI + review. Keep `main` linear (squash / rebase-merge).
4. Never commit secrets or generated output. `.env` is ignored; `.env.example` is
   the template.
5. CI must actually run the checks — don't add a workflow that skips the failing
   step. A red build is not "done".
6. Unity CI that needs a license/GPU is **documented** (QAN-024), not committed as
   a broken job.
7. Commit message trailer for AI-authored commits:
   `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`.

## Workflow

1. `git switch -c feat/<area>-<short>`.
2. Make the change + tests + doc updates together.
3. `pnpm run ci` · `pnpm run validate:cases` · `cd clinical-core && dotnet test`.
4. Commit atomically with a Conventional Commit message.
5. PR: what/why, command output, ADR/skill links, review notes.
6. On green + approval: rebase-merge.

## Validation

- `git status` clean before and after.
- CI green on the PR (TS workspace job + dotnet engine job).
- `pnpm run format:check` and `dotnet format --verify-no-changes` clean.

## Done criteria

Atomic Conventional commits; branch off `main`; CI green; no secrets/junk; docs in
the same PR; `main` stays linear.

## Common failure modes

- One giant commit spanning engine + UI + backend.
- Force-pushing over a teammate's history.
- A CI job that `continue-on-error`s the real test.
- Committing `node_modules/`, `bin/`, `obj/`, Unity `Library/`, or a built DLL.
- Tagging a release without the TestFlight/Play checklist (QAN-030).
