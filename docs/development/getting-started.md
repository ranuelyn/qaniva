# Getting started

## Prerequisites

| Tool | Version | For |
| --- | --- | --- |
| Node | ≥ 20.18 (`.nvmrc` pins 20.18.1) | TS workspace |
| pnpm | ≥ 10 (`packageManager` pins 10.5.0) | workspace install |
| .NET SDK | ≥ 8 (10 works) | clinical engine |
| Unity | `6000.0.x` with URP | simulation runtime (optional for most work) |
| Xcode / Android SDK | current | only for native mobile builds |

## First run

```bash
git clone https://github.com/ranuelyn/qaniva.git
cd qaniva
cp .env.example .env            # no real secrets needed for local dev

pnpm install
pnpm -r --filter "./packages/*" run build   # shared contracts -> dist (needed by apps/api)

pnpm run ci                     # format:check + lint + typecheck + test (all TS)
pnpm run validate:cases         # case JSON schema + cross-reference checks

cd clinical-core && dotnet test # engine unit + golden replay tests
```

Everything above should be green on a clean checkout.

## Run the backend

```bash
pnpm --filter @qaniva/api start:dev      # http://localhost:3000/health
```

## Run the mobile shell

```bash
pnpm --filter @qaniva/mobile start        # Expo dev server (needs a dev build, not Expo Go)
```

The mobile flow (Home → Cases → Briefing → Simulation → Results) runs today against
a deterministic **fake** Unity bridge; the native embed is QAN-004.

## Open the Unity project

`unity/QanivaSimulation` in Unity `6000.0.x`. First open regenerates
`ProjectSettings/*` and `.meta` files — commit those. Then follow
`unity/QanivaSimulation/README.md` for scenes/prefabs and enabling the real engine.

## Common tasks

| I want to… | Do |
| --- | --- |
| add/modify a case | edit JSON under `packages/case-schema/fixtures/…`; `pnpm run validate:cases`; add golden scripts |
| change the engine | edit `clinical-core/`; `dotnet test`; if a golden shifts, review the diff and `UPDATE_GOLDEN=1 dotnet test` |
| change the bridge protocol | edit `packages/contracts/src/protocol.ts` **and** `BridgeProtocol.cs`; bump `PROTOCOL_VERSION`; `pnpm --filter @qaniva/contracts test` |
| add an API endpoint | new/existing Nest module under `apps/api/src/`; add a `*.spec.ts` |
