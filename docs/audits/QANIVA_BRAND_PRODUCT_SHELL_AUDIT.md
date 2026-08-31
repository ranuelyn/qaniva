# Qaniva brand & product-shell audit (2026-09-01)

**BRAND STATUS: MVP / PROVISIONAL.** No final trademarked logo exists. The MVP
mark is a text wordmark ("Qaniva" + teal period) and a provisional geometric
app icon (teal Q-ring with a tick notch on ink). Both are generated assets
(`scripts` not needed — icon/wordmark produced during this sprint and
committed into the iOS asset catalog); replace when a final identity is
commissioned. Visual proof: [`product-shell-screenshots/`](product-shell-screenshots/).

## Design tokens (`apps/mobile/src/theme/tokens.ts`)

- **Surfaces:** ink background `#0e1116`, surface `#171c23`, surfaceAlt,
  border. Dark, calm, clinical — deliberately not hospital-blue-only, no
  gradients/neon/glassmorphism.
- **Brand:** Qaniva teal `#3ec6b4` (CTAs, active tab, accents) with dark-on-teal
  button text.
- **Semantic:** success `#46c98b`, harmful `#e5484d`, warning/delayed
  `#e8a33d`, unnecessary `#8fa3b0` (visually neutral — never red), info,
  disabled. **Clinical meaning is never color-only**: every badge/section
  carries text (StatusBadge = dot + label; debrief sections are titled).
- **Typography scale:** display 32/800 · screenTitle 24/700 · sectionTitle
  17/700 · cardTitle 16/600 · body 15 (lh 21) · bodySecondary 14 · caption 12 ·
  button 16/600 · numeric 30/800. Body never below 14.
- **Rhythm:** spacing 4/8/16/24/32; radius 8/12/20/pill; touch targets ≥44 pt,
  buttons 52 pt.
- Enforced by review + grep: no hex/font-size/radius literals in screens
  (verified — only tokens.ts holds hex values).

## Components (`components/ui.tsx`, `components/CaseCard.tsx`)

Screen · Card · PrimaryButton · SecondaryButton · Title · SectionHeader ·
CardTitle · Body · Caption · Numeric · StatusBadge · BadgeRow · EmptyState ·
SettingsRow · Wordmark · CaseCard. Deliberately small; no generic component
library.

## Screen inventory & navigation

12 surfaces per [`docs/product/MVP_SCREEN_INVENTORY.md`](../product/MVP_SCREEN_INVENTORY.md),
all implemented and screenshot-evidenced (completeness table in the
screenshot README). Navigation: bottom tabs Home/Cases/Progress/Settings +
root stack for Onboarding/Briefing/Simulation/Results/About/Disclaimer;
Simulation is a task flow, never a tab. Deep links (`qaniva://…`) cover every
shell surface. First launch: Splash → Onboarding → Home; returning: Splash →
Home (persisted flag; verified live). Back behavior: sim abort → Briefing;
completion → Results (replace) → Home.

## Case-agnostic shell

The catalog derives manifests, teasers and **briefings directly from the
versioned case JSONs** (`metadata.briefing`, new schema field) — the previous
audit's content-in-code briefing/fallback maps are deleted. Adding case #4 =
author case.json + one import line. Remaining content-in-code (accepted,
test-gated): the Unity e2e driver's per-case ideal-path table.

## Full Code lessons applied / rejected

Applied as principles: next-action clarity (Home Continue), a case library
that sells content, real briefing, sim→debrief loop, one-tap replay, visible
progress. Rejected for MVP: daily case, challenges, subscription, accounts,
difficulty toggles (shown informationally only), search/filters at this
catalog size, educator surfaces. Nothing visual, textual or structural was
copied. Full matrix: [`docs/product/QANIVA_MVP_SCREEN_GAP_ANALYSIS.md`](../product/QANIVA_MVP_SCREEN_GAP_ANALYSIS.md).

## Accessibility

Buttons/rows carry `accessibilityRole`/labels; touch targets ≥44 pt; text
contrast on ink surfaces is high (teal-on-ink and text tokens); no icon-only
controls in the shell (tab icons pair with labels); clinical states are
labeled, never color-only. Not yet done: full VoiceOver pass, dynamic-type
stress test — listed as limitations.

## Bugs found & fixed during the sprint

1. **Unity window covered the RN error screen** on SIMULATION_FAILED (the
   full-screen Unity view stayed up; the friendly error was invisible). Fixed:
   transport `hide()` invoked on failure; evidence `22-error-state.png`.
2. **White pre-JS launch flash** — native window/root background now brand ink.
3. **E2E attemptId collision across cases** silently overwrote persisted
   attempts (idempotent store + reused hardcoded ids). E2E now uses fresh ids.

## Known visual limitations

- Splash wordmark displays sub-400 ms on the dev simulator (compiled + asset
  verified; not frame-capturable). Release builds show it longer.
- The RN "Preparing the simulation" state lasts <500 ms on this machine; the
  visible transition is Unity's own dark splash (Unity-license branding).
- Dev-build frames show the small Metro banner at launch and "E2E run n"
  titles in driver-run sim/results captures.
- Patient visual quality remains improved-EARLY-MVP (art purchase decision
  pending, see asset manifest); Results uses text-first hierarchy — tone
  colors in debrief cards are minimal by design but could be richer.
- No VoiceOver/dynamic-type audit yet.

## Remaining product-shell blockers (feeds the release-readiness phase)

Privacy Policy/Terms (honest placeholders only — must exist before
TestFlight) · final brand assets (wordmark/icon are provisional) · crash
reporting · physical-device validation (QAN-008, deferred) · VoiceOver pass.
