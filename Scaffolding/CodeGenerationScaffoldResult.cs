namespace BrighterTools.CodeGenerator.Scaffolding;

internal sealed class CodeGenerationScaffoldResult
{
    public IReadOnlyList<string> CreatedFiles { get; init; } = [];
    public IReadOnlyList<string> SkippedFiles { get; init; } = [];
    public IReadOnlyList<string> UnresolvedItems { get; init; } = [];
}
