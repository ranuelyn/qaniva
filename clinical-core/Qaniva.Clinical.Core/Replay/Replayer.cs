using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qaniva.Clinical.Core.Engine;
using Qaniva.Clinical.Core.Model;

namespace Qaniva.Clinical.Core.Replay;

/// <summary>
/// Runs an <see cref="AttemptScript"/> against a case and returns the full,
/// deterministic result. Running the same script twice must produce byte-identical
/// output — this is the core replay invariant enforced by the golden tests.
/// </summary>
public static class Replayer
{
    public static ReplayResult Run(CaseDefinition caseDefinition, AttemptScript script)
    {
        if (!string.IsNullOrEmpty(script.CaseId) && script.CaseId != caseDefinition.Id)
        {
            throw new InvalidOperationException(
                $"Script targets case \"{script.CaseId}\" but \"{caseDefinition.Id}\" was supplied.");
        }
        if (script.CaseVersion != 0 && script.CaseVersion != caseDefinition.Version)
        {
            throw new InvalidOperationException(
                $"Script targets case version {script.CaseVersion} but v{caseDefinition.Version} was supplied.");
        }

        var sim = new Simulation(caseDefinition, script.Seed).Initialize();
        var rejections = new List<string>();
        var appliedActionIds = new List<string>();

        foreach (var step in script.Steps)
        {
            if (sim.IsTerminated)
            {
                rejections.Add("step after terminal state was skipped");
                break;
            }

            ActionResult result;
            if (!string.IsNullOrEmpty(step.Action))
            {
                result = sim.ApplyAction(step.Action!, step.Params);
                if (result.Accepted)
                {
                    appliedActionIds.Add(step.Action!);
                }
            }
            else if (step.WaitSec is { } waitSec)
            {
                result = sim.AdvanceTime(waitSec);
            }
            else
            {
                throw new InvalidOperationException("An AttemptStep must have either Action or WaitSec.");
            }

            if (!result.Accepted)
            {
                rejections.Add($"{step.Action ?? "wait"}: {result.RejectionReason}");
            }
        }

        var snapshot = sim.Snapshot();
        var score = sim.Score();
        var debrief = sim.BuildDebriefFacts();
        string replayHash = ComputeReplayHash(caseDefinition, script.Seed, appliedActionIds, snapshot.StateHash);

        return new ReplayResult(sim.Timeline, snapshot, score, debrief, replayHash, rejections);
    }

    public static string ComputeReplayHash(
        CaseDefinition caseDefinition,
        ulong seed,
        IEnumerable<string> appliedActionIds,
        string finalStateHash)
    {
        var sb = new StringBuilder();
        sb.Append(caseDefinition.Id).Append('|');
        sb.Append(caseDefinition.Version).Append('|');
        sb.Append(seed).Append('|');
        sb.Append(string.Join(",", appliedActionIds)).Append('|');
        sb.Append(finalStateHash);
        return Hashing.Sha256Hex(sb.ToString());
    }
}

public sealed class ReplayResult
{
    public ReplayResult(
        AttemptTimeline timeline,
        SimulationSnapshot finalSnapshot,
        AttemptScore score,
        DebriefFacts debriefFacts,
        string replayHash,
        IReadOnlyList<string> rejections)
    {
        Timeline = timeline;
        FinalSnapshot = finalSnapshot;
        Score = score;
        DebriefFacts = debriefFacts;
        ReplayHash = replayHash;
        Rejections = rejections;
    }

    public AttemptTimeline Timeline { get; }
    public SimulationSnapshot FinalSnapshot { get; }
    public AttemptScore Score { get; }
    public DebriefFacts DebriefFacts { get; }
    public string ReplayHash { get; }

    /// <summary>Steps the engine refused (invalid action, unmet precondition, ...).</summary>
    public IReadOnlyList<string> Rejections { get; }
}
