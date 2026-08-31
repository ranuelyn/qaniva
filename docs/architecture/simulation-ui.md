# Simulation UI (interactive loop)

How the in-simulation interaction layer works (QAN-006). The invariants of
[ADR-003](../adr/ADR-003-deterministic-engine-owns-clinical-truth.md) hold
everywhere here: the UI renders engine output and forwards user intent; it never
computes availability, state, vitals, scores, or completion.

## Technology

**UI Toolkit** (runtime). Chosen because every artifact is text-authorable —
`SimulationScreen.uxml` (layout), `SimulationScreen.uss` (styles),
`QanivaTheme.tss` (theme import) live in
`unity/QanivaSimulation/Assets/Qaniva/Resources/Qaniva/UI/` and are committed; the
one binary asset (`QanivaPanelSettings.asset`) is generated idempotently by
`QanivaBuild.CreateUiAssets` (batchmode) and committed. uGUI would have required
Editor-authored prefabs for every list/panel. This is the MVP interaction
foundation, not the final UX.

## Runtime modes

`START_SIMULATION.payload.mode` (typed in `packages/contracts`, mirrored in
`BridgeProtocol.Modes`, parity-tested):

| Mode | Who sends it | Behaviour |
| --- | --- | --- |
| `interactive` | every user launch (contract default) | No automation. Every action is a user tap. |
| `e2e_autoplay` | test harness only | `IntegrationAutoPlayer` applies the golden script **directly to the runtime** (bridge/lifecycle regression; bypasses the UI by design). |
| `e2e_ui` | test harness only | `InteractiveE2eDriver` presses the **real UI controls** through the UI Toolkit event system (interactive-path regression; run 1 aborts via the real Exit button, run 2 completes). |

Two gates make accidental automation in production difficult: both drivers are
compiled only under the `QANIVA_INTEGRATION_AUTOPLAY` scripting define, **and**
each is inert unless `SimulationBridgeController.CurrentMode` equals its own mode
(`ShouldRunFor`, unit-tested; PlayMode-tested that `interactive` never auto-plays).

## Data flow

```
                    engine (clinical-core, via ClinicalRuntime)
                      │ GetActionAvailability()   │ GetTimeline()   │ Snapshot
                      ▼                           ▼                 ▼
SimulationBridgeController ──SimulationStarted/SnapshotUpdated──► SimulationUiController
                                                                    ├─ VitalsPresenter    (vitals bar + status)
                                                                    ├─ ActionListPresenter (tabs + buttons)
                                                                    └─ TimelinePresenter  (case log)
user tap ─► Button ─► ActionListPresenter handler ─► SimulationUiController.Submit(actionId)
        ─► SimulationBridgeController.SubmitPlayerAction ─► ClinicalRuntime ─► clinical-core
        ─► ActionOutcomeView ─► result banner + refreshed availability/vitals/timeline
```

## Canonical action availability

`Simulation.GetActionAvailability()` (clinical-core) computes, per action:

- **HIDDEN** — `Visible == false` (`visibleWhen` unmet): not rendered at all.
- **VISIBLE + DISABLED** — unmet precondition or non-repeatable already performed:
  rendered greyed with the engine-worded `DisabledReason`.
- **VISIBLE + ENABLED** — tappable.

`IsActionOfferable` (what `ApplyAction` accepts) is computed from the **same
projection** (`offerable == Visible && Enabled`), so UI and engine can never
disagree. Unity carries the projection through
`IClinicalRuntime.GetActionAvailability()` untouched.

## Categories

`ActionListPresenter` groups engine action **types** into the product taxonomy
(presentation-only mapping): `communication→Patient`, `examine→Examine`,
`order→Orders`, `medication|procedure→Treat`, `consult|disposition→More`. Tabs
render only for categories that currently have visible actions. Adding a new
category = one entry in `CategoryByType` + `CategoryOrder`; a different case
renders without any scene/UI change because everything is data-driven.

## Submission rules

`SimulationUiController.Submit` is the single user-intent entry point:
a re-entrancy lock plus a 0.3s debounce (`SubmitDebounceSeconds`) absorb
double-taps/duplicated UI events (PlayMode-tested); the engine's rejection of
non-repeatable/unavailable actions remains the canonical backstop. Only the
canonical action id (+ params) crosses the boundary.

## Timeline / case log

The "Case log" panel renders `IClinicalRuntime.GetTimeline()` — the engine's own
attempt timeline. There is no Unity-side action log that could drift.

## Completion & exit

- Terminal state is decided **only** by the engine. When a submit returns
  `Terminated`, the UI shows the completion panel and
  `SimulationBridgeController` emits `SIMULATION_COMPLETED` (guarded
  exactly-once; EditMode + PlayMode tested) with the canonical `AttemptSummary`.
  RN navigates to Results.
- The Exit button calls `SimulationBridgeController.RequestExit()` →
  `EXIT_REQUESTED` (never `SIMULATION_COMPLETED`); RN receives phase `exited`
  and navigates back. The warm Unity runtime stays intact for the next launch.

## Element naming contract (E2E + tests)

Tabs: `tab-<Category>` (e.g. `tab-Treat`); actions: `action-<actionId>`;
`exit-button`, `toggle-timeline`, `timeline-<seq>`. `InteractiveE2eDriver` and
the PlayMode tests locate controls by these names and press them via
`NavigationSubmitEvent` — real event dispatch, same handlers as a tap.

## Reproducing the interactive demo

```bash
# assets + framework (after any Unity change)
SIM=1 scripts/export-unity-ios.sh
cd apps/mobile/ios && LANG=en_US.UTF-8 pod install && xcodebuild -workspace Qaniva.xcworkspace \
  -scheme Qaniva -configuration Debug -destination 'generic/platform=iOS Simulator' \
  ARCHS=arm64 ONLY_ACTIVE_ARCH=YES CODE_SIGNING_ALLOWED=NO build

# interactive play (human): launch the app, Start a case -> Enter simulation
# scripted interactive-path proof:
EXPO_PUBLIC_E2E_AUTOSTART=demo_sync_bradycardia_001 npx expo start --dev-client
# (EXPO_PUBLIC_E2E_MODE=autoplay for the runtime-direct regression variant)
```

The golden parity criterion: the interactive path (manual or `e2e_ui`) with the
ideal action sequence must reproduce the committed golden replay —
outcome `complete`, score 80, replayHash `fe2191ff…dfc5` — verified in the
`ManualUiPlayReproducesTheGoldenReplay` PlayMode test and in the on-simulator run.
