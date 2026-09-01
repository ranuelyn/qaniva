# Qaniva final visual polish plan

**Plan date:** 2026-09-01  
**Inputs:** 26-screen pixel audit, Full Code inventory/teardown, fresh official competitor research, current RN/Unity source inspection.  
**Scope guard:** polish only. No new navigation, persistence, engine behavior, case, backend, authentication, AI, environment or physical-device work.

## P0 — must complete

| Screen | Issue | Competitor principle/reference | Proposed change | Expected benefit | Risk |
| --- | --- | --- | --- | --- | --- |
| Onboarding | Pager captures overlap/repeat page 01 and final CTA never appears | Body Interact uses distinct, stable onboarding/briefing states | Replace module-level `Dimensions` width with live `useWindowDimensions`; give each page a distinct code-native clinical diagram; verify page state and CTA | Correct first-run story and stronger brand memory | Low: local layout/state only |
| Briefing / Settings / Disclaimer / Error | Horizontal clipping and status/header collisions | All serious references protect stable navigation and readable content bounds | Remove fixed-width assumptions/overflow, use safe-area-aware content and native headers consistently, add max-width/flex shrink where required | Restores comprehension and trust | Low–medium: verify all stack/tab states |
| ECG viewer | Title/status collision; medical asset and controls compete | Body Interact lets test/report assets dominate | Reframe viewer with compact safe header and footer, fit ECG to available viewport, retain watermark/provenance, reduce controls to zoom and Close | Makes the investigation clinically readable | Medium: Unity UI Toolkit sizing |
| Results | Evidence capture clips content beneath header | Body Interact's staged feedback maintains orientation | Correct scroll content inset and persistent footer behavior; ensure section starts are never obscured | Prevents lost clinical information | Low |
| Splash evidence | Baseline does not show Qaniva identity | Strong launch state must establish product ownership | Verify native launch assets and recapture a settled, final-code splash without simulator download chrome; do not add animation | Credible first impression | Low; capture timing may be environment-sensitive |

## P1 — high-value, implement

| Screen | Issue | Competitor principle/reference | Proposed change | Expected benefit | Risk |
| --- | --- | --- | --- | --- | --- |
| Home | Next action competes with repeated library cards | Full Code next-patient principle; Body Interact direct scenario access | Create a stronger next-action module and compact two-case preview rows; retain full library in Cases | Returning/first-run user knows what to do in <3 seconds | Low |
| Cases | Cards are visually identical and too tall | Scalable scenario-library principle | Add subtle case index/accent, tighten teaser/metadata/status, keep case-agnostic list | Looks scalable to 20 cases without adding search | Low |
| Briefing | Context, handoff and task are one bullet wall | Body Interact separates details/goals/Start | Use labeled Context/Handoff and Your task blocks, compact duration near title, sticky CTA | Faster prebrief comprehension | Low |
| Unity simulation | Blue rectangular UI feels separate from Qaniva | Patient-first Body Interact; Full Code persistent vitals | Tune USS/TSS to ink/teal tokens, rounded surfaces, clearer selected tab and less dominant drawer; modest camera crop | RN→Unity feels like one product; patient stays central | Medium: needs Unity compile and captures |
| In-sim result overlays | History/result text covers too much scene | Keep patient context visible while presenting results | Cap result area height, use clearer title/body/provenance styles, preserve scene around overlay | Lower cognitive obstruction | Medium |
| Results hero | Excellent content lacks progressive hierarchy | Full Code direct score; Body Interact staged feedback | Build compact outcome/score hero, causal “patient response” block, labeled criterion rows with semantic accents and evidence chips; reduce nested cards | Makes Results Qaniva's defining screen | Medium: layout only, no scoring changes |
| Results density | Same-weight cards and repeated evidence IDs cause fatigue | Detail-on-demand and learning dimensions | Group noncritical successes, render timeline as one connected list, keep references quiet and separated | Faster scan with no clinical information loss | Medium |
| Progress | Card-heavy for two attempts | Simple small-data progress | Borderless metrics, slimmer case rows, divided recent list | Useful from 1–10 attempts | Low |
| Settings/About/Disclaimer | Card-inside-document feeling | Body Interact modest Settings | Group rows with dividers; use short sections and controlled line length | More product-like, less prototype/legal wall | Low |
| Brand system | Distinctiveness relies mostly on teal | Qaniva's own timing/evidence identity | Add causal rail/evidence marker components; preserve teal and icon | More recognizable without logo redesign | Low |
| Patient/environment | Scene is flat and patient small | OMS neutral lighting/composition/scale | Modestly tighten portrait camera and adjust lighting/material contrast; no model surgery | Better physician-demo composition | Medium: visually verify both cases |

## P2 — only if cheap

| Screen | Issue | Proposed change | Expected benefit | Risk |
| --- | --- | --- | --- | --- |
| Progress empty | No direct CTA | Add “Choose a case” secondary action | Better empty-state completion | Low |
| About | Copy is long | Tighten prose without changing claims | Better scan | Low |
| App icon | Provisional | Keep unchanged; it remains legible at 60/180/1024 px | Avoids unnecessary brand churn | None |
| Loading | Unity license splash interrupts brand | Match surrounding background and minimize observable dwell; do not attempt license removal | Softer transition | Low |

## Explicit non-goals

- No clinical rule, score, case schema, bridge message or persistence change.
- No new search, filters, difficulty system, account, payment, AI or educator surface.
- No new environment or purchased/production patient model.
- No physical iPhone polling/build/watcher. QAN-008 remains deferred.
- No competitor layouts, wording, colors, icons, assets or screenshots copied into Qaniva.

## Validation and stopping rule

Implement all justified P0 and the P1 items above that remain local to presentation. Run the normal first-run, STEMI, anaphylaxis and governance flows in the iOS simulator; run TypeScript, case, clinical-core, Unity EditMode/PlayMode and simulator build checks. Recapture every MVP inventory state from final code, audit the new PNGs, fix any remaining P0, then stop. P2 work is not a reason to prolong the sprint.

