# Scenes (create in the Unity Editor)

Scene `.unity` files are not committed by the foundation because they require the
Editor to author and would otherwise be broken YAML. Create these by hand:

| Scene | Contents | Notes |
| --- | --- | --- |
| `Bootstrap.unity` | Empty GameObject `SimulationBridge` with `SimulationBridgeController` (and, on device, `NativeUnityBridge`). | First entry in Build Settings. `DontDestroyOnLoad`. |
| `ED_Resus.unity` | Blockout resus room: bed, `VitalMonitor` prefab, IV pole, crash cart, fixed camera at `bedside_01`, one baked directional light. | Single reusable room (blueprint §3, ADR-005). No free-roam camera. |

Keep to the MVP 3D budget: 1 room, 1 bed, 1 monitor, 1 trolley, 1 oxygen/IV set,
2 patient looks, 6–10 animations (blueprint §3).
