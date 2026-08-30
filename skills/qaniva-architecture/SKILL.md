# Skill: qaniva-architecture

## Purpose

Keep every change inside Qaniva's architectural boundaries and stop the design from
being re-invented per task.

## When to use

Before any change that touches more than one component, adds a dependency between
components, introduces a new contract, or feels like it needs a new pattern.

## Inputs (read first)

- `AGENTS.md`
- `docs/QANIVA_MVP_BLUEPRINT.md` (§1, §5, §19)
- `docs/architecture/system-overview.md`
- `docs/adr/` (all)

## Non-negotiable rules

1. `Clinical Engine = truth · Unity = presentation · RN = product shell · AI =
   constrained language layer`. Nothing moves truth out of the engine.
2. No shared global state between RN and Unity. Only versioned bridge messages.
3. The engine has no Unity reference and no I/O.
4. Cases are data (`case.json`), not code.
5. A significant, hard-to-reverse decision gets an ADR **before** the code.

## Workflow

1. Locate the component that owns the concern (system-overview table).
2. If the change crosses a boundary, define/extend the contract in the owning
   package (`packages/contracts`, `packages/case-schema`, `packages/analytics-schema`).
3. If you're about to add a pattern/dependency/datastore: write an ADR
   (`docs/adr/ADR-00N-*.md`, Status/Context/Decision/Alternatives/Consequences),
   get it accepted, then build.
4. Implement in the owning component only. Update `docs/` in the same PR.

## Validation

- `pnpm run ci` green.
- Grep check: no `UnityEngine` under `clinical-core/`; no clinical numbers computed
  in `apps/mobile/` or `apps/api/` (search for vitals math, score math).
- New contract has a test in its package.

## Done criteria

Change lives in one component; any cross-boundary data goes through a shared,
tested contract; an ADR exists for any structural decision; docs updated.

## Common failure modes

- "Just this once" putting a rule in the client or the API → drift. Don't.
- Adding a second copy of a contract instead of importing the shared one.
- A new datastore/queue/framework with no ADR.
- Expanding the bridge into a general RPC channel.
