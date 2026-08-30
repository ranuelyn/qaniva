using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Qaniva.Clinical.Core.Model;

/// <summary>Classification of a timeline entry — produced by the deterministic engine only.</summary>
public enum EntryClassification
{
    Neutral,
    Correct,
    Delayed,
    Missed,
    Harmful,
}

/// <summary>One recorded step in an attempt: an applied action plus the rules it triggered.</summary>
public sealed class AttemptEvent
{
    public int Seq { get; init; }
    public int SimTimeSec { get; init; }
    public string ActionId { get; init; } = "";
    public IReadOnlyDictionary<string, string> Params { get; init; } =
        new Dictionary<string, string>();
    public string BeforeHash { get; init; } = "";
    public string AfterHash { get; init; } = "";
    public IReadOnlyList<string> TriggeredRuleIds { get; init; } = new List<string>();
    public double ScoreDelta { get; init; }
    public EntryClassification Classification { get; init; } = EntryClassification.Neutral;
    public string Label { get; init; } = "";
}

/// <summary>Ordered, append-only list of <see cref="AttemptEvent"/> for one attempt.</summary>
public sealed class AttemptTimeline
{
    private readonly List<AttemptEvent> _events = new();

    public IReadOnlyList<AttemptEvent> Events => new ReadOnlyCollection<AttemptEvent>(_events);
    public int Count => _events.Count;

    internal void Append(AttemptEvent evt) => _events.Add(evt);
}
