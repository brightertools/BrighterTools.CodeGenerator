using BrighterTools.CodeGenerator.Metadata;
using System.Text.Json;

namespace BrighterTools.CodeGenerator.Runtime;

internal static class GenerationRunHistoryWriter
{
    private const string HistoryFileName = "generation-history.jsonl";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string GetHistoryFilePath(GeneratorOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConfigDirectory))
        {
            return string.Empty;
        }

        return Path.Combine(options.ConfigDirectory, HistoryFileName);
    }

    public static void AppendIfEnabled(GeneratorOptions options, GeneratedFileWriteResult result)
    {
        if (options.DryRun || string.IsNullOrWhiteSpace(options.ConfigDirectory))
        {
            return;
        }

        var historyFilePath = GetHistoryFilePath(options);
        if (string.IsNullOrWhiteSpace(historyFilePath))
        {
            return;
        }

        Directory.CreateDirectory(options.ConfigDirectory);

        var entry = new GenerationRunHistoryEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow.ToString("O"),
            ToolName = options.ToolName,
            ToolVersion = options.ToolVersion,
            UserName = Environment.UserName,
            ConfigPath = string.IsNullOrWhiteSpace(options.ConfigPath)
                ? string.Empty
                : NormalizeRelativePath(Path.GetRelativePath(options.RootDirectory, options.ConfigPath)),
            RepoRoot = ".",
            GeneratedFileCount = result.WrittenCount,
            SkippedExistingCount = result.SkippedExistingCount
        };

        var line = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;
        File.AppendAllText(historyFilePath, line);
    }

    private static string NormalizeRelativePath(string path) => path.Replace('\\', '/');

    private sealed class GenerationRunHistoryEntry
    {
        public required string TimestampUtc { get; init; }
        public required string ToolName { get; init; }
        public required string ToolVersion { get; init; }
        public required string UserName { get; init; }
        public required string ConfigPath { get; init; }
        public required string RepoRoot { get; init; }
        public required int GeneratedFileCount { get; init; }
        public required int SkippedExistingCount { get; init; }
    }
}
