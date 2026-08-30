# Skill: coding-standards

## Purpose

One consistent style across TS and C# so diffs are about substance.

## When to use

Every change. Especially before opening a PR.

## Inputs

- `eslint.config.mjs`, `.prettierrc.json`, `tsconfig.base.json`
- `clinical-core/Directory.Build.props`, `.editorconfig`

## Non-negotiable rules

### TypeScript

1. Strict mode stays on (`noUncheckedIndexedAccess`, `noImplicitOverride`,
   `exactOptionalPropertyTypes` where enabled). Don't disable a strict flag to
   dodge an error.
2. `import type` for type-only imports (`consistent-type-imports`) — except
   `apps/api/**` where NestJS DI needs runtime type refs.
3. No `any`. Use `unknown` + a narrow, or a real type. No non-null `!` to silence
   a real possibility.
4. Prefix intentionally-unused with `_`. `console` only `warn`/`error` in app code.
5. Shared cross-package types come from `@qaniva/*` packages, never re-declared.

### C# (`clinical-core/`)

1. Warnings are errors. `Nullable` enabled. `dotnet format` clean.
2. `Qaniva.Clinical.Core` = `netstandard2.1`, no Unity, no I/O.
3. Deterministic iteration only (sorted collections / explicit `OrderBy`).
4. Public API gets an XML `<summary>` when its purpose isn't obvious.

### Everything

- Match the surrounding code's naming, comment density, and idiom.
- Reference code as `path:line`.
- `.editorconfig`: LF, final newline, 2-space (4 for `.cs`).

## Workflow

1. Write it in the local style.
2. `pnpm run format` then `pnpm run lint`. C#: `dotnet format`.
3. `pnpm run typecheck` / `dotnet build`.

## Validation

`pnpm run ci` green; `cd clinical-core && dotnet build && dotnet format
--verify-no-changes` clean.

## Done criteria

Format + lint + typecheck/build all green with no rule disabled to get there.

## Common failure modes

- `// eslint-disable-next-line` instead of fixing the issue (unused-disable is
  itself a lint error here).
- `as any` / `as unknown as X` chains where a proper type exists.
- Turning off `TreatWarningsAsErrors` in a `.csproj`.
- Reformatting unrelated lines and burying the real change.
