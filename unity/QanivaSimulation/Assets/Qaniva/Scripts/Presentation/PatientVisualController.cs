using UnityEngine;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Applies a mapped <see cref="PatientVisualState"/> to the patient prefab:
    /// procedural chest breathing (rate from the CANONICAL respiratory rate,
    /// character/amplitude from the visual state) and a generic skin-tone shift so
    /// state changes read in stills as well as motion.
    ///
    /// Presentation only: this component receives the mapped state + display
    /// values; it never reads clinical fields, never decides state, and the tint
    /// is a generic "looks worse" cue, not a diagnostic claim.
    ///
    /// Prefab contract (children by name): "Chest" (breathing transform),
    /// "Head" and "Hands" renderers share the skin material instance.
    /// Anchors for future procedures: "AnchorHead", "AnchorChest",
    /// "AnchorLeftArm", "AnchorRightArm".
    /// </summary>
    public sealed class PatientVisualController : MonoBehaviour
    {
        private static readonly Color SkinNormal = new Color(0.87f, 0.72f, 0.62f);
        private static readonly Color SkinDistressed = new Color(0.84f, 0.76f, 0.70f); // paler
        private static readonly Color SkinUnconscious = new Color(0.80f, 0.76f, 0.72f);
        private static readonly Color SkinUnresponsive = new Color(0.72f, 0.74f, 0.78f); // grey

        private Transform _chest;
        private Vector3 _chestBaseScale;
        private Material _skinMaterial; // instanced once, shared by head/hands

        private PatientVisualState _state = PatientVisualState.Normal;
        private float _breathsPerMinute = 14f;
        private float _phase;

        public PatientVisualState CurrentState => _state;

        private void Awake()
        {
            _chest = transform.Find("Chest");
            if (_chest != null)
            {
                _chestBaseScale = _chest.localScale;
            }

            // Instance the skin material once and share it across skin renderers.
            foreach (var name in new[] { "Head", "HandLeft", "HandRight" })
            {
                var part = transform.Find(name);
                if (part == null)
                {
                    continue;
                }
                var renderer = part.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }
                if (_skinMaterial == null)
                {
                    _skinMaterial = renderer.material; // instantiates
                }
                else
                {
                    renderer.sharedMaterial = _skinMaterial;
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
            _breathsPerMinute = Mathf.Clamp((float)canonicalRrPerMin, 0f, 60f);

            if (_skinMaterial != null)
            {
                // URP Lit: set _BaseColor explicitly (Material.color mapping is
                // not reliable for headless-created materials).
                _skinMaterial.SetColor("_BaseColor", state switch
                {
                    PatientVisualState.Distressed => SkinDistressed,
                    PatientVisualState.Unconscious => SkinUnconscious,
                    PatientVisualState.Unresponsive => SkinUnresponsive,
                    _ => SkinNormal,
                });
            }
        }

        /// <summary>Fresh simulation on the warm runtime: restart from a neutral pose.</summary>
        public void ResetPresentation()
        {
            _phase = 0f;
            if (_chest != null)
            {
                _chest.localScale = _chestBaseScale;
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
                PatientVisualState.Normal => 0.020f,
                PatientVisualState.Distressed => 0.040f, // visibly laboured
                PatientVisualState.Unconscious => 0.012f,
                PatientVisualState.Unresponsive => 0f,   // no respiratory motion
                _ => 0.02f,
            };

            if (amplitude <= 0f || _breathsPerMinute <= 0f)
            {
                _chest.localScale = _chestBaseScale;
                return;
            }

            _phase += Time.deltaTime * (_breathsPerMinute / 60f) * Mathf.PI * 2f;
            float breath = 1f + Mathf.Sin(_phase) * amplitude;
            _chest.localScale = new Vector3(
                _chestBaseScale.x * breath,
                _chestBaseScale.y * breath,
                _chestBaseScale.z);
        }
    }
}
