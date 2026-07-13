namespace BrighterTools.CodeGenerator.Scaffolding;

internal sealed class RepositoryLayout
{
    public required string RepoRootPath { get; init; }
    public required string RepoName { get; init; }
    public string? AppProjectPath { get; init; }
    public string? AppDirectory { get; init; }
    public string? BackendProjectPath { get; init; }
    public string? FrontendDirectory { get; init; }
    public string? ControllerStubDirectory { get; init; }
    public string? ControllerGeneratedDirectory { get; init; }
    public string? TypeScriptModelsOutputPath { get; init; }
    public string? TypeScriptEnumsOutputPath { get; init; }
    public string? TypeScriptServiceScaffoldsOutputDirectory { get; init; }
}
