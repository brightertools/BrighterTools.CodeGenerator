namespace BrighterTools.CodeGenerator.Metadata;

public sealed class GeneratorOptions
{
    public string ToolName { get; init; } = "BrighterTools.CodeGenerator";
    public string ToolVersion { get; init; } = "7.0.0";
    public string RootDirectory { get; init; } = string.Empty;
    public string ProjectPath { get; init; } = string.Empty;
    public string AppProjectPath { get; init; } = string.Empty;
    public string TemplatesDirectory { get; init; } = string.Empty;
    public string AppDirectory { get; init; } = string.Empty;
    public string ModelNamespace { get; init; } = "App.Domain.Models";
    public string RepositoryNamespace { get; init; } = "App.Data.Repositories";
    public string ServiceNamespace { get; init; } = "App.Services";
    public string DtoNamespace { get; init; } = "App.Dto";
    public string ControllerNamespace { get; init; } = "Web.Server.Controllers";
    public string ControllerGeneratedDirectory { get; init; } = Path.Combine("Web.Server", "Controllers", "Generated");
    public string ControllerStubDirectory { get; init; } = Path.Combine("Web.Server", "Controllers");
    public string TenantNamespace { get; init; } = "App.Infrastructure.Security.MultiTenancy";
    public string CurrentUserNamespace { get; init; } = "App.Security.Auth";
    public string TypeExtensionsNamespace { get; init; } = "App.Extensions";
    public string ListRequestNamespace { get; init; } = "App.Dto";
    public string ServiceResultNamespace { get; init; } = "App.Domain.Results";
    public string ListResultNamespace { get; init; } = "App.Domain.Results";
    public string DataNamespace { get; init; } = "App.Data";
    public IReadOnlyList<string> EnumNamespacePrefixes { get; init; } = ["App.Domain.Enums", "App.Data"];
    public IReadOnlyList<string> TypeScriptModelNamespacePrefixes { get; init; } = [];
    public string TypeScriptModelsOutputPath { get; init; } = string.Empty;
    public string TypeScriptEnumsOutputPath { get; init; } = string.Empty;
    public string TypeScriptServiceScaffoldsOutputDirectory { get; init; } = string.Empty;
    public string TypeScriptCoreTypesImportPath { get; init; } = "../../types/core-app-types";
    public string TypeScriptGeneratedModelsImportPath { get; init; } = "../../types/generated/api-models.g";
    public string TypeScriptHttpRequestImportPath { get; init; } = "../httpRequest";
    public bool TypeScriptModelsGeneratedOnly { get; init; }
    public IReadOnlyList<string> EnabledGenerators { get; init; } = [];
    public IReadOnlyList<string> IncludedModels { get; init; } = [];
    public IReadOnlyList<string> ExcludedModels { get; init; } = [];
    public IReadOnlyList<string> ServiceExcludedModels { get; init; } = [];
    public IReadOnlyList<string> TypeScriptServiceIncludedModels { get; init; } = [];
    public IReadOnlyList<string> TypeScriptServiceExcludedModels { get; init; } = [];
    public IReadOnlyList<string> ControllerScaffoldExcludedModels { get; init; } = [];
    public IReadOnlyList<string> TypeScriptModelExcludedTypeNames { get; init; } = [];
    public bool DryRun { get; init; }
}





