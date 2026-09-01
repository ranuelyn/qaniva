# Qaniva design-language repair — capture pack

Captured 2026-09-01 on the **iPhone 16 Pro simulator** (native `simctl` PNGs)
from the final code of the design-language repair sprint. Every frame is the
real app in a settled state, reached through real routes (production
`qaniva://` deep links; e2e driver pressing the real Unity buttons for in-sim
and Results states). Dev artifacts: Metro banner on cold launch, "E2E run n"
attempt titles in driver-run frames.

The five review screenshots that triggered this sprint map to captures 01–05.

| File | Screen / state | What it evidences |
| --- | --- | --- |
| 01-settings.png | Settings | SettingsRow style-drop bug fixed (informational rows had lost all padding/layout); filled groups without hard outlines, label left / value right, inset dividers, centered version footer |
| 02-error-state.png | Simulation failure | Calm centered error: icon badge, humane title, short body, technical detail behind a "Technical details" disclosure, single CTA. Also evidences the SIMULATION_FAILED `attemptId: null` contract fix — the Unity window no longer covers this screen |
| 03-simulation-screen.png | STEMI in-sim (Treat drawer) | Bedside monitor rotated to face the camera (vitals readable in-scene); segmented category control on one shared surface; calmer action rows with accent edge; ghost Case log/Exit under a hairline |
| 04-ecg-viewer.png | 12-lead ECG viewer | Full-screen diagnostic sheet: action drawer hidden while open, larger title + provenance line, asset centered in a hugging dark surface (no dead white canvas), −/+ left and prominent teal Close right |
| 05-stemi-results-top.png | Results hero | Outcome + score + domain grid + case focus + accented critical decision rows |
| 06-stemi-results-donewell.png | Results, Done well | Former prose wall now one grouped list of compact rows: label / accent status line / faint evidence ids |
| 07-stemi-results-timeline.png | Results, common errors + timeline | Quiet left-rail lists and the time-railed clinical timeline |
| 08-stemi-results-references.png | Results, references | Quiet evidence entries + evidence ledger + replay hash |
| 09-home.png | Home, returning | Continue card dominant; compact indexed case rows; tonal (borderless) secondary button |
| 10-anaphylaxis-results.png | Anaphylaxis results | Same Results system on the second case; causal "What happened to the patient" rail |

All ten captures were viewed individually at review size before this pack was
finalized; transition frames, dev-console frames and stale-build frames were
rejected and recaptured during the sprint (three Unity rebuild iterations for
the viewer centering alone).
