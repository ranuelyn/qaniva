using System;
using System.Collections.Generic;
using System.Linq;

namespace Qaniva.Clinical.Core.Model;

/// <summary>
/// Live patient state during a simulation. Only <see cref="Engine.Simulation"/> mutates it.
/// All collections are sorted so that serialization and hashing are deterministic.
/// </summary>
public sealed class PatientState
{
    public Vitals Vitals { get; set; } = new();
    public string Rhythm { get; set; } = "";
    public string Airway { get; set; } = "patent";
    public string Breathing { get; set; } = "spontaneous";
    public string Circulation { get; set; } = "normal";
    public string Neuro { get; set; } = "alert";
    public int PainScore { get; set; }
    public int SimTimeSec { get; set; }

    public SortedSet<string> Flags { get; private set; } = new(StringComparer.Ordinal);
    public SortedSet<string> DisclosedFacts { get; private set; } = new(StringComparer.Ordinal);
    public SortedSet<string> FiredRuleIds { get; private set; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, int> ActionCounts { get; private set; } = new(StringComparer.Ordinal);

    public static PatientState FromInitial(InitialStateDto dto)
    {
        var state = new PatientState
        {
            Vitals = new Vitals
            {
                Hr = dto.Vitals.Hr,
                SbpMmHg = dto.Vitals.SbpMmHg,
                DbpMmHg = dto.Vitals.DbpMmHg,
                Spo2 = dto.Vitals.Spo2,
                RrPerMin = dto.Vitals.RrPerMin,
                TempC = dto.Vitals.TempC,
            },
            Rhythm = dto.Rhythm,
            Airway = dto.Airway,
            Breathing = dto.Breathing,
            Circulation = dto.Circulation,
            Neuro = dto.Neuro,
            PainScore = dto.PainScore,
            SimTimeSec = 0,
        };
        foreach (var flag in dto.Flags)
        {
            state.Flags.Add(flag);
        }
        return state;
    }

    public PatientState DeepClone()
    {
        var clone = new PatientState
        {
            Vitals = Vitals.Clone(),
            Rhythm = Rhythm,
            Airway = Airway,
            Breathing = Breathing,
            Circulation = Circulation,
            Neuro = Neuro,
            PainScore = PainScore,
            SimTimeSec = SimTimeSec,
            Flags = new SortedSet<string>(Flags, StringComparer.Ordinal),
            DisclosedFacts = new SortedSet<string>(DisclosedFacts, StringComparer.Ordinal),
            FiredRuleIds = new SortedSet<string>(FiredRuleIds, StringComparer.Ordinal),
            ActionCounts = new SortedDictionary<string, int>(ActionCounts, StringComparer.Ordinal),
        };
        return clone;
    }

    public bool HasFlag(string flag) => Flags.Contains(flag);

    public int ActionCount(string actionId) =>
        ActionCounts.TryGetValue(actionId, out var count) ? count : 0;

    internal void IncrementActionCount(string actionId) =>
        ActionCounts[actionId] = ActionCount(actionId) + 1;
}

public sealed class Vitals
{
    public double Hr { get; set; }
    public double SbpMmHg { get; set; }
    public double DbpMmHg { get; set; }
    public double Spo2 { get; set; }
    public double RrPerMin { get; set; }
    public double TempC { get; set; }

    public Vitals Clone() => (Vitals)MemberwiseClone();

    public double Get(string name) => name switch
    {
        "hr" => Hr,
        "sbpMmHg" => SbpMmHg,
        "dbpMmHg" => DbpMmHg,
        "spo2" => Spo2,
        "rrPerMin" => RrPerMin,
        "tempC" => TempC,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown vital"),
    };

    public void Set(string name, double value)
    {
        switch (name)
        {
            case "hr": Hr = value; break;
            case "sbpMmHg": SbpMmHg = value; break;
            case "dbpMmHg": DbpMmHg = value; break;
            case "spo2": Spo2 = value; break;
            case "rrPerMin": RrPerMin = value; break;
            case "tempC": TempC = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown vital");
        }
    }
}
