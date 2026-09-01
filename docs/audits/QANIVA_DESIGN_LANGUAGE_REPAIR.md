# Qaniva design-language repair (2026-09-01)

A focused redesign sprint triggered by owner design review of five screens
(Settings, error state, simulation, ECG viewer, STEMI Results). Evidence:
[`design-repair-screenshots/`](design-repair-screenshots/).

## Diagnosis

The previous polish pass improved hierarchy but left systemic problems:

1. **A real component bug shaped the Settings review.** `SettingsRow` passed a
   Pressable-style function to a plain `View` for rows without `onPress`; React
   Native silently drops such styles, so informational rows rendered as
   cramped, unpadded stacked text touching the group border.
2. **A real bridge bug shaped the error-state review.** Unity emits
   `SIMULATION_FAILED` with `attemptId: null` when a case fails before an
   attempt exists; the RN contract (`z.string().uuid().optional()`) rejected
   null, RN never entered the failed phase, and the Unity window stayed up
   over the error screen.
3. **Structural, not cosmetic, UI debt**: heavy outlines and same-weight boxes
   (Settings groups, Results cards), raw technical copy at full prominence
   (error details), an in-scene monitor angled away from the camera, ad-hoc
   blue-block tab/action styling in Unity, and an ECG viewer where the asset
   sat on a dead white canvas beneath competing action controls.

## What changed

### Design system (RN)

- `SettingsRow` rebuilt (bug fix + premium grouped-row layout: 56pt min
  height, proper insets, value right-aligned muted, subtle chevron).
- New `Group` (filled surface, no hard outline) and `Divider inset` for
  grouped lists; new `TextButton` for tertiary CTAs; `SecondaryButton` made
  tonal (border removed).
- Grouped surfaces replace bordered boxes across Settings and Results; the
  Results hero dropped its outline.

### Screens (RN)

- **Settings**: filled groups + inset dividers; version becomes a centered
  footer caption; destructive action stays distinct.
- **Error state**: icon badge, humane title per failure code, short body,
  technical detail collapsed behind a "Technical details" disclosure, single
  primary CTA. Loading state centered with the case title as quiet metadata.
- **Results**: every criterion section (harmful, unnecessary, delayed,
  missed, done well) now renders through the single compact `CriterionRow`
  shape inside grouped surfaces — label, accent status line, faint
  context + evidence meta. Teaching points/common errors/alternatives use
  quiet left-rail lists. Footer is primary Replay + quiet text "Back to home".
- **Contracts**: `SIMULATION_FAILED.attemptId` is now `nullish` (a failure
  before an attempt legitimately has no id).

### Unity

- **Monitor faces the user**: rotated from yaw −35° to (8°, 14°, 0°) toward
  the presentation camera; vitals are readable in-scene.
- **Category tabs** are a segmented control on one shared surface (teal filled
  active segment); **action rows** are calmer flat rows with an accent edge
  and clearer disabled treatment; **Case log/Exit** are ghost utilities under
  a hairline.
- **ECG viewer** redesigned as a full-screen diagnostic sheet: the presenter
  hides the action drawer/result banner while open (they previously drew on
  top of the viewer), the title/provenance sit in a slim header, and the
  asset floats centered in a dark surface that hugs it (a stage wrapper +
  non-growing ScrollView — margin-based centering caused scrollbar-feedback
  layout loops and was rejected).

## Validation

`pnpm run ci` green (typecheck/lint/format/tests incl. contracts 20) · cases
3/3 · clinical-core 84/84 · Unity EditMode 18/18 · PlayMode 13/13 (run on the
final state) · SIM=1 Unity export + pod install + native simulator build +
install, three iterations, final captures from the final build. All ten final
captures visually reviewed.

## Remaining known limitations

- Patient/environment remain improved-EARLY-MVP art (unchanged by design).
- Unity vitals/status typography could take one more refinement pass (P2).
- Dev-build artifacts (Metro banner, "E2E run n" titles) appear only in
  capture conditions.
