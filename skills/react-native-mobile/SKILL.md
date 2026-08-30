# Skill: react-native-mobile

## Purpose

Build the RN product shell so it stays a shell — product UI, navigation, and the
Unity host — with no clinical logic and no Unity scene logic.

## When to use

Any change under `apps/mobile/`.

## Inputs (read first)

- `docs/adr/ADR-001-react-native-product-shell.md`
- `docs/architecture/rn-unity-boundary.md`
- `apps/mobile/src/unity/` (bridge client + `useUnitySimulation`)
- `apps/mobile/src/navigation/types.ts` (the vertical slice)

## Non-negotiable rules

1. Expo **development build** target, not Expo Go (the Unity embed needs native
   code). `newArchEnabled` stays true.
2. The shell computes no clinical values. Vitals/score/timeline come from
   `AttemptSummary` over the bridge or from the API.
3. Talk to Unity only through `useUnitySimulation` / the `UnityBridgeTransport`
   contract. Swapping the fake for the native transport must not change screen code.
4. All backend calls go through `src/api/client.ts`. No `fetch` scattered in screens.
5. Analytics via `src/analytics/` using `@qaniva/analytics-schema` event types.
6. Keep design minimal — `src/theme/tokens.ts` + `src/components/ui.tsx`. Don't
   grow a design system opportunistically.
7. RN-free modules (bridge handling, analytics, pure helpers) stay importable
   without `react-native` so `vitest` can test them.

## Workflow

1. Add a screen under `src/screens/`, register it in `RootNavigator` + `types.ts`.
2. Data: `apiClient` for backend, `useUnitySimulation` for the sim.
3. Wrap risky trees in the `ErrorBoundary` (already at the root).
4. `pnpm --filter @qaniva/mobile typecheck` and `test`.

## Validation

- `pnpm --filter @qaniva/mobile run typecheck` clean.
- `pnpm --filter @qaniva/mobile run test` green (RN-free units).
- `pnpm --filter @qaniva/mobile run lint` clean.

## Done criteria

Typecheck + lint + RN-free tests green; new screens in the navigator + param list;
no clinical math; no direct `fetch`; bridge access only via the hook.

## Common failure modes

- Importing `react-native` into a module that a `vitest` test needs.
- Duplicating `AttemptSummary` fields into local types instead of importing from
  `@qaniva/contracts`.
- Putting business rules in a screen instead of the API or the engine.
- Assuming Metro resolves workspace packages without `metro.config.js` watchFolders
  (it's configured — don't remove it).
