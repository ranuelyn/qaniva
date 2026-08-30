using UnityEngine;
using Qaniva.Bridge;
using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Maps engine state / presentation cues to a small set of patient animation
    /// states (blueprint §3: "a small number of state-driven animations"). The
    /// mapping table is data here, not medicine — the engine decides WHEN, this
    /// decides only HOW it looks.
    /// </summary>
    public sealed class PatientAnimationBinding : MonoBehaviour, IPresentationAdapter
    {
        [SerializeField] private SimulationBridgeController controller;
        [SerializeField] private Animator animator;

        private static readonly int DistressLevel = Animator.StringToHash("DistressLevel");
        private static readonly int Unconscious = Animator.StringToHash("Unconscious");

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.SnapshotUpdated += Apply;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.SnapshotUpdated -= Apply;
            }
        }

        public void Apply(SimulationSnapshotView snapshot)
        {
            if (snapshot == null || animator == null)
            {
                return;
            }

            int distress = snapshot.Circulation switch
            {
                "arrest" => 3,
                "shock" => 2,
                "poor_perfusion" => 1,
                _ => 0,
            };
            animator.SetInteger(DistressLevel, distress);
            animator.SetBool(Unconscious, snapshot.Neuro is "unresponsive" or "pain");
        }

        public void OnPresentationCue(string cue)
        {
            if (animator == null)
            {
                return;
            }
            switch (cue)
            {
                case "arrest":
                    animator.SetInteger(DistressLevel, 3);
                    animator.SetBool(Unconscious, true);
                    break;
                case "recovery":
                    animator.SetInteger(DistressLevel, 0);
                    animator.SetBool(Unconscious, false);
                    break;
            }
        }
    }
}
