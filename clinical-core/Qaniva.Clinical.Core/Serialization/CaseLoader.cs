using System;
using System.IO;
using System.Text.Json;
using Qaniva.Clinical.Core.Model;

namespace Qaniva.Clinical.Core.Serialization;

/// <summary>
/// Loads a schema-validated <c>case.json</c> into a <see cref="CaseDefinition"/>.
/// Authoritative structural validation is done by <c>@qaniva/case-schema</c> (JSON Schema);
/// this loader adds only a light sanity check so a misfed file fails loudly.
/// </summary>
public static class CaseLoader
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false,
    };

    public static CaseDefinition FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Case JSON is empty.", nameof(json));
        }

        CaseDefinition definition;
        try
        {
            definition = JsonSerializer.Deserialize<CaseDefinition>(json, Options)
                ?? throw new CaseLoadException("Case JSON deserialised to null.");
        }
        catch (JsonException ex)
        {
            throw new CaseLoadException($"Case JSON is not parseable: {ex.Message}", ex);
        }

        Validate(definition);
        return definition;
    }

    public static CaseDefinition FromFile(string path) => FromJson(File.ReadAllText(path));

    private static void Validate(CaseDefinition c)
    {
        if (c.SchemaVersion != 1)
        {
            throw new CaseLoadException($"Unsupported schemaVersion {c.SchemaVersion} (engine supports 1).");
        }
        if (string.IsNullOrEmpty(c.Id))
        {
            throw new CaseLoadException("Case is missing an id.");
        }
        if (c.Version < 1)
        {
            throw new CaseLoadException("Case version must be >= 1.");
        }
        if (c.AvailableActions.Count == 0)
        {
            throw new CaseLoadException("Case has no availableActions.");
        }
        if (c.ScoringCriteria.Count == 0)
        {
            throw new CaseLoadException("Case has no scoringCriteria.");
        }
        if (c.TerminalStates.Count == 0)
        {
            throw new CaseLoadException("Case has no terminalStates.");
        }
        if (!c.Metadata.Fictional)
        {
            throw new CaseLoadException("MVP invariant violated: metadata.fictional must be true.");
        }
    }
}

public sealed class CaseLoadException : Exception
{
    public CaseLoadException(string message)
        : base(message)
    {
    }

    public CaseLoadException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
