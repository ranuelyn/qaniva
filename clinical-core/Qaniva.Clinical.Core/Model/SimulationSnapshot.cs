using System.Collections.Generic;

namespace Qaniva.Clinical.Core.Model;

/// <summary>
/// Immutable capture of the simulation at a point in time. Safe to hand to the
/// presentation layer (Unity) — it carries no engine internals and no behaviour.
/// </summary>
public sealed class SimulationSnapshot
{
    public required string CaseId { get; init; }
    public required int CaseVersion { get; init; }
    public required int SimTimeSec { get; init; }
    public required VitalsSnapshot Vitals { get; init; }
    public required string Rhythm { get; init; }
    public required string Airway { get; init; }
    public required string Breathing { get; init; }
    public required string Circulation { get; init; }
    public required string Neuro { get; init; }
    public required int PainScore { get; init; }
    public required IReadOnlyList<string> Flags { get; init; }
    public required IReadOnlyList<string> DisclosedFacts { get; init; }
    public required bool IsTerminal { get; init; }
    public string? TerminalStateId { get; init; }
    public string? TerminalOutcome { get; init; }

    /// <summary>Canonical hash of the underlying patient state (see <see cref="Engine.Hashing"/>).</summary>
    public required string StateHash { get; init; }
}

public sealed class VitalsSnapshot
{
    public required double Hr { get; init; }
    public required double SbpMmHg { get; init; }
    public required double DbpMmHg { get; init; }
    public required double Spo2 { get; init; }
    public required double RrPerMin { get; init; }
    public required double TempC { get; init; }
}
