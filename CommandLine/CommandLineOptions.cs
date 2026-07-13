namespace BrighterTools.CodeGenerator.CommandLine;

internal sealed class CommandLineOptions
{
    public CommandKind CommandKind { get; init; } = CommandKind.Generate;
    public string? ConfigPath { get; init; }
    public bool DryRun { get; init; }
    public string? RepoRootPath { get; init; }
    public string? ConfigDirectory { get; init; }
    public bool Force { get; init; }
    public bool UsesLegacyGenerateMode { get; init; }
}
