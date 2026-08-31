using System.Collections.Generic;
using UnityEngine;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Applies a mapped <see cref="PatientVisualState"/> to the patient prefab:
    /// procedural breathing (rate from the CANONICAL respiratory rate,
    /// character/amplitude from the visual state) and a generic skin-tone shift so
    /// state changes read in stills as well as motion.
    ///
    /// Works with both patient generations behind one contract:
    ///  - primitive prefab (adult_neutral_v1): "Chest" child is scaled; skin
    ///    renderers found by the legacy child names Head/HandLeft/HandRight.
    ///  - rigged prefab (adult_rigged_v1): the "Chest" BONE is translated along
    ///    the patient's up axis (chest/shoulder rise); skin materials found by
    ///    material name containing "Skin" (bone + material names are part of the
    ///    generator contract, scripts/generate-patient-blender.py).
    ///
    /// Presentation only: this component receives the mapped state + display
    /// values; it never reads clinical fields, never decides state, and the tint
    /// is a generic "looks worse" cue, not a diagnostic claim. Animation NEVER
    /// feeds back into clinical state.
    /// </summary>
    public sealed class PatientVisualController : MonoBehaviour
    {
        private static readonly Color SkinNormal = new Color(0.87f, 0.72f, 0.62f);
        private static readonly Color SkinDistressed = new Color(0.84f, 0.76f, 0.70f); // paler
        private static readonly Color SkinUnconscious = new Color(0.80f, 0.76f, 0.72f);
        private static readonly Color SkinUnresponsive = new Color(0.72f, 0.74f, 0.78f); // grey

        /// <summary>Presentation-only clamp: canonical RR is NEVER mutated; the
        /// visual breathing rate is limited so extreme canonical values cannot
        /// produce absurd animation speeds.</summary>
        private const float MaxVisualBreathsPerMinute = 60f;

        private Transform _chest;
        private Vector3 _chestBaseScale;
        private Vector3 _chestBasePosition;
        private bool _rigged;
        private readonly List<Material> _skinMaterials = new List<Material>();

        private PatientVisualState _state = PatientVisualState.Normal;
        private float _breathsPerMinute = 14f;
        private float _phase;

        public PatientVisualState CurrentState => _state;

        private void Awake()
        {
            _rigged = GetComponentInChildren<SkinnedMeshRenderer>() != null;

            _chest = FindDeep(transform, "Chest");
            if (_chest != null)
            {
                _chestBaseScale = _chest.localScale;
                _chestBasePosition = _chest.localPosition;
            }

            // Legacy primitive contract: named skin part children share one instance.
            Material legacyInstance = null;
            foreach (var name in new[] { "Head", "HandLeft", "HandRight" })
            {
                var part = transform.Find(name);
                var renderer = part != null ? part.GetComponent<Renderer>() : null;
                if (renderer == null)
                {
                    continue;
                }
                if (legacyInstance == null)
                {
                    legacyInstance = renderer.material; // instantiates
                    _skinMaterials.Add(legacyInstance);
                }
                else
                {
                    renderer.sharedMaterial = legacyInstance;
                }
            }

            // Rigged contract: instance every material whose name says skin.
            if (_skinMaterials.Count == 0)
            {
                foreach (var renderer in GetComponentsInChildren<Renderer>())
                {
                    var shared = renderer.sharedMaterials;
                    for (int i = 0; i < shared.Length; i++)
                    {
                        if (shared[i] != null && shared[i].name.Contains("Skin"))
                        {
                            var instances = renderer.materials; // instantiates the whole slot array once
                            _skinMaterials.Add(instances[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called by <see cref="EnvironmentBootstrap"/> on every snapshot. The rate
        /// is the canonical RR; the state decides how the breathing looks.
        /// </summary>
        public void Apply(PatientVisualState state, double canonicalRrPerMin)
        {
            _state = state;
            _breathsPerMinute = Mathf.Clamp((float)canonicalRrPerMin, 0f, MaxVisualBreathsPerMinute);

            var tint = state switch
            {
                PatientVisualState.Distressed => SkinDistressed,
                PatientVisualState.Unconscious => SkinUnconscious,
                PatientVisualState.Unresponsive => SkinUnresponsive,
                _ => SkinNormal,
            };
            foreach (var material in _skinMaterials)
            {
                // URP Lit: set _BaseColor explicitly (Material.color mapping is
                // not reliable for headless-created materials).
                material.SetColor("_BaseColor", tint);
            }
        }

        /// <summary>Fresh simulation on the warm runtime: restart from a neutral pose.</summary>
        public void ResetPresentation()
        {
            _phase = 0f;
            if (_chest != null)
            {
                _chest.localScale = _chestBaseScale;
                _chest.localPosition = _chestBasePosition;
            }
        }

        private void Update()
        {
            if (_chest == null)
            {
                return;
            }

            float amplitude = _state switch
            {
                PatientVisualState.Normal => 1.0f,
                PatientVisualState.Distressed => 2.0f, // visibly laboured
                PatientVisualState.Unconscious => 0.6f,
                PatientVisualState.Unresponsive => 0f, // no respiratory motion
                _ => 1.0f,
            };

            if (amplitude <= 0f || _breathsPerMinute <= 0f)
            {
                _chest.localScale = _chestBaseScale;
                _chest.localPosition = _chestBasePosition;
                return;
            }

            _phase += Time.deltaTime * (_breathsPerMinute / 60f) * Mathf.PI * 2f;
            float breath = Mathf.Sin(_phase);

            if (_rigged)
            {
                // Chest bone rises along the patient's up axis (shoulders/ribcage).
                float rise = breath * 0.009f * amplitude;
                var localUp = _chest.parent != null
                    ? _chest.parent.InverseTransformDirection(transform.up)
                    : Vector3.up;
                _chest.localPosition = _chestBasePosition + localUp * rise;
            }
            else
            {
                float scale = 1f + breath * 0.020f * amplitude;
                _chest.localScale = new Vector3(
                    _chestBaseScale.x * scale,
                    _chestBaseScale.y * scale,
                    _chestBaseScale.z);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }
    }
}
