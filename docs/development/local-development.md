# Local development notes

## Monorepo shape

- **pnpm workspaces**: `apps/*`, `packages/*`. The C# solution
  (`clinical-core/`) and the Unity project (`unity/`) are outside the pnpm
  workspace and built with their own toolchains.
- Shared TS packages (`@qaniva/contracts`, `@qaniva/case-schema`,
  `@qaniva/analytics-schema`) compile to **CommonJS** `dist/`. `apps/api` (Nest,
  CJS) and `apps/mobile` (Metro) both consume the built `dist`. **Run
  `pnpm -r --filter "./packages/*" run build` after changing a package** before
  building/testing `apps/api`.
- Package `test`/`typecheck` run against `src` (via vitest/tsc), so day-to-day
  edits don't need a rebuild — only `apps/api` does.

## TypeScript

- `tsconfig.base.json` is strict (`noUncheckedIndexedAccess`, `noImplicitOverride`,
  `exactOptionalPropertyTypes`, …). Packages narrow `module`/`moduleResolution` to
  `CommonJS`/`Node`; `apps/api` adds decorators; `apps/mobile` uses
  `Bundler` resolution + `react-jsx` and relaxes `exactOptionalPropertyTypes`.
- ESLint flat config at the root (`eslint.config.mjs`). `apps/api/**` disables
  `consistent-type-imports` because NestJS DI needs runtime type refs.
- Prettier; `.prettierignore` excludes `unity/`, `clinical-core/`, lockfiles.

## .NET

```bash
cd clinical-core
dotnet build          # warnings are errors here
dotnet test
dotnet format --verify-no-changes
```

`Qaniva.Clinical.Core` targets `netstandard2.1` on purpose (Unity). If you need a
modern C# feature that the polyfills in `Compat/Polyfills.cs` don't cover, add a
guarded polyfill rather than changing the target.

## Unity

`.gitignore` covers `Library/`, `Temp/`, `Logs/`, `Builds/`, `UserSettings/`,
generated `*.csproj`/`*.sln`, and the synced `Plugins/ClinicalCore/` +
`Resources/Qaniva/Cases/`. It does **not** ignore `Assets/**`, `Packages/`,
`ProjectSettings/`, or `.meta` files — commit those.

Sync the engine DLL into Unity:

```bash
scripts/sync-clinical-core-to-unity.sh
```

## iOS native project (bare workflow, ADR-008)

`apps/mobile/ios/` is committed. Do **not** run `expo prebuild` again — native
changes (the Unity host pod, Podfile Unity logic) live in the tree. Workflow:

```bash
cd apps/mobile/ios
LANG=en_US.UTF-8 pod install          # after any pod/podspec/Podfile change
xcodebuild -workspace Qaniva.xcworkspace -scheme Qaniva -configuration Debug \
  -destination 'generic/platform=iOS Simulator' CODE_SIGNING_ALLOWED=NO build
```

To embed the real Unity runtime: `scripts/export-unity-ios.sh` (add `SIM=1` for a
simulator-arch framework), then `pod install` again. The framework lands in
`apps/mobile/unity-frameworks/ios/` (git-ignored). **Stale-framework rule:** after
any change under `unity/QanivaSimulation/Assets/`, re-run the export before
trusting a device/simulator run.

## Env vars

Copy `.env.example` → `.env`. Public mobile vars are `EXPO_PUBLIC_*`. Nothing
secret is required for local dev; the AI provider defaults to the deterministic
stub.

## Git LFS

Rules are declared in `.gitattributes` for `*.fbx/*.blend/*.png/*.wav/…` but LFS is
**not** initialised in the foundation (no binary assets yet). Before adding the
first 3D/media asset: `git lfs install` in the repo.
