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
        IReadOnlyList<string>? presentationCues,
        string? resultText = null,
        string? resultAssetId = null,
        string? resultAssetLabel = null,
        string? resultAssetClinicalStatus = null,
        IReadOnlyList<DisclosedFact>? newlyDisclosedFacts = null)
    {
        Accepted = accepted;
        RejectionReason = rejectionReason;
        Event = evt;
        Terminated = terminated;
        PresentationCues = presentationCues ?? new List<string>();
        ResultText = resultText;
        ResultAssetId = resultAssetId;
        ResultAssetLabel = resultAssetLabel;
        ResultAssetClinicalStatus = resultAssetClinicalStatus;
        NewlyDisclosedFacts = newlyDisclosedFacts ?? new List<DisclosedFact>();
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

    /// <summary>Resolved result-template text for the action (null when the action has none).</summary>
    public string? ResultText { get; }

    /// <summary>ResultAsset id the learner may open for this result (e.g. an ECG image), or null.</summary>
    public string? ResultAssetId { get; }

    /// <summary>Learner-facing label of that asset (from the case's resultAssets), or null.</summary>
    public string? ResultAssetLabel { get; }

    /// <summary>The asset's provenance.clinicalStatus (e.g. placeholder_replacement_required), or null.</summary>
    public string? ResultAssetClinicalStatus { get; }

    /// <summary>Facts whose disclosure was caused by this step (action or triggered rules), in disclosure order.</summary>
    public IReadOnlyList<DisclosedFact> NewlyDisclosedFacts { get; }

    public static ActionResult Rejected(string reason) => new(false, reason, null, false, null);

    public static ActionResult Ok(
        AttemptEvent evt,
        bool terminated,
        IReadOnlyList<string>? presentationCues = null,
        string? resultText = null,
        string? resultAssetId = null,
        string? resultAssetLabel = null,
        string? resultAssetClinicalStatus = null,
        IReadOnlyList<DisclosedFact>? newlyDisclosedFacts = null) =>
        new(true, null, evt, terminated, presentationCues, resultText, resultAssetId, resultAssetLabel, resultAssetClinicalStatus, newlyDisclosedFacts);
}

/// <summary>A hidden fact that became disclosed, with its learner-facing text.</summary>
public sealed class DisclosedFact
{
    public DisclosedFact(string id, string text)
    {
        Id = id;
        Text = text;
    }

    public string Id { get; }
    public string Text { get; }
}
