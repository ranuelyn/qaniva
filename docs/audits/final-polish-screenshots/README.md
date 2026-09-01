# Qaniva final-polish screenshots — visual review pack

Captured 2026-09-01 on the **iPhone 16 Pro simulator** (native `simctl` PNGs,
1206×2622) from the final code of the competitive-visual-audit + final-polish
sprint. Every image is the real app in a settled UI state — no mocks, no debug
menus, no system confirmation sheets, no transition frames. Navigation was
driven through **real routes only**: the production `qaniva://` deep-link
scheme, the real onboarding pager, and (for in-simulation and Results states)
the e2e driver that presses the actual Unity buttons. No internal state was set
directly.

Dev-build artifacts visible in some frames (documented, not product defects):
the tiny Metro banner on cold launch and "E2E run n" attempt titles in
driver-run simulation/Results frames.

| File | Screen / state | Route | Reached by | Data state | Notes |
| --- | --- | --- | --- | --- | --- |
| 00-splash.png | Launch (ink field) | native | warm relaunch | — | Clean capture, no simulator download chrome. The storyboard wordmark (verified compiled: nib references `SplashScreenLogo`) displays sub-400 ms on the dev simulator and is not frame-capturable; release builds show it longer. Known limitation. |
| 01…04-onboarding-*.png | Onboarding pages 1–4 | `Onboarding` | first launch; `qaniva://onboarding?page=N` scrolls the real pager | fresh install | Four genuinely distinct pages: icon + 3-step flow diagram each; dots advance; page 4 CTA is "Get started" |
| 05-home-empty.png | Home, first run | tab Home | onboarding complete | empty store | One explicit "Start first case" action card; compact case previews; View all link |
| 06-cases.png | Case library | tab Cases | `qaniva://cases` | fresh | indexed CaseCards (01/02/03), tightened metadata, "New case" status |
| 07-stemi-briefing.png | STEMI briefing | `CaseDetail` | `qaniva://case/stemi_anterior_001` | fresh | labeled Case information (Role/Setting/Resources/Triage note) + "Your task" teal rail |
| 08-anaphylaxis-briefing.png | Anaphylaxis briefing | `CaseDetail` | `qaniva://case/anaphylaxis_food_001` | fresh | same structured briefing; baseline's left-edge clipping is gone |
| 09-progress-empty.png | Progress, empty | tab Progress | `qaniva://progress` | 0 attempts | honest empty state |
| 10-settings.png | Settings | tab Settings | `qaniva://settings` | — | grouped rows with dividers; destructive Reset distinct; no clipping |
| 11-about.png | About Qaniva | `About` | `qaniva://about` | — | teal "Engine-owned truth" callout; honest validation status |
| 12-medical-disclaimer.png | Educational disclaimer | `Disclaimer` | `qaniva://disclaimer` | — | labeled short sections, no header collision |
| 13-loading-state.png | RN→Unity transition | `Simulation` | deep link into sim | — | The branded RN "Preparing" state lasts <500 ms here; the visible transition is Unity's license splash on ink-matched background. Known limitation. |
| 14-error-state.png | Simulation failure | `Simulation` | deep link to an unknown case (real route) | — | "Simulation unavailable" eyebrow, friendly copy first, technical detail muted, Back to cases CTA; safe area respected |
| 15-stemi-simulation.png | STEMI in-sim | `Simulation` | normal interactive deep-link launch | live engine | teal-selected Examine tab, ink surfaces, tighter patient camera; vitals strip + monitor |
| 16-ecg-viewer.png | 12-lead ECG viewer | in-sim overlay | e2e_ui (real button press) | placeholder asset | dark contained viewer; title clear of the status area; ECG fit to viewport; watermark/provenance retained |
| 17-stemi-results-top.png | Results hero | `Results` | e2e run, settled screen | 88-pt ideal run | Outcome hero + score-domain grid + Case focus + accented critical decisions |
| 18-stemi-results-mid.png | Results, done well | `Results` | e2e auto-scroll of the real ScrollView | 88-pt run | grouped non-critical successes with evidence ids |
| 19-stemi-results-deep.png | Results, clinical timeline | `Results` | e2e auto-scroll | 88-pt run | time-railed connected timeline rows |
| 20-stemi-results-references.png | Results, references | `Results` | e2e auto-scroll | 88-pt run | quiet evidence entries + evidence ledger/provenance + replay hash |
| 21-home-completed.png | Home after completion | tab Home | end of e2e loop | completed attempts | dominant Continue/Replay card; compact library rows subordinate |
| 22-anaphylaxis-simulation.png | Anaphylaxis in-sim | `Simulation` | e2e_ui run | live engine | its own vitals (HR 118); same coherent sim UI |
| 23-anaphylaxis-treatment.png | Treat drawer, epinephrine choice | `Simulation` | e2e_ui run | live engine | IM 0.5 mg vs IV 1 mg push visible — the route-safety decision surface |
| 24-anaphylaxis-results.png | Anaphylaxis results + causality | `Results` | e2e run, settled | 80-pt run | hero + "What happened to the patient" causal rail (epi response) |
| 25-progress-with-attempts.png | Progress, populated | tab Progress | `qaniva://progress` after real runs | 2 cases, 5 attempts | borderless metric panel, grouped case rows, score-column recent list |
| 26-replay-state.png | Briefing, replay | `CaseDetail` | `qaniva://case/stemi…` after attempts | history present | recent attempts + sticky "Play again"; deep-link title fallback fixed this sprint |

## Completeness vs MVP_SCREEN_INVENTORY.md

| Screen | Required | Screenshot(s) | Status |
| --- | --- | --- | --- |
| Splash | yes | 00 | ✅ (ink field; wordmark sub-frame, see note) |
| Onboarding | yes | 01–04 | ✅ all 4 distinct pages |
| Home | yes | 05, 21 | ✅ empty + returning/completed |
| Cases | yes | 06 | ✅ |
| Briefing | yes | 07, 08, 26 | ✅ both cases + replay state |
| Simulation | yes | 15, 22, 23, 13 | ✅ both cases + treatment drawer + transition |
| Result viewer (ECG) | yes | 16 | ✅ |
| Results/Debrief | yes | 17–20, 24 | ✅ hero/mid/timeline/references + 2nd-case causality |
| Progress | yes | 09, 25 | ✅ empty + populated |
| Settings | yes | 10 | ✅ |
| About | yes | 11 | ✅ |
| Disclaimer | yes | 12 | ✅ |
| Error state | yes | 14 | ✅ |

Invalid captures excluded during the final audit: a mid-transition
"leaving the simulation" frame, a results frame with a push-transition ghost
sliver, a mislabeled ECG capture showing the monitor result banner, and the
baseline-style splash frame containing simulator download chrome. All were
recaptured or removed; none are in this pack.
