# Qaniva visual screen audit

**Audit date:** 2026-09-01  
**Baseline:** `docs/audits/product-shell-screenshots/` (26 PNG files, inspected individually)  
**Scope:** current final-code iPhone simulator captures. This is a visual audit, not a feature or architecture audit.

## Executive finding

Qaniva already has a coherent ink-and-teal shell, readable typography, a credible case-to-debrief loop, and unusually strong evidence traceability for an MVP. It does not yet present as release-polished. The largest trust problems are not taste questions: several captures show content colliding with the iOS status/header area, horizontal clipping, repeated onboarding content, and an investigation viewer whose controls compete with the medical asset. Results contains excellent information but gives too much of it equal visual weight.

The visual position to preserve is calm, evidence-driven and mobile-native. The polish pass should strengthen Qaniva's timing-and-causality identity, reduce generic card repetition, and make React Native and Unity feel deliberately related. It should not imitate Full Code's crowded side controls or Body Interact's light teal shell.

## Rating summary

- **PASS (2):** `16-progress-empty.png`, `19-about.png`
- **NEEDS POLISH (13):** `04`, `05`, `06`, `07`, `09`, `10`, `12`, `13`, `15`, `17`, `21`, `23`, `24`
- **NEEDS REDESIGN (11):** `00`, `01`, `02`, `03`, `03b`, `08`, `11`, `14`, `18`, `20`, `22` (several are one shared safe-area/width defect rather than eleven independent redesigns)

“Needs redesign” here means the captured state cannot be signed off through cosmetic tuning alone. It does **not** authorize new navigation or architecture.

## Per-screen audit

| Filename | Screen/state | Classification | Strongest element | Biggest issue | Severity | Recommended fix |
| --- | --- | --- | --- | --- | --- | --- |
| `00-splash.png` | Native splash | NEEDS REDESIGN | Ink launch color avoids a white flash | No Qaniva identity is visible; simulator download chrome dominates | P0 | Ensure the native wordmark/mark is visible for the actual launch window and recapture without install/download chrome |
| `01-onboarding-01.png` | Onboarding 1, transition | NEEDS REDESIGN | Strong wordmark and confident display type | Two layouts overlap during capture, producing a broken first impression | P0 | Remove transient double-layout capture state; use stable page width and recapture after settling |
| `02-onboarding-02.png` | Onboarding 2 | NEEDS REDESIGN | Clear CTA and restrained palette | Shows page 01 content; large dead zone and no explanatory visual | P0 | Make pager width responsive, verify route/page state, and give each page a distinct lightweight clinical diagram |
| `03-onboarding-03.png` | Onboarding 3 | NEEDS REDESIGN | Consistent typography | Repeats page 01; timing/order story is absent | P0 | Render the actual third page and introduce an ordered timeline/causality visual |
| `03b-onboarding-04.png` | Onboarding 4 | NEEDS REDESIGN | CTA remains reachable | Repeats page 01 and still says Next; debrief story never appears | P0 | Render final-page content, change CTA to Get started, and show evidence/debrief motif |
| `04-home-empty.png` | Home, first run | NEEDS POLISH | Clear Cases section and visible first case | “Choose your first case” is passive and the screen repeats large case cards immediately below | P1 | Turn the top module into one explicit “Start first case” action and show only two compact case previews |
| `05-home-with-progress.png` | Home, returning | NEEDS POLISH | Continue/Replay is correctly first | Continue and first library card duplicate the same case at similar weight | P1 | Strengthen one next-action card and demote the library to compact secondary rows |
| `06-cases.png` | Case library | NEEDS POLISH | Titles, teaser, specialty and status are all present | Three nearly identical dark cards have weak case differentiation and heavy vertical density | P1 | Add restrained case accents/indexes, tighten metadata, preserve scalable list structure |
| `07-stemi-briefing.png` | STEMI briefing | NEEDS POLISH | Honest role, setting, resources and task | Dense bullet wall; “what you know” and “what you must do” are not separated | P1 | Split into compact Context/Handoff and Your task sections; keep diagnosis hidden |
| `08-anaphylaxis-briefing.png` | Anaphylaxis briefing | NEEDS REDESIGN | Correct content exists | Left edge and navigation are clipped; comprehension and trust are damaged | P0 | Fix screen width/safe-area layout, then apply the shared briefing hierarchy |
| `09-stemi-simulation.png` | STEMI simulation | NEEDS POLISH | Vitals always visible; patient and monitor share one frame | Patient is visually small/flat; action surface uses blue and hard rectangles unrelated to shell | P1 | Use Qaniva ink/teal UI tokens, slightly increase patient prominence, soften panel hierarchy |
| `10-anaphylaxis-simulation.png` | Anaphylaxis simulation | NEEDS POLISH | Patient remains visible while history result is open | Result overlay consumes too much scene and the action drawer dominates the lower half | P1 | Cap overlays, improve text hierarchy, keep patient/status context visible |
| `11-ecg-viewer.png` | ECG result viewer | NEEDS REDESIGN | ECG asset is large and legible | Title hits status area; excessive white canvas; controls and underlying action UI compete with the asset | P0 | Build a contained dark viewer header/footer, fit ECG to viewport, preserve watermark, keep only Close and compact zoom controls |
| `12-stemi-results-top.png` | Results, top | NEEDS POLISH | Score and critical decisions are visible | Raw score breakdown, summary and criteria form a long text stack with weak causal hierarchy | P1 | Create outcome hero, compact score domains, and status-accented critical decision rows |
| `13-stemi-results-timeline.png` | Results, mid/timeline | NEEDS POLISH | Teaching content is evidence-specific | Capture starts mid-stream with no orientation; dense prose causes scrolling fatigue | P1 | Add anchored section treatment, group related content, use progressive disclosure for secondary teaching detail |
| `14-stemi-results-evidence.png` | Results, evidence | NEEDS REDESIGN | References are present and specific | First row is clipped beneath header; evidence is presented as a long undifferentiated block | P0 | Correct scroll/header inset and style references as quiet evidence entries with visible provenance labels |
| `15-anaphylaxis-results.png` | Anaphylaxis Results | NEEDS POLISH | “What happened to the patient” makes causality explicit | Score, summary, outcome and criteria still share similar typographic weight | P1 | Apply the same Results hero and causal decision-row system |
| `16-progress-empty.png` | Progress, empty | PASS | Honest, calm empty state sized for MVP | No direct CTA to Cases | P2 | Add a low-emphasis “Choose a case” action if cheap |
| `17-progress-with-attempts.png` | Progress, populated | NEEDS POLISH | Useful 2/3 and attempt metrics | Too many bordered cards and repetitive metadata; recent attempts lack scan structure | P1 | Use borderless metric strip, slimmer per-case rows, and divided recent-attempt list |
| `18-settings.png` | Settings | NEEDS REDESIGN | No fake toggles; destructive action is clearly red | Horizontal clipping removes Home/Cases tabs and shifts content | P0 | Fix width/overflow, group rows with dividers instead of separate cards, retain modest scope |
| `19-about.png` | About | PASS | Clear product explanation and honest validation status | Long line lengths and multiple prose blocks feel document-like | P2 | Shorten copy and use subtle section dividers; no new cards needed |
| `20-medical-disclaimer.png` | Disclaimer | NEEDS REDESIGN | Required educational boundary is explicit | Back/title collision and clipped heading; paragraph wall harms legal comprehension | P0 | Fix navigation/safe area and introduce short labeled sections with controlled line length |
| `21-loading-state.png` | Unity launch | NEEDS POLISH | Dark background prevents a flash | “Made with Unity” creates a sharp brand interruption | P1 | Keep the preceding RN transition unmistakably Qaniva; if license splash cannot change, minimize dwell and ensure seamless background color |
| `22-error-state.png` | Simulation error | NEEDS REDESIGN | Recovery action is clear | Wordmark collides with status time; raw engine detail is too prominent | P0 | Respect safe area, lead with friendly recovery copy, tuck technical detail into secondary styling |
| `23-replay-state.png` | Briefing, replay | NEEDS POLISH | Previous attempt and one-tap replay are visible | Same briefing wall and oversized empty lower region | P1 | Apply structured briefing, compact recent-attempt row, keep sticky Play again CTA |
| `24-case-completed-home.png` | Home after completion | NEEDS POLISH | Continue loop works and completed state is labeled | Duplicate same-case content and tall cards weaken the next-action principle | P1 | Preserve one dominant replay/continue card and make library items compact |

## Cross-screen system audit

### Brand distinctiveness

The ink foundation, high-contrast white type, teal period and restrained teal CTA are already recognizable as a family. Without the Qaniva wordmark, however, most shell screens could still be a generic dark healthcare app. The distinctive opportunity is not more teal. It is the repeated visual language of **time, decision, response, evidence**: timestamp rails, causal connectors, compact evidence markers, and a controlled ink/surface hierarchy.

### Color

`#3ec6b4` remains suitable as the brand anchor and should stay. It is not overused in the shell; the bigger problem is that Unity substitutes a generic medium blue for selected tabs and actions. Use teal for selection/primary action, reserve green/red/amber for labeled clinical semantics, and let neutral ink surfaces do more work. This also keeps Qaniva distinct from Body Interact's predominantly light/teal identity.

### Typography and spacing

The token scale is sensible, but screenshots expose three issues: large display titles wrap aggressively, Results uses body text where metadata/captions should carry load, and vertical rhythm is often produced by stacking bordered cards. Keep the token scale, add no new font family, reduce unnecessary bolding, and enforce the 24-point screen gutter / 16-point card padding rhythm.

### Cards and icons

Ionicons gives the bottom navigation a consistent family and acceptable stroke weight. Keep it. Cards are overused on Home, Results, Progress and Settings. Replace containers with spacing/dividers where the grouping is already obvious. The provisional app icon remains recognizable as a Q at 1024, 180 and ~60 px; the small white notch remains legible. **Keep it for MVP.**

### Accessibility

The baseline has good nominal contrast and labels semantic states in text. P0 accessibility defects are geometric: clipped text, status-bar collisions and controls outside the stable viewport. High-value P1 work is to preserve 44-point targets, avoid color-only Results rows, add accessible labels to any new onboarding diagram, and keep body copy at or above the current 15-point token. Full VoiceOver and dynamic-type stress testing remains release-readiness scope.

## Initial verdict by product area

- **Shell:** coherent foundation, but safe-area and overflow defects prevent sign-off.
- **Onboarding:** conceptually correct, visually repetitive, and the captured sequence is functionally wrong.
- **Simulation:** readable but visibly a different application; UI coherence is the cheapest meaningful win.
- **Patient/environment:** honest improved EARLY MVP; low-risk composition/material improvements only.
- **Results/debrief:** strongest product content, weakest density management; this is the hero-screen opportunity.
