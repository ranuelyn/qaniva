using System.Collections.Generic;

namespace Qaniva.Clinical.Core.Model;

/// <summary>Outcome of a single <c>Simulation.ApplyAction</c> / <c>AdvanceTime</c> call.</summary>
public sealed class ActionResult
{
    private ActionResult(
        bool accepted,
        string? rejectionReason,
        AttemptEvent? evt,
        bool terminated,
        IReadOnlyList<string>? presentationCues)
    {
        Accepted = accepted;
        RejectionReason = rejectionReason;
        Event = evt;
        Terminated = terminated;
        PresentationCues = presentationCues ?? new List<string>();
    }

    public bool Accepted { get; }

    /// <summary>Set when <see cref="Accepted"/> is false. State is unchanged in that case.</summary>
    public string? RejectionReason { get; }

    /// <summary>The timeline entry that was appended (null when rejected).</summary>
    public AttemptEvent? Event { get; }

    /// <summary>True if this step drove the simulation into a terminal state.</summary>
    public bool Terminated { get; }

    /// <summary>Presentation cues raised by rules that fired during this step.</summary>
    public IReadOnlyList<string> PresentationCues { get; }

    public static ActionResult Rejected(string reason) => new(false, reason, null, false, null);

    public static ActionResult Ok(AttemptEvent evt, bool terminated, IReadOnlyList<string>? presentationCues = null) =>
        new(true, null, evt, terminated, presentationCues);
}
