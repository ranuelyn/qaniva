# Qaniva simulation interaction redesign — demo captures

Captured 2026-09-02 on the **iPhone 16 Pro simulator** from the final code of
the simulation-interaction redesign sprint. Everything is the real app: the
product shell is driven through production `qaniva://` deep links, the
in-simulation footage comes from the e2e driver pressing the **real** Unity
buttons (category dock, decision rows, ECG viewer Close), and the interaction
stills (04–07) were produced by real taps on the running app. No mockups, no
dev menus, no system sheets. Dev-build artifacts: a brief Metro banner on cold
launch and "E2E run n" attempt titles in driver-run footage.

## Videos (`video/`, H.264, 604×1312, silent)

| File | Length | Covers |
| --- | --- | --- |
| 01-shell-flow.mp4 | ~46 s | launch → Home → onboarding page → Home → Cases → STEMI briefing → Progress → Settings → About → Medical disclaimer → Home |
| 02-stemi-simulation.mp4 | ~35 s | briefing → Unity hand-off → simulation: Examine/Patient/Orders/Treat categories, decision rows becoming **Done**, result cards, **12-lead ECG viewer open and closed**, cath-lab hand-off → "Case complete" → Results (hero, criterion groups, timeline, references) → Home |
| 03-anaphylaxis-simulation.mp4 | ~33 s | same loop on the second case: IM epinephrine route decision in the Treat sheet, patient response, Results with the causal "What happened to the patient" rail |

Note: the shell video shows onboarding page 1 only — the `?page=N` deep link
did not advance the pager during this recording; the four distinct pages are
evidenced in `docs/audits/final-polish-screenshots/01–04`.

## Stills

| File | Screen / state | Reached by | Why it matters |
| --- | --- | --- | --- |
| 01-home.png | Home, returning learner | `qaniva://home` | shell baseline the simulation must feel continuous with |
| 02-cases.png | Case library | `qaniva://cases` | — |
| 03-stemi-briefing.png | STEMI briefing | `qaniva://case/stemi_anterior_001` | entry point of the simulation flow |
| 04-simulation-default.png | Simulation, default (Examine) | real launch, no interaction | vitals tiles + humanized status strip, camera-facing monitor, patient dominant, **action sheet** with grabber, quiet Case log/Exit, underline dock, decision rows with chevrons |
| 05-simulation-treat-category.png | Treat category | real tap on **Treat** | title + secondary-line rows (Aspirin 300 mg / chewed), inline **Not yet available** statuses stay readable; result card above the sheet |
| 06-simulation-inline-status.png | Examine after an action | real tap on a decision row | performed row becomes muted with inline green **Done**; engine result card above the sheet |
| 07-simulation-sheet-collapsed.png | Sheet collapsed to its dock | real tap on the grabber | patient takes the whole scene; dock + last result remain |
| 08-ecg-viewer.png | 12-lead ECG viewer | e2e run frame | full-screen diagnostic sheet, action UI hidden, provenance warning, teal Close |
| 09-stemi-results-top.png | Results hero | e2e run | outcome + score grid + critical decisions |
| 10-stemi-results-donewell.png | Results, Done well | e2e auto-scroll | compact grouped rows — same status language as the sheet |
| 11-stemi-results-references.png | Results, references | e2e auto-scroll | evidence ledger + replay hash |
| 12-anaphylaxis-simulation.png | Anaphylaxis, Treat | e2e run frame | epinephrine IM given → Done; IV push row visible as the route-safety decision |
| 13-anaphylaxis-results.png | Anaphylaxis results | e2e run | causal patient-response rail |
| 14-progress.png … 17-disclaimer.png | Progress / Settings / About / Disclaimer | deep links | unchanged shell for coherence review |
| 18-error-state.png | Simulation failure | `qaniva://simulate/unknown_case` | calm error surface (unchanged) |

All 18 stills and all three videos were reviewed frame-by-frame (contact
sheets) before packaging; transition frames and mis-tapped states were
replaced.
