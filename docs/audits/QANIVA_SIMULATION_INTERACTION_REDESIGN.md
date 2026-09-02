# Qaniva simulation interaction redesign (2026-09-02)

Evidence: [`simulation-redesign-captures/`](simulation-redesign-captures/)
(3 videos + 18 stills, README inside).

## Diagnosis

The prior simulation layer was styled, not designed. Structurally:

- **Wrong scale.** The UI Toolkit panel is 1:1 with device pixels (1206×2622),
  so its 26–28 px type rendered at ~9–11 pt against the shell's 15–16 pt —
  the whole layer read at two-thirds scale, which is most of the "dev tool"
  feel.
- **Permanent control slab.** The bottom third was always occupied: tabs +
  giant button rows + footer blocks, all equal weight, no way to give the
  patient the screen.
- **Machine copy on screen.** `Rhythm: sinus_rhythm Circulation:
  poor_perfusion`, "HR 96", "already performed" as a detached amber line.
- **Buttons as rows.** Each action was a repeated slab with no
  title/secondary hierarchy and no inline state.
- **Monitor turned away**, partially cropped.

## Redesign

- **Action sheet model.** The bottom area is a rounded sheet: grabber + quiet
  utilities (Case log / Exit as text), an underline-indicator category dock,
  and a fixed-height decision list. The grabber collapses the sheet to its
  dock (patient dominant); choosing any category — or re-tapping the active
  one — expands it. A fresh simulation always starts expanded.
- **Category dock.** Five quiet labels, active = teal text + 4 px underline; no
  filled blocks, no segmented-control chrome.
- **Decision rows.** Each action is a row (still a `Button` for the
  driver/tests contract): title, optional secondary line split from the
  authored label ("Aspirin 300 mg" / "chewed"), right-side chevron when
  available, inline status when not — **Done** (green) for performed,
  **Not yet available** for unmet preconditions, engine text passthrough
  otherwise. Disabled rows stay readable.
- **Top strip.** Vitals as caption + value tiles (HR / BP / SpO2 / RR /
  Elapsed) at shell scale; status line humanized (`Sinus rhythm · Poor
  perfusion · Alert`). Identifiers are only displayed differently, never
  altered.
- **Monitor.** Moved further into frame and turned toward the camera
  ((0.56, 0, 1.16), yaw 18°, tilt 8°) — readable as a secondary immersive
  object while the strip stays primary.
- **Viewer / case log.** Viewer unchanged structurally, re-scaled; hidden
  scrollers; case log entries humanized.
- **Type scale.** Body 42–45 px, secondary 33–36 px, captions 30–33 px,
  values 50 px — the shell's scale, so RN→Unity reads as one product.

## Supporting RN change

The production `qaniva://simulate/:caseId` route could start a simulation
without an attempt id; the bridge rejected the resulting `attemptId: null`
messages. `SimulationScreen` now mints attempt id / seed and derives version
and title from the catalog when the route omits them.

## Validation

`pnpm run ci` green · cases 3/3 · Unity EditMode 18/18 · PlayMode 13/13 (final
code) · SIM=1 export + pod install + native build + install (final build) ·
all sheet interactions verified by real taps on the running app · every
still and video frame-reviewed. Clinical-core untouched this sprint.

## Known limitations

- Dev-build artifacts in footage (Metro banner, "E2E run n").
- Shell video shows one onboarding page (deep-link pager advance did not fire
  in that recording).
- Patient/environment remain EARLY-MVP art by design.
