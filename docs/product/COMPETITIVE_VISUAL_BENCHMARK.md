# Qaniva competitive visual benchmark

**Research date:** 2026-09-01  
**Compared:** Qaniva baseline, Full Code Medical Simulation, Body Interact, Oxford Medical Simulation.  
**Sources:** current official URLs are indexed in `COMPETITIVE_VISUAL_REFERENCE_INDEX.md`; the historical Full Code teardown and 106-row inventory were reviewed first and treated as historical context.

## Qaniva's desired visual position

**Full Code's immediate clinical readability + Body Interact's mobile cleanliness + Oxford's long-term 3D credibility + Qaniva's timing/evidence-driven identity.**

Qaniva should feel modern, calm, premium, evidence-driven, clinical, mobile-native, intelligent and focused. It should not feel like a hospital information system, a video game, a generic teal healthcare template, an AI startup landing page, or Full Code with different colors.

Its differentiating visual grammar is:

**decision → time → patient response → evidence**

That sequence should appear in onboarding, in-simulation feedback, Results and Progress. Teal identifies the product and primary actions; green/red/amber identify labeled clinical meaning; ink and neutral surfaces carry the majority of the interface.

## Benchmark matrix

| Category | Competitor best practice | Qaniva current state | Qaniva opportunity |
| --- | --- | --- | --- |
| Brand identity | OMS is sober and clinical; Body Interact is clean and consistent | Strong ink/teal foundation and wordmark, but many screens remain generic without the name | Own time/causality/evidence motifs; use less container chrome, not more teal |
| Home clarity | Full Code emphasizes the next patient; Body Interact gives direct scenario access | Continue is first, but the same case is immediately repeated in the library | One unmistakable next action; compact secondary case previews |
| Case discovery | Body Interact uses distinct scenario cards and a scalable filtered library | Complete metadata but three visually identical tall cards | Add restrained case identity and tighter rows; do not build search/filter yet |
| Case briefing | Body Interact separates scenario details, goals and Start | Qaniva contains excellent context but presents it as one long bullet list | Separate known context/handoff from learner task and duration |
| Simulation readability | Full Code keeps patient, monitor and action navigation visible; Body Interact simplifies bottom tools | Vitals are visible, but drawer and rectangular tabs dominate | Preserve patient context, shorten result overlays, use Qaniva-selected states |
| Patient prominence | Body Interact makes patient the visual center; OMS makes face/pose readable | Patient is centered but small and visually flat in a long bed composition | Tighten camera target/field of view modestly; avoid expensive model rework |
| Clinical action navigation | Body Interact's bottom toolbar is fast to scan; Full Code offers breadth | Five categories are correct, but styling feels like a generic blue debug panel | Keep taxonomy, align tab/row spacing, teal selection, ink surfaces and clearer active state |
| Vitals readability | Full Code/Body Interact keep essential vitals persistently visible | Qaniva has a strong always-on top strip and physical monitor duplication | Retain redundancy, improve label contrast/spacing, align semantic color with shell |
| Investigation-result UX | Body Interact lets the clinical asset dominate with restrained controls | ECG is large but title collides and viewer has excessive white/competing controls | Dark framed viewer, safe header, fit-to-screen asset, compact zoom/Close, visible provenance |
| Results hierarchy | Full Code leads with score/categories; Body Interact stages Timeline, Performance, Knowledge | Score leads, but raw breakdown and many same-weight cards create fatigue | Outcome hero → critical decisions → timing/causality → timeline → alternatives → evidence |
| Debrief quality | Body Interact separates learning dimensions; Full Code explains critical actions | Qaniva content is stronger on timing, causality, alternatives and evidence | Make the existing differentiation visually obvious; preserve engine-owned facts |
| Progress | Body Interact provides organized performance views | Qaniva is honest and useful for two attempts, but card-heavy | Borderless metrics, compact per-case mastery, divided recents; stay small-data appropriate |
| Settings | Body Interact groups modest preferences simply | Correctly avoids fake toggles but uses a card for every row | Group rows with dividers, keep destructive action distinct, retain honest fixed values |
| 3D realism | OMS uses believable room scale, props, patient pose/expression and neutral lighting | Improved EARLY MVP; basic geometry/materials and procedural face remain obvious | Low-risk camera/lighting/material tuning only; production patient remains a later purchase decision |
| Mobile density | Body Interact's shell is sparse; Full Code proves high information density can stay actionable | Shell is readable; Results and briefing are long; Unity lower third is crowded | Use progressive hierarchy and compact metadata without hiding important clinical facts |
| Clinical credibility | OMS environment and Body Interact's asset presentation feel intentional | Deterministic/evidence content is highly credible; clipping and Unity mismatch reduce trust | Fix geometry defects first, align Unity styling, keep validation-pending disclaimers visible |
| Visual distinctiveness | Competitors have repeatable interaction patterns tied to their product | Qaniva is recognizable mostly through ink/teal and wordmark | Repeat a unique causal timeline/evidence signature across onboarding and debrief |

## Competitor-specific conclusions

### Full Code

Strongest principles: immediate scene comprehension, persistent vitals, breadth without hiding the patient, direct score-to-discussion path, and a clear deliberate-practice loop. The current App Store listing also verifies the product's next-patient/home emphasis and detailed scoring/debrief positioning.

Do not copy: its crowded edge controls, portrait chrome, small labels, bright clinical blue, or legacy panel density. Those patterns now feel dated on a modern phone and would undermine Qaniva's calmer identity.

### Body Interact

Strongest principles: a clean shell, direct scenario entry, patient-dominant simulation, compact bottom clinical tools, and a staged feedback system. The March 2026 manual is especially useful because it shows the current path from Scenario Feedback to Timeline, Performance, priority details, take-home messages and references.

Do not copy: its light palette, exact circular controls, orange/teal card language, scoring dimensions or layout. Qaniva should learn from the information architecture while keeping a distinct ink-based system and deterministic timing/causality focus.

### Oxford Medical Simulation

Strongest principles: believable clinical scale, neutral rooms, relevant equipment density, readable patient expression, purposeful lighting and visible interaction with the patient. Its official 2026 media also shows that functional fidelity can be convincing without photorealism.

Qaniva's long-term direction is improved anatomical/material fidelity and richer response animation, but the current sprint should only adjust camera, composition, lighting and interface coherence where risk is low.

## RN ↔ Unity coherence target

The transition should read as **Qaniva entering simulation mode**:

- same ink background and teal selection color;
- same rounded, low-contrast surface hierarchy;
- same text roles: compact labels, readable values, restrained metadata;
- patient and vitals remain dominant;
- result overlays use the same evidence/provenance language as Results;
- no change to clinical engine, bridge contract or Unity UI architecture.

