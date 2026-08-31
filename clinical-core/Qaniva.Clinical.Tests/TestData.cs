using System;
using System.IO;
using System.Text.Json;
using Qaniva.Clinical.Core.Model;
using Qaniva.Clinical.Core.Replay;
using Qaniva.Clinical.Core.Serialization;

namespace Qaniva.Clinical.Tests;

internal static class TestData
{
    private static readonly string BaseDir = AppContext.BaseDirectory;

    public static string CasePath => Path.Combine(BaseDir, "TestData", "case.json");

    public static string StemiCasePath => Path.Combine(BaseDir, "TestData", "stemi_case.json");

    public static string GoldenDir => Path.Combine(BaseDir, "Golden");

    public static CaseDefinition DemoCase() => CaseLoader.FromFile(CasePath);

    public static CaseDefinition StemiCase() => CaseLoader.FromFile(StemiCasePath);

    public static AttemptScript LoadScript(string fileName)
    {
        var json = File.ReadAllText(Path.Combine(GoldenDir, fileName));
        return JsonSerializer.Deserialize<AttemptScript>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"Failed to load script {fileName}");
    }

    /// <summary>
    /// Reads a committed golden file, or (when UPDATE_GOLDEN=1) regenerates it from
    /// the current engine output so it can be reviewed and committed.
    /// </summary>
    public static string ReadOrUpdateGolden(string fileName, ReplayResult freshResult)
    {
        var path = Path.Combine(GoldenDir, fileName);
        var fresh = GoldenSerializer.ToGoldenJson(freshResult) + "\n";

        if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1")
        {
            // Also update the source-tree copy, not just the build output.
            File.WriteAllText(path, fresh);
            var sourcePath = Path.GetFullPath(
                Path.Combine(BaseDir, "..", "..", "..", "Golden", fileName));
            File.WriteAllText(sourcePath, fresh);
            return fresh;
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Golden file {fileName} missing. Run tests once with UPDATE_GOLDEN=1 to create it.", path);
        }

        return File.ReadAllText(path);
    }
}
