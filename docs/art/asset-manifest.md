# Asset manifest

Policy: prefer procedurally/Unity-created assets (class A/B); external assets only
with verified licenses, recorded here before import. See
`docs/architecture/3d-presentation.md` for the authoring model.

## Current state (QAN-002 foundation)

**Zero external assets.** Every 3D presentation asset is generated from Unity
primitives + URP Lit materials by `QanivaPresentationAssets.CreateAll` and
committed as prefab YAML.

| Asset | Purpose | Source | License | Path | Approx. size | Status | Replacement plan |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ed_resus_v1.prefab` | reusable ED/resus room (shell, bed, IV pole, cart, wall props, lights, camera, monitor instance) | generated (primitives) | n/a (first-party) | `Assets/Qaniva/Resources/Qaniva/Environments/` | ~150 KB YAML | active | production room model or purchased modular hospital kit (license-verified) later |
| `adult_neutral_v1.prefab` | patient base (supine, breathing chest, skin parts, procedure anchors) | generated (primitives) | n/a | `Assets/Qaniva/Resources/Qaniva/Patients/` | ~30 KB YAML | active | rigged humanoid + animation set later (QAN-020) |
| `BedsideMonitor.prefab` | snapshot-driven vitals monitor | generated (primitives + TextMesh) | n/a | `Assets/Qaniva/Resources/Qaniva/Props/` | ~30 KB YAML | active | modelled monitor + nicer screen art later |
| `Assets/Qaniva/Materials/*.mat` (13) | shared URP Lit set (wall/floor/metal/plastics/mattress/blanket/gown/skin/screen/accent/IV/emissive) | generated | n/a | `Assets/Qaniva/Materials/` | ~4 KB each | active | textured PBR materials later (≤2K, mobile-compressed) |
| `LegacyRuntime.ttf` (monitor text) | TextMesh font | Unity built-in | Unity | built-in resource | 0 (built-in) | active | SDF text with production monitor art |

## Rules for adding an external asset

Record here BEFORE import: name, source URL, license (must permit commercial
use), attribution requirement, file size, poly count (if mesh), and why a
generated placeholder is insufficient. No Sketchfab rips, no unclear licenses,
no multi-hundred-MB packs, textures ≤2K with mobile compression. Git LFS is not
initialised — adding binary art requires `git lfs install` first
(`.gitattributes` rules already exist).
