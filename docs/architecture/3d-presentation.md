# 3D presentation foundation

How the reusable ED/resus presentation works (QAN-002). Invariant everywhere:
**presentation reacts to clinical truth; it never becomes clinical truth**
([ADR-003](../adr/ADR-003-deterministic-engine-owns-clinical-truth.md)).

```
Clinical Core ──► SimulationSnapshot ──► PatientPresentationMapper ──► PatientVisualState
                        │                                                   │
                        │                                                   ▼
                        │                                        PatientVisualController
                        │                                        (breathing amplitude, skin tint)
                        └────────────────────────► BedsideMonitorView (HR/BP/SpO2/RR/clock)
```

## Environment selection (case → room, no new scenes)

A case selects its presentation **only** through the `presentationProfile` block it
already carries in `case.json` (clinical truth and art selection stay separated at
the schema level — no schema change was needed):

```
case.json presentationProfile { roomKey, patientVariant, cameraPreset, ... }
  → engine parses it → IClinicalRuntime.GetPresentationProfile() (pass-through)
  → EnvironmentBootstrap (on SimulationStarted)
  → PresentationRegistry: roomKey  → Resources/Qaniva/Environments/<key>.prefab
                          variant  → Resources/Qaniva/Patients/<key>.prefab
  → instantiate room; instantiate patient at the room's PatientAnchor
```

- The **environment prefab** owns: room geometry, lights, the composed portrait
  camera, `PatientAnchor`, and a nested `BedsideMonitor` prefab instance.
- **Unknown keys fail loudly** (error log, nothing composed, screen-space UI stays
  fully usable). Never a silent wrong room.
- **Warm relaunch**: same `roomKey` reuses the room instance; the patient's
  presentation state is reset and re-derived from the fresh canonical snapshot
  (PlayMode-tested: exactly one room, one patient, reset monitor + visual state).

### Adding a second environment / patient visual

1. Generate or author a prefab under `Assets/Qaniva/Resources/Qaniva/Environments/`
   (must contain a camera, lights, `PatientAnchor`, and a `BedsideMonitorView`)
   or `.../Patients/` (must follow the patient prefab contract below).
2. Add one entry to `PresentationRegistry`.
3. Reference the key from a case's `presentationProfile`. No gameplay code changes.

Planned library shape (documented, deliberately not built): environments
`ed_resus_01 → ambulance_01 → ward_01 → icu_01`; patient visuals
`adult_neutral_v1 → adult_male_01 / adult_female_01 / …`.

## Patient prefab contract

```
PatientRoot (PatientVisualController)
├── Head, HandLeft, HandRight     — share one instanced "skin" material
├── Chest                          — procedural breathing transform
├── Pillow/Pelvis/Arms/Legs/Blanket — dressing geometry
└── AnchorHead / AnchorChest / AnchorLeftArm / AnchorRightArm
                                   — generic future-procedure attachment points
```

## Patient visual state (presentation-only)

`PatientPresentationMapper.Map(snapshot)` — pure, deterministic, non-mutating
(EditMode/PlayMode tested):

| Canonical input (engine enums) | PatientVisualState | Visual result |
| --- | --- | --- |
| `circulation == arrest` | `Unresponsive` | no respiratory motion, grey skin |
| `neuro == unresponsive` | `Unconscious` | minimal breathing, pale skin |
| `circulation ∈ {shock, poor_perfusion}` or `neuro ∈ {pain, voice}` | `Distressed` | laboured (larger-amplitude) breathing, pale skin |
| otherwise | `Normal` | calm breathing, normal skin |

Breathing **rate** comes from the canonical `RrPerMin`; the visual state only
shapes amplitude/character. The skin tint is a generic "looks worse" cue, not a
diagnostic claim. No canonical field was added for animation.

## Bedside monitor

World-space readout (dark screen + `TextMesh` labels — chosen over world-space
UI Toolkit, which is not stable in this Unity version, and over render-texture
UI, which costs an extra pass). Renders **only** snapshot fields: HR, BP, SpO2,
RR, sim clock. No waveforms (waveforms are not canonical data — decorative fake
ECG is deliberately avoided), no alarm thresholds (thresholds would be clinical
logic). The screen-space vitals bar remains the guaranteed-legibility readout;
the 3D monitor provides spatial believability with the same canonical numbers.

## Camera & lighting

One composed portrait camera per environment prefab (`bedside_01` composition:
patient centred between the top vitals bar and the bottom action UI, monitor
upper-right). No player camera control, no cinematic movement. Lighting: one
soft-shadow directional key + one shadowless fill + flat ambient (set in the
Bootstrap scene). URP mobile profile: shadow distance 12 m, 1024 shadow map,
MSAA 4x, HDR off (`QanivaBuild.ConfigureUrp`).

## Authoring model (why code-generated primitives)

All presentation assets are built by
`QanivaBuild`/`QanivaPresentationAssets.CreateAll` from Unity primitives + a
small shared URP material set (~13 materials), then **committed as normal
prefabs**. Consequences: zero external assets (nothing to license or track —
see `docs/art/asset-manifest.md`), tiny repo cost (prefab YAML), reproducible
headlessly, and trivially replaceable piece-by-piece with production art later
(swap prefab internals; the contracts — `PatientAnchor`, child names,
`BedsideMonitorView` labels — stay).

Composition iteration without device builds: run the PlayMode suite with
`QANIVA_CAPTURE_DIR=<dir>` to dump real-pipeline portrait captures
(`PresentationPlayModeTests`). Note: captures convert linear→sRGB explicitly.

## Deliberately NOT built yet

Addressables (all assets ship in the bundle — revisit when multiple
environment/patient packs need runtime delivery/versioning); humanoid rig +
animation clips (procedural breathing is the placeholder); a second environment
or patient visual (registry proves the boundary); waveforms; equipment
interaction (defib/vent/IV mechanics); avatar customization.
