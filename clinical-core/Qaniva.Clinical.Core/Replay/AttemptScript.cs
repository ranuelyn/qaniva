using System.Collections.Generic;

namespace Qaniva.Clinical.Core.Replay;

/// <summary>
/// A recorded, replayable attempt: the exact inputs needed to reproduce a run.
/// This is what CI golden tests and "replay this attempt" both consume.
/// </summary>
public sealed class AttemptScript
{
    public string CaseId { get; set; } = "";
    public int CaseVersion { get; set; }
    public ulong Seed { get; set; }
    public List<AttemptStep> Steps { get; set; } = new();
}

public sealed class AttemptStep
{
    /// <summary>Action id to apply. Null/empty means this is a wait step.</summary>
    public string? Action { get; set; }

    /// <summary>Optional action parameters (dose, route, ...).</summary>
    public Dictionary<string, string>? Params { get; set; }

    /// <summary>When set (and Action is empty), advance the clock by this many seconds.</summary>
    public int? WaitSec { get; set; }
}
