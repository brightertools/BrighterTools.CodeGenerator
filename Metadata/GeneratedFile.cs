namespace BrighterTools.CodeGenerator.Metadata;

public sealed class GeneratedFile
{
    public required string RelativePath { get; init; }
    public required string Content { get; init; }
    public bool OverwriteIfExists { get; init; } = true;
}
