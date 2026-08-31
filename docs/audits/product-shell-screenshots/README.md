# Qaniva product-shell screenshots — visual review pack

Captured 2026-09-01 on the **iPhone 16 Pro simulator** from the final code of
the product-shell sprint (post-fix recaptures; nothing here predates the last
code change it shows). All are simulator-native PNGs of the real app — no
mocks, no debug overlays, no developer menus. Navigation for the captures was
driven through **real routes** (deep links via the app's production `qaniva://`
scheme and, for in-simulation states, the e2e driver that presses the real UI
controls); no internal state was set directly. Dev-only artifacts visible in a
few frames: the tiny Metro banner during launch (debug builds only) and "E2E
run n" attempt titles inside sim/results frames from driver-run sessions.

| File | Screen / state | Route | Reached by | Data state | Notes |
| --- | --- | --- | --- | --- | --- |
| 00-splash.png | Launch (ink field) | native | cold launch | — | Storyboard = ink + wordmark (verified compiled: nib references `SplashScreenLogo`); it displays sub-400 ms on this simulator so the frame shows the ink launch field. Known limitation. |
| 01…03b-onboarding-0*.png | Onboarding pages 1–4 | `Onboarding` | first launch; pages via `qaniva://onboarding?page=N` (real pager scroll) | fresh install | Skip + Next + dots; completion persisted |
| 04-home-empty.png | Home, no attempts | tab Home | onboarding `complete=1` (invokes the real Get-started handler) | empty store | Continue empty-state + case cards + tabs |
| 05-home-with-progress.png | Home, returning learner | tab Home | `qaniva://home` after real runs | 2 completed cases | Continue card w/ last score; Completed badges |
| 06-cases.png | Case library | tab Cases | `qaniva://cases` | fresh | catalog-driven CaseCards |
| 07/08-*-briefing.png | Briefings | `CaseDetail` | `qaniva://case/<id>` | fresh | authored `metadata.briefing` from case.json |
| 09-stemi-simulation.png | STEMI in-sim | `Simulation` | **normal interactive** deep-link launch | live engine | no e2e driver in this frame |
| 10-anaphylaxis-simulation.png | Anaphylaxis in-sim | `Simulation` | e2e_ui run | live engine | its own vitals (118 / 88-54 / 92% / 26) |
| 11-ecg-viewer.png | ECG result viewer | in-sim overlay | e2e_ui (real button) | placeholder asset | provenance note active |
| 12/13/14-stemi-results-*.png | Results top / mid / references | `Results` | e2e run + capture auto-scroll of the REAL ScrollView | 88-pt ideal run | taxonomy, teaching points, citations |
| 15-anaphylaxis-results.png | Anaphylaxis results | `Results` | e2e run | 80-pt run | causality section visible |
| 16/17-progress-*.png | Progress empty / with attempts | tab Progress | `qaniva://progress` | 0 vs 2 attempts | metrics + per-case + recent |
| 18-settings.png | Settings | tab Settings | `qaniva://settings` | — | informational difficulty/language; destructive reset |
| 19-about.png | About Qaniva | `About` | `qaniva://about` | — | product copy, status honesty, version |
| 20-medical-disclaimer.png | Educational disclaimer | `Disclaimer` | `qaniva://disclaimer` | — | validation-pending stated |
| 21-loading-state.png | RN→Unity transition | `Simulation` | cold deep-link into sim | — | The branded RN "Preparing the simulation" screen lasts <500 ms here; the captured visible transition is the dark Unity splash. Known (good) limitation. |
| 22-error-state.png | Simulation failure | `Simulation` | deep link to an unknown case (real route) | — | friendly copy + muted detail + Back; Unity window correctly dismissed (bug found & fixed this sprint) |
| 23-replay-state.png | Replay affordance | `CaseDetail` | `qaniva://case/stemi…` after attempts | history present | recent attempts + "Play again" |
| 24-case-completed-home.png | Home after completion | tab Home | end of e2e loop | 1 completed | Continue card + Completed badge |

## Completeness vs MVP_SCREEN_INVENTORY.md

| Screen | Required | Screenshot(s) | Status |
| --- | --- | --- | --- |
| Splash | yes | 00 | ✅ (ink field; wordmark sub-frame, see note) |
| Onboarding | yes | 01–03b | ✅ all 4 pages |
| Home | yes | 04, 05, 24 | ✅ empty + progress + completed |
| Cases | yes | 06 | ✅ |
| Briefing | yes | 07, 08, 23 | ✅ both cases + replay state |
| Simulation | yes | 09, 10, 21 | ✅ both cases + transition |
| Result viewer | yes | 11 | ✅ |
| Results/Debrief | yes | 12–15 | ✅ top/mid/references + 2nd case |
| Progress | yes | 16, 17 | ✅ empty + populated |
| Settings | yes | 18 | ✅ |
| About | yes | 19 | ✅ |
| Disclaimer | yes | 20 | ✅ |
| Error state | yes | 22 | ✅ |

Known screens NOT capturable headlessly: none remaining (Cases-list tap gap
from previous sprints is closed by deep links).
