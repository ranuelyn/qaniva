# Qaniva MVP screen inventory (frozen 2026-09-01)

The canonical product shell. 12 surfaces; navigation = bottom tabs
(Home / Cases / Progress / Settings) + a root stack for task flows
(Onboarding, Briefing, Simulation, Results, About, Disclaimer). Simulation is
a task flow launched from a case — never a tab.

| # | Surface | Route | Purpose | Entry points | Primary CTA | Secondary | Data | Empty state | Loading | Error | Future |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Splash | native | brand identity during native startup | app launch | — (auto) | — | none | n/a | native | n/a | animated mark |
| 2 | Onboarding | `Onboarding` (first launch only) | explain the product concept in 4 pages | first launch; never again after completion | Get started (last page) | Skip | none | n/a | n/a | n/a | localized copy |
| 3 | Home | tab `Home` | main return surface: continue, cases, progress summary | tabs, launch (returning user) | Continue/Replay latest case | open a case; view progress | catalog + attempt store | "Choose your first case" (no attempts) | brief while store reads | store read fails silently → empty variant | daily suggestions |
| 4 | Cases | tab `Cases` | case library | tabs, Home "All cases" | open a case | — | catalog + per-case progress | n/a (catalog is bundled) | progress read | offline keeps bundled catalog | search/filters at >6 cases |
| 5 | Case Briefing | `CaseDetail` | INACSL prebrief + attempt history + start | Cases, Home, Progress | Enter simulation / Play again | back | case data (briefing now authored IN case.json) + history | "no attempts yet" implicit | history read | — | difficulty selector when real |
| 6 | Simulation transition + host | `Simulation` | branded hand-off into the full-screen Unity sim | Briefing, Replay | — (auto) | exit (in-sim) | bridge | n/a | branded "Preparing the simulation" | friendly per-code message + back | — |
| 7 | Result viewer (ECG etc.) | in-sim overlay | inspect investigation assets | in-sim result | close | zoom | case result assets + provenance | missing-asset message | — | loud missing-asset text | more modalities |
| 8 | Results / Debrief | `Results` | deterministic timing/causality/evidence debrief | sim completion | Replay this case | back to home | AttemptSummary | n/a | — | render-safe (summary always present) | share/export |
| 9 | Progress | tab `Progress` | attempts + per-case mastery | tabs | replay a case | — | attempt store + catalog | "You haven't completed a case yet." | store read | — | trends |
| 10 | Settings | tab `Settings` | preferences shell + governance links | tabs | — | rows | app prefs | n/a | — | reset confirm | language, difficulty when real |
| 11 | About | `About` | what Qaniva is; version; provisional-brand + legal status | Settings | — | back | static + version | n/a | n/a | n/a | team/links |
| 12 | Educational disclaimer | `Disclaimer` | educational-use boundary + validation-pending honesty | Settings, About | — | back | static | n/a | n/a | n/a | localized |

Deliberately NOT built (see gap analysis): search/filters, login, profile
page, subscription, daily case, challenges, difficulty toggles, educator
surfaces, notifications.
