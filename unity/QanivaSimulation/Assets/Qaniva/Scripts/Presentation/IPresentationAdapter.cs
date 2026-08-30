using Qaniva.Simulation.Core;

namespace Qaniva.Presentation
{
    /// <summary>
    /// A presentation adapter turns engine snapshots into what the player sees and
    /// hears. INVARIANT: adapters are READ-ONLY consumers of engine state. They must
    /// never decide a vital, a transition, or a score — they only render.
    /// </summary>
    public interface IPresentationAdapter
    {
        void Apply(SimulationSnapshotView snapshot);

        void OnPresentationCue(string cue);
    }
}
