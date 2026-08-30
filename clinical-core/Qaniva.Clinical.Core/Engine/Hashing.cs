using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Qaniva.Clinical.Core.Model;

namespace Qaniva.Clinical.Core.Engine;

/// <summary>
/// Deterministic, canonical hashing of patient state. Two states with the same
/// meaningful content always produce the same hash regardless of insertion order
/// or floating-point formatting noise.
/// </summary>
public static class Hashing
{
    public static string StateHash(PatientState state) => Sha256Hex(CanonicalStateJson(state));

    public static string CanonicalStateJson(PatientState state)
    {
        var sb = new StringBuilder(256);
        sb.Append('{');
        sb.Append("\"simTimeSec\":").Append(state.SimTimeSec).Append(',');
        sb.Append("\"vitals\":{");
        sb.Append("\"hr\":").Append(Num(state.Vitals.Hr)).Append(',');
        sb.Append("\"sbpMmHg\":").Append(Num(state.Vitals.SbpMmHg)).Append(',');
        sb.Append("\"dbpMmHg\":").Append(Num(state.Vitals.DbpMmHg)).Append(',');
        sb.Append("\"spo2\":").Append(Num(state.Vitals.Spo2)).Append(',');
        sb.Append("\"rrPerMin\":").Append(Num(state.Vitals.RrPerMin)).Append(',');
        sb.Append("\"tempC\":").Append(Num(state.Vitals.TempC));
        sb.Append("},");
        sb.Append("\"rhythm\":").Append(Str(state.Rhythm)).Append(',');
        sb.Append("\"airway\":").Append(Str(state.Airway)).Append(',');
        sb.Append("\"breathing\":").Append(Str(state.Breathing)).Append(',');
        sb.Append("\"circulation\":").Append(Str(state.Circulation)).Append(',');
        sb.Append("\"neuro\":").Append(Str(state.Neuro)).Append(',');
        sb.Append("\"painScore\":").Append(state.PainScore).Append(',');
        AppendStringArray(sb, "flags", state.Flags);
        sb.Append(',');
        AppendStringArray(sb, "disclosedFacts", state.DisclosedFacts);
        sb.Append(',');
        AppendStringArray(sb, "firedRuleIds", state.FiredRuleIds);
        sb.Append(',');
        sb.Append("\"actionCounts\":{");
        bool first = true;
        foreach (var kv in state.ActionCounts)
        {
            if (!first)
            {
                sb.Append(',');
            }
            first = false;
            sb.Append(Str(kv.Key)).Append(':').Append(kv.Value);
        }
        sb.Append('}');
        sb.Append('}');
        return sb.ToString();
    }

    public static string Sha256Hex(string input)
    {
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
        {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    private static void AppendStringArray(StringBuilder sb, string key, System.Collections.Generic.IEnumerable<string> values)
    {
        sb.Append('"').Append(key).Append("\":[");
        bool first = true;
        foreach (var v in values)
        {
            if (!first)
            {
                sb.Append(',');
            }
            first = false;
            sb.Append(Str(v));
        }
        sb.Append(']');
    }

    private static string Num(double value) =>
        value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string Str(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
