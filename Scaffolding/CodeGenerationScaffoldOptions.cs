namespace BrighterTools.CodeGenerator.Scaffolding;

internal sealed class CodeGenerationScaffoldOptions
{
    public string? RepoRootPath { get; init; }
    public string? ConfigDirectory { get; init; }
    public bool Force { get; init; }
}
