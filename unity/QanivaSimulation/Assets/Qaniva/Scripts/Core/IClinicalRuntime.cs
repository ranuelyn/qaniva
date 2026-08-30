using System.Collections.Generic;

namespace Qaniva.Simulation.Core
{
    /// <summary>
    /// Unity's view of the deterministic clinical engine. The real implementation
    /// (<c>Qaniva.Clinical.Runtime.ClinicalRuntime</c>) wraps the compiled
    /// <c>Qaniva.Clinical.Core.dll</c>; <c>Qaniva.Bridge.StubClinicalRuntime</c>
    /// is a canned stand-in so the scene compiles and runs before the DLL is synced.
    ///
    /// INVARIANT: Unity code calls INTO this interface. It never computes vitals,
    /// state transitions, drug results, or scores itself. Those belong to the engine.
    /// </summary>
    public interface IClinicalRuntime
    {
        void LoadCase(string caseJson, ulong seed);

        SimulationSnapshotView Initialize();

        IReadOnlyList<string> GetAvailableActionIds();

        ActionOutcomeView ApplyAction(string actionId, IReadOnlyDictionary<string, string> parameters);

        SimulationSnapshotView AdvanceTime(int seconds);

        AttemptSummaryView BuildAttemptSummary();

        bool IsTerminated { get; }
    }
}
