using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Presentation-only visual states for the patient. These are NOT clinical
    /// categories — they are generic "how the patient looks" buckets derived from
    /// canonical engine state, used to pick breathing amplitude/pose/tint.
    /// </summary>
    public enum PatientVisualState
    {
        Normal,
        Distressed,
        Unconscious,
        Unresponsive,
    }

    /// <summary>
    /// Deterministic, pure mapping from the canonical <see cref="SimulationSnapshotView"/>
    /// to a <see cref="PatientVisualState"/>. This is the ONLY place presentation
    /// interprets clinical state; visual controllers below it never look at the
    /// snapshot's clinical fields themselves (they receive the mapped state plus
    /// canonical display values like the respiratory rate).
    ///
    /// Same snapshot in => same visual state out, always. No randomness, no time,
    /// no mutation.
    /// </summary>
    public static class PatientPresentationMapper
    {
        public static PatientVisualState Map(SimulationSnapshotView snapshot)
        {
            if (snapshot == null)
            {
                return PatientVisualState.Normal;
            }

            // Ordered by presentation severity; every branch reads canonical
            // engine enums (circulation/neuro) — never raw numbers, never rules.
            if (snapshot.Circulation == "arrest")
            {
                return PatientVisualState.Unresponsive;
            }
            if (snapshot.Neuro == "unresponsive")
            {
                return PatientVisualState.Unconscious;
            }
            if (snapshot.Circulation == "shock"
                || snapshot.Circulation == "poor_perfusion"
                || snapshot.Neuro == "pain"
                || snapshot.Neuro == "voice")
            {
                return PatientVisualState.Distressed;
            }
            return PatientVisualState.Normal;
        }
    }
}
