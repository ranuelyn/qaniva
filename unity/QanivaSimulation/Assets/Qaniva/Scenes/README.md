# Scenes

| Scene | How it's made | Contents |
| --- | --- | --- |
| `Bootstrap.unity` | **Generated** — `Qaniva.EditorTools.QanivaBuild.CreateMinimalScene` (run by `scripts/export-unity-ios.sh`, or from the Editor via batchmode). Committed once generated. | Camera, directional light, primitive bed + capsule "patient" placeholder. The bridge (`BridgeBootstrap`) and the `IntegrationHud` self-attach at runtime — the scene needs no wiring. |
| `ED_Resus.unity` | Manual, later (QAN-002) | Blockout resus room: bed, `VitalMonitor` prefab, IV pole, crash cart, fixed `bedside_01` camera, one baked directional light. Single reusable room (ADR-005 blueprint rule); no free-roam camera. |

Keep to the MVP 3D budget: 1 room, 1 bed, 1 monitor, 1 trolley, 1 oxygen/IV set,
2 patient looks, 6–10 animations (blueprint §3).
