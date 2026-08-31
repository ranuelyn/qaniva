namespace Qaniva.Clinical.Core.Model;

/// <summary>
/// Canonical UI-facing projection of one action's current availability, computed
/// by the engine so no client ever re-derives visibility or precondition logic.
///
/// The three states an interaction layer must distinguish:
///   Visible == false                  -> HIDDEN   (do not show at all)
///   Visible &amp;&amp; !Enabled          -> DISABLED (show greyed, with the reason)
///   Visible &amp;&amp; Enabled           -> ENABLED  (may be submitted)
///
/// Invariant: an action is offerable to <c>ApplyAction</c> exactly when
/// <c>Visible &amp;&amp; Enabled</c> — the same computation backs both paths.
/// </summary>
public sealed class ActionAvailability
{
    public ActionAvailability(
        string actionId,
        string label,
        string type,
        bool visible,
        bool enabled,
        string? disabledReason)
    {
        ActionId = actionId;
        Label = label;
        Type = type;
        Visible = visible;
        Enabled = enabled;
        DisabledReason = disabledReason;
    }

    public string ActionId { get; }

    /// <summary>Display label from the case definition.</summary>
    public string Label { get; }

    /// <summary>Case action type (examine | order | medication | procedure | consult | disposition | communication).</summary>
    public string Type { get; }

    public bool Visible { get; }

    public bool Enabled { get; }

    /// <summary>
    /// Engine-worded reason when Visible &amp;&amp; !Enabled (e.g. an unmet
    /// precondition expression or "already performed"); null otherwise.
    /// </summary>
    public string? DisabledReason { get; }
}
