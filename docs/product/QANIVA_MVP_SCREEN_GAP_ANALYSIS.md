# Full Code → Qaniva MVP screen gap analysis

Date: 2026-09-01. **Inputs:** the competitor teardown digested in
`docs/QANIVA_MVP_BLUEPRINT.md` §2 (105 documented screens/states; densest
areas Results 10, Educator Dashboard 10, Investigate 8, Simulation 8, Case
Library 7, Profile 7; source: `Qaniva_Full_Code_Detayli_Rapor.pdf`, 21.08.2026
— the raw PDF/CSV is **not committed to this repository**, so this analysis
works from the blueprint's digest plus the sprint brief's area list), the
official-docs competitor research in `docs/clinical/cases/stemi/research.md`
§13, and an independent audit of every current RN screen. Principles only —
no layouts, wording, assets or screen structures are copied.

## Matrix

| Full Code surface | Qaniva equivalent (before sprint) | Status before | MVP necessity | Decision | Notes |
| --- | --- | --- | --- | --- | --- |
| Launch/Splash | expo default splash (blank) | missing brand | REQUIRED | build (storyboard + wordmark) | native splash, no fake delay |
| Onboarding | none | missing | REQUIRED | build (4 concise pages, skip, persisted) | concept-level, no feature tour |
| Home | minimal (title + 1 button) | weak | REQUIRED | rebuild (continue + cases + progress, data-driven) | main return surface |
| Continue Learning | none | missing | REQUIRED | fold into Home "Continue" card | from persisted attempts |
| Daily Patient | — | — | NOT APPLICABLE | exclude | engagement mechanics post-MVP at best |
| Challenges | — | — | NOT APPLICABLE | exclude | |
| EMS mode | — | — | NOT APPLICABLE | exclude | different product line |
| Case Library | Cases list (dev-styled) | partial | REQUIRED | polish (CaseCard, progress, teaser) | 3 cases; no search |
| Search / Filters | none | — | POST-MVP | exclude | enterprise search for 2 real cases is waste |
| Case Detail/Briefing | CaseDetail (briefing text in RN code) | partial | REQUIRED | polish + move briefing into case data | INACSL prebrief stays |
| Difficulty controls | none functional | — | POST-MVP | show fixed MVP mode informationally only | no fake toggles |
| Login/Account | none | — | POST-MVP | exclude | no auth at MVP (deliberate) |
| Subscription/Paywall | none | — | NOT APPLICABLE (MVP) | exclude | |
| Profile | none | missing | NICE TO HAVE | fold into Settings + Progress | no identity → no profile page |
| Simulation (8 states) | Unity interactive sim (5 areas) | strong | REQUIRED | keep; brand the RN→Unity transition | 5 areas (Patient/Examine/Orders/Treat/More) vs their 8 verticals — deliberate |
| Investigate / result viewers | generic result viewer (ECG) | strong | REQUIRED | keep (provenance note stays) | |
| Differential/Consult/Hand-off | inside More tab | present | REQUIRED | keep | |
| Score screen | Results header | strong | REQUIRED | restyle to tokens | |
| Debrief (10 states) | Results taxonomy + causality + evidence | strong | REQUIRED | restyle only — semantics frozen | Qaniva goes further: timing decay, causality, evidence ids |
| Replay | Replay button (Results/Briefing) | present | REQUIRED | surface also on Home/Progress | |
| Progress/Stats | per-case rows on Cases only | partial | REQUIRED | build a Progress tab | no charts, no gamification |
| Settings | none | missing | REQUIRED | build (lightweight) | reset progress, about, disclaimer, version |
| About | none | missing | REQUIRED | build | concise, no overclaims |
| Medical/educational disclaimer | buried in case metadata only | missing as surface | REQUIRED | build | validation-pending stated here |
| Privacy/Terms | none | missing | NICE TO HAVE | honest placeholder status in About (no fake legal text) | needed before TestFlight |
| Educator Dashboard (10) | — | — | POST-MVP | exclude | QAN-034 |
| Case Creator | internal case factory (docs/skills) | different by design | NOT APPLICABLE (as UI) | keep factory internal | authoring is a repo workflow, not an app |

## What Qaniva borrows as product principles

Next-action clarity on Home · a Case Library that sells the content · a real
Briefing before the sim · the simulation→debrief loop as the core value ·
one-tap replay · visible progress.

## What Qaniva deliberately does differently

5 core simulation areas instead of 8 vertical action sections · timing/order/
state/causality-aware scoring (not flat critical/recommended lists) ·
learner-visible evidence traceability · a simpler Home (no daily case,
challenges, streaks, subscription) · no account requirement · governance
honesty in-product (educational disclaimer states that MVP clinical content is
pending validation).
