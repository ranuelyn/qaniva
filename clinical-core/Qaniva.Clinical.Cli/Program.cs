using System;
using System.IO;
using System.Text.Json;
using Qaniva.Clinical.Core.Replay;
using Qaniva.Clinical.Core.Serialization;

if (args.Length == 0)
{
    Console.Error.WriteLine(
        """
        qaniva-clinical — headless deterministic engine tool

        Usage:
          qaniva-clinical validate <case.json>
          qaniva-clinical replay   <case.json> <script.json>
          qaniva-clinical golden   <case.json> <script.json> [--write <out.json>]
        """);
    return 2;
}

try
{
    switch (args[0])
    {
        case "validate":
            RequireArgs(args, 2);
            _ = CaseLoader.FromFile(args[1]);
            Console.Out.WriteLine($"OK  {args[1]} loaded and passed engine sanity checks.");
            return 0;

        case "replay":
            {
                RequireArgs(args, 3);
                var result = RunScript(args[1], args[2]);
                Console.Out.WriteLine(GoldenSerializer.ToGoldenJson(result));
                return 0;
            }

        case "golden":
            {
                RequireArgs(args, 3);
                var result = RunScript(args[1], args[2]);
                string json = GoldenSerializer.ToGoldenJson(result);
                int writeIdx = Array.IndexOf(args, "--write");
                if (writeIdx >= 0 && writeIdx + 1 < args.Length)
                {
                    File.WriteAllText(args[writeIdx + 1], json + "\n");
                    Console.Error.WriteLine($"wrote golden -> {args[writeIdx + 1]}");
                }
                else
                {
                    Console.Out.WriteLine(json);
                }
                return 0;
            }

        default:
            Console.Error.WriteLine($"Unknown command \"{args[0]}\".");
            return 2;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    return 1;
}

static void RequireArgs(string[] args, int n)
{
    if (args.Length < n)
    {
        throw new ArgumentException($"Command \"{args[0]}\" needs {n - 1} argument(s).");
    }
}

static ReplayResult RunScript(string casePath, string scriptPath)
{
    var caseDefinition = CaseLoader.FromFile(casePath);
    var script = JsonSerializer.Deserialize<AttemptScript>(
        File.ReadAllText(scriptPath),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidOperationException("Script JSON deserialised to null.");
    return Replayer.Run(caseDefinition, script);
}
