# Simulation scene, flow and asset research (2026-09-02)

Inputs: owner-supplied Full Code reference stills (four App Store frames), the
Full Code App Store listing and fullcodemedical.com, Body Interact public help
pages, prior competitive benchmark (`COMPETITIVE_VISUAL_BENCHMARK.md`), and
asset-source checks. **Nothing is copied**: layouts, wording, artwork and
branding stay Qaniva's; what follows is principle extraction.

## What the Full Code reference frames actually do (composition analysis)

| Element | Observation | Qaniva translation (this sprint) |
| --- | --- | --- |
| Camera | Elevated three-quarter view from the **foot end**, slightly to one side; the bed runs diagonally toward the back wall; ~50° vertical FOV feel | Presentation camera moved to the foot-left, elevated (−1.05, 1.78, −1.95) looking at the torso, FOV 50 |
| Patient | **Semi-recumbent** (head of bed raised ~30°), face and chest readable, legs visible to the feet, gown, bare feet, IV/oxygen props | Torso bones posed up (Spine/Chest/Head ≈32° total), bed gets a raised backrest section; feet remain in frame |
| Monitor | **Wall/arm-mounted at the head-right, turned to the viewer**, waveform + four numeric vitals in clinical color coding | Bedside monitor moved to head-right and yawed toward the new camera; the top vitals strip stays the primary readout |
| Header | Case number + chief complaint + compact vitals row, hamburger | Qaniva keeps the humanized vitals tiles + status strip (no case number, no hamburger) |
| Category rails | Vertical side tabs (Patient/Exam/Stabilize/Differential left; Investigate/Intervene/Communicate/Hand-off right) | **Not copied** — Qaniva's bottom action sheet + underline dock is deliberately different and thumb-reachable |
| People | Attending/nurse portrait bubbles with speech callouts | Not adopted (no NPC layer in MVP) |
| Bottom bar | Log · back · forward · sound round buttons | Qaniva: quiet Case log / Exit text utilities inside the sheet header |

Take-aways applied: the **angle** and **pose** are what make Full Code's scene
read as "a patient in a room" instead of "a mannequin on a slab"; the monitor
must face the viewer; the lower third belongs to interaction.

## Flow / entry principles (Full Code, Body Interact)

- Next-patient-first home; case library by specialty; short intro; simulation;
  score → debrief with critical/recommended/unnecessary/harmful buckets;
  replay loop. Qaniva already implements this loop (Home → Cases → Briefing →
  Simulation → Results) and keeps its own timing/causality/evidence identity.
- Body Interact: patient-first canvas, compact bottom tools, staged feedback
  (Timeline → Performance → take-home messages). Qaniva's Results order matches
  in spirit (outcome → critical → causality → timeline → evidence).
- Localization: Full Code is English-only (App Store listing). **Qaniva ships
  Turkish as its product language** (see below) — a real differentiator for
  the Turkish medical-education market.

## Ready-made 3D human asset (licensing-clean options)

| Source | License | Fit | Decision |
| --- | --- | --- | --- |
| Quaternius — Universal Base Characters (Standard) | CC0 | 6 rigged humanoids (~13k tris), FBX/glTF, hairstyles; needs gown material + lying pose | **Selected** — downloaded to the working scratchpad; integration tracked in QAN-020/021 (rig retarget to Qaniva's bone-name contract: Spine/Chest/Neck/Head, Skin material) |
| Sketchfab CC0 rigged humans | CC0 / CC-BY per model | Variable quality; API download requires OAuth | Fallback |
| Poly Pizza (Google Poly archive) | CC0 / CC-BY | Low-poly stylized | Not clinical enough |
| Mixamo | Adobe terms (login) | Good rigs | Rejected (login/terms) |
| Meshy / TurboSquid / CGTrader "free" | mixed, often non-CC | — | Rejected for provenance |

The Blender MCP add-on installed on this machine predates the Sketchfab/Poly
Pizza commands, so asset retrieval is done via direct download + Blender
scripting rather than the MCP asset tools.

## Turkish product language — decisions

- Product language is Turkish everywhere the learner reads: RN shell (all
  screens), Unity simulation UI (dock, rows, statuses, vitals captions, viewer,
  case log), and the **case content itself** (titles, briefings, action labels,
  result narratives, criteria labels, terminal states, debrief, references).
- Engine identifiers (action ids, state enums, flags, evidence ids) are never
  translated — they are contracts. Display mapping lives in presenters
  (`VitalsPresenter.Humanize`, RN `*_TR` maps).
- Clinical terminology follows Turkish ED usage: adrenalin (not epinefrin),
  PKG (primer perkütan koroner girişim), AKS, NSAİİ, TA/SS/NABIZ captions,
  damar yolu, kateter laboratuvarı, triyaj kategorisi.
- Golden replays are unaffected (they hash state/score, not labels); the two
  tests that asserted English display strings were updated.
- Clinical status is unchanged: translated content remains
  `mvp_demo_approved`, clinical validation pending — the Turkish wording itself
  is part of what the reviewing clinician should check.

## Addendum (2026-09-02, second pass) — pose, camera, edge rails

**Lying-patient asset search.** Searched for ready "hospital patient lying" rigs
with a clean license: CGTrader/TurboSquid/Free3D listings are mostly paid or
license-unclear; Meshy "CC0" library is AI-generated and unrigged; Sketchfab
downloads need OAuth. Decision: keep the CC0 Quaternius rig and **pose it
properly in Blender** (reproducible, license-clean): arms adducted and rotated
back into the mattress plane, elbows slightly bent, palms down with relaxed
fingers, slight external leg rotation, plantar-flexed feet, torso and head on
the raised backrest; a shrink-wrapped gown mesh replaces the painted torso
region. Iterated through eight headless previews (`CapturePreview`) until the
figure read as "lying", not "standing on its back".

**Camera.** Matches the Full Code reference frames' principle rather than the
first pass: from the **foot end, centred and elevated** (−0.38, 1.98, −2.55 →
(0.02, 0.72, 0.62), FOV 45) so the whole patient sits in the middle third and
the monitor on the far wall faces the viewer.

**Edge rails.** Category navigation moved from a bottom sheet to **rotated
tabs on both screen edges** (left: Hasta, Muayene; right: İstemler, Tedavi,
Diğer) — the thumb-reach principle behind Full Code's side tabs, with Qaniva's
own styling (ink pills, teal active). A category opens a side panel of
decision rows from its own edge; re-tapping or ✕ closes it so the patient owns
the scene. Utilities (Vaka günlüğü, Çık) are a floating pill bar at the
bottom. Element names (`tab-*`, `action-*`) are unchanged for the driver/tests.

## Addendum (2026-09-02, third pass) — pose fit, gown, monitor, Sketchfab check

- **Owner-suggested Sketchfab models** (Patient, Female Patient With Gown, Patient
  Monitor, stretcher trolley, …) were checked via the public API: all but one are
  **Standard/Editorial licence and not downloadable**; the only downloadable one
  ("Patient hurt holding his stomach", CC-BY, 10k faces) still requires a
  Sketchfab API token to fetch. They remain **direction references** (a proper
  gown silhouette, a waveform monitor); nothing from them is used.
- **Pose fit:** arms lowered less into the mattress plane so forearms rest on
  the sheet instead of sinking; gown mesh rebuilt as a looser shrink-wrap with
  closed shoulders, a rounded neckline and thickness (≈40k tris total).
- **Monitor:** rebuilt in the reference style — lower stand (screen at ~1.16 m
  so it clears the vitals strip), an emissive ECG waveform strip across the
  top, and monitor-convention colours (HR green, SpO2 cyan, NIBP red, RR
  yellow); labels NABIZ / SpO2 / TA / SS.
