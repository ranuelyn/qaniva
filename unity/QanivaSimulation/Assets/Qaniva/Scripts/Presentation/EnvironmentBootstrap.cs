using UnityEngine;
using Qaniva.Bridge;
using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Deterministic 3D presentation bootstrap. On every SimulationStarted it reads
    /// the case's presentation profile (roomKey/patientVariant from case.json via
    /// the engine), resolves prefabs through <see cref="PresentationRegistry"/>,
    /// and (re)composes the scene:
    ///
    ///   environment prefab  = room + lights + camera + "PatientAnchor" + monitor
    ///   patient prefab      = instantiated at the anchor
    ///
    /// On every SnapshotUpdated it forwards canonical values:
    ///   snapshot -> PatientPresentationMapper -> PatientVisualController
    ///   snapshot -> BedsideMonitorView
    ///
    /// Unknown keys fail loudly (error log, nothing instantiated, the screen-space
    /// UI stays fully functional) — never a silent wrong room. A warm-runtime
    /// relaunch with the same roomKey reuses the room; the patient presentation is
    /// reset so no visual state leaks between attempts.
    /// </summary>
    public sealed class EnvironmentBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToBridge()
        {
            var bridgeGo = GameObject.Find(BridgeBootstrap.BridgeGameObjectName);
            if (bridgeGo == null || bridgeGo.GetComponent<EnvironmentBootstrap>() != null)
            {
                return;
            }
            if (bridgeGo.GetComponent<SimulationBridgeController>() == null)
            {
                return;
            }
            bridgeGo.AddComponent<EnvironmentBootstrap>();
        }

        private SimulationBridgeController _controller;
        private GameObject _environmentInstance;
        private string _environmentRoomKey;
        private GameObject _patientInstance;
        private string _patientVariantKey;
        private PatientVisualController _patient;
        private BedsideMonitorView _monitor;

        public GameObject EnvironmentInstance => _environmentInstance;
        public PatientVisualController Patient => _patient;
        public BedsideMonitorView Monitor => _monitor;

        private void Awake()
        {
            _controller = GetComponent<SimulationBridgeController>();
            if (_controller != null)
            {
                _controller.SimulationStarted += OnSimulationStarted;
                _controller.SnapshotUpdated += OnSnapshot;
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.SimulationStarted -= OnSimulationStarted;
                _controller.SnapshotUpdated -= OnSnapshot;
            }
        }

        private void OnSimulationStarted()
        {
            var profile = _controller.GetPresentationProfile();
            if (profile == null)
            {
                Debug.LogError("[EnvironmentBootstrap] no presentation profile on the loaded case — no 3D scene composed");
                return;
            }

            ComposeEnvironment(profile);
            ComposePatient(profile);

            // Presentation must start clean on the warm runtime and immediately
            // reflect the initial canonical snapshot.
            if (_patient != null)
            {
                _patient.ResetPresentation();
            }
            OnSnapshot(_controller.CurrentSnapshot);
        }

        private void ComposeEnvironment(PresentationProfileView profile)
        {
            if (_environmentInstance != null && _environmentRoomKey == profile.RoomKey)
            {
                return; // same room: reuse (warm relaunch)
            }

            if (_environmentInstance != null)
            {
                Destroy(_environmentInstance);
                _environmentInstance = null;
                _monitor = null;
            }

            var path = PresentationRegistry.ResolveEnvironment(profile.RoomKey);
            if (path == null)
            {
                Debug.LogError($"[EnvironmentBootstrap] unknown roomKey \"{profile.RoomKey}\" — no environment loaded");
                return;
            }
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[EnvironmentBootstrap] environment prefab missing at Resources/{path}");
                return;
            }

            _environmentInstance = Instantiate(prefab);
            _environmentInstance.name = $"Environment_{profile.RoomKey}";
            _environmentRoomKey = profile.RoomKey;
            _monitor = _environmentInstance.GetComponentInChildren<BedsideMonitorView>();

            DisableFallbackCameras();
        }

        private void ComposePatient(PresentationProfileView profile)
        {
            if (_patientInstance != null && _patientVariantKey == profile.PatientVariant)
            {
                return; // same patient visual: reuse
            }

            if (_patientInstance != null)
            {
                Destroy(_patientInstance);
                _patientInstance = null;
                _patient = null;
            }

            if (_environmentInstance == null)
            {
                return; // no room, nowhere to anchor — already reported
            }

            var path = PresentationRegistry.ResolvePatient(profile.PatientVariant);
            if (path == null)
            {
                Debug.LogError($"[EnvironmentBootstrap] unknown patientVariant \"{profile.PatientVariant}\" — no patient loaded");
                return;
            }
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[EnvironmentBootstrap] patient prefab missing at Resources/{path}");
                return;
            }

            var anchor = _environmentInstance.transform.Find("PatientAnchor");
            if (anchor == null)
            {
                Debug.LogError("[EnvironmentBootstrap] environment prefab has no PatientAnchor");
                return;
            }

            _patientInstance = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
            _patientInstance.name = $"Patient_{profile.PatientVariant}";
            _patientVariantKey = profile.PatientVariant;
            _patient = _patientInstance.GetComponent<PatientVisualController>();
        }

        private void OnSnapshot(SimulationSnapshotView snapshot)
        {
            if (snapshot == null)
            {
                return;
            }
            if (_patient != null)
            {
                _patient.Apply(PatientPresentationMapper.Map(snapshot), snapshot.RrPerMin);
            }
            if (_monitor != null)
            {
                _monitor.SetVitals(snapshot);
            }
        }

        /// <summary>The environment prefab brings its composed camera; any scene
        /// fallback camera is disabled so exactly one camera renders.</summary>
        private void DisableFallbackCameras()
        {
            var envCamera = _environmentInstance != null
                ? _environmentInstance.GetComponentInChildren<Camera>()
                : null;
            if (envCamera == null)
            {
                return;
            }
            foreach (var cam in Camera.allCameras)
            {
                if (cam != envCamera)
                {
                    cam.gameObject.SetActive(false);
                }
            }
        }
    }
}
