using BrighterTools.CodeGenerator.Metadata;
using System.Text.Json;

namespace BrighterTools.CodeGenerator.Configuration;

internal static class GeneratorOptionsLoader
{
    public static GeneratorOptions Resolve(CommandLine.CommandLineOptions commandLineOptions)
    {
        if (!string.IsNullOrWhiteSpace(commandLineOptions.ConfigPath))
        {
            return LoadFromConfigPath(commandLineOptions.ConfigPath!, commandLineOptions.DryRun);
        }

        return ResolveLegacyOptions(commandLineOptions.DryRun);
    }

    internal static GeneratorOptions ResolveLegacyOptions(bool dryRun)
    {
        var rootDirectory = ResolveLegacyRootDirectory();
        return new GeneratorOptions
        {
            RootDirectory = rootDirectory,
            ProjectPath = Path.Combine(rootDirectory, "BrighterTools.CodeGenerator", "BrighterTools.CodeGenerator.csproj"),
            AppProjectPath = Path.Combine(rootDirectory, "App", "App.csproj"),
            TemplatesDirectory = Path.Combine(rootDirectory, "BrighterTools.CodeGenerator", "Templates"),
            AppDirectory = Path.Combine(rootDirectory, "App"),
            ControllerGeneratedDirectory = Path.Combine("Web.Server", "Controllers", "Generated"),
            ControllerStubDirectory = Path.Combine("Web.Server", "Controllers"),
            DryRun = dryRun
        };
    }

    internal static GeneratorOptions LoadFromConfigPath(string configPath, bool dryRun, string? workingDirectory = null)
    {
        var baseDirectory = string.IsNullOrWhiteSpace(workingDirectory)
            ? Environment.CurrentDirectory
            : workingDirectory;
        var fullConfigPath = ResolveAgainstBase(baseDirectory, configPath);
        if (!File.Exists(fullConfigPath))
        {
            throw new FileNotFoundException($"Code generation config file not found: {fullConfigPath}");
        }

        var rawOptions = JsonSerializer.Deserialize<GeneratorOptions>(
            File.ReadAllText(fullConfigPath),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
            ?? throw new InvalidOperationException($"Unable to deserialize code generation config: {fullConfigPath}");

        var configDirectory = Path.GetDirectoryName(fullConfigPath)
            ?? throw new InvalidOperationException($"Unable to determine config directory for: {fullConfigPath}");

        var rootDirectory = ResolveRequiredPath(configDirectory, rawOptions.RootDirectory, nameof(rawOptions.RootDirectory));
        var appProjectPath = ResolveRequiredPath(configDirectory, rawOptions.AppProjectPath, nameof(rawOptions.AppProjectPath));
        var appDirectory = string.IsNullOrWhiteSpace(rawOptions.AppDirectory)
            ? Path.GetDirectoryName(appProjectPath) ?? throw new InvalidOperationException($"Unable to determine App directory from project path: {appProjectPath}")
            : ResolveAgainstBase(configDirectory, rawOptions.AppDirectory);

        var templatesDirectory = string.IsNullOrWhiteSpace(rawOptions.TemplatesDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Templates")
            : ResolveAgainstBase(configDirectory, rawOptions.TemplatesDirectory);

        var projectPath = string.IsNullOrWhiteSpace(rawOptions.ProjectPath)
            ? string.Empty
            : ResolveAgainstBase(configDirectory, rawOptions.ProjectPath);

        var controllerGeneratedDirectory = string.IsNullOrWhiteSpace(rawOptions.ControllerGeneratedDirectory)
            ? Path.Combine("Web.Server", "Controllers", "Generated")
            : ResolveOptionalRelativePath(configDirectory, rawOptions.ControllerGeneratedDirectory, rootDirectory);
        var controllerStubDirectory = string.IsNullOrWhiteSpace(rawOptions.ControllerStubDirectory)
            ? Path.Combine("Web.Server", "Controllers")
            : ResolveOptionalRelativePath(configDirectory, rawOptions.ControllerStubDirectory, rootDirectory);
        var typeScriptModelsOutputPath = ResolveOptionalRelativePath(configDirectory, rawOptions.TypeScriptModelsOutputPath, rootDirectory);
        var typeScriptEnumsOutputPath = ResolveOptionalRelativePath(configDirectory, rawOptions.TypeScriptEnumsOutputPath, rootDirectory);
        var typeScriptServiceScaffoldsOutputDirectory = ResolveOptionalRelativePath(configDirectory, rawOptions.TypeScriptServiceScaffoldsOutputDirectory, rootDirectory);

        return new GeneratorOptions
        {
            ToolName = string.IsNullOrWhiteSpace(rawOptions.ToolName) ? "BrighterTools.CodeGenerator" : rawOptions.ToolName,
            ToolVersion = string.IsNullOrWhiteSpace(rawOptions.ToolVersion) ? "2.0.0" : rawOptions.ToolVersion,
            ToolCommand = string.IsNullOrWhiteSpace(rawOptions.ToolCommand) ? "brightertools-codegenerator" : rawOptions.ToolCommand,
            RootDirectory = rootDirectory,
            ProjectPath = projectPath,
            AppProjectPath = appProjectPath,
            TemplatesDirectory = templatesDirectory,
            AppDirectory = appDirectory,
            ModelNamespace = string.IsNullOrWhiteSpace(rawOptions.ModelNamespace) ? "App.Domain.Models" : rawOptions.ModelNamespace,
            RepositoryNamespace = string.IsNullOrWhiteSpace(rawOptions.RepositoryNamespace) ? "App.Data.Repositories" : rawOptions.RepositoryNamespace,
            ServiceNamespace = string.IsNullOrWhiteSpace(rawOptions.ServiceNamespace) ? "App.Services" : rawOptions.ServiceNamespace,
            DtoNamespace = string.IsNullOrWhiteSpace(rawOptions.DtoNamespace) ? "App.Dto" : rawOptions.DtoNamespace,
            ControllerNamespace = string.IsNullOrWhiteSpace(rawOptions.ControllerNamespace) ? "Web.Server.Controllers" : rawOptions.ControllerNamespace,
            ControllerGeneratedDirectory = controllerGeneratedDirectory,
            ControllerStubDirectory = controllerStubDirectory,
            TenantNamespace = string.IsNullOrWhiteSpace(rawOptions.TenantNamespace) ? "App.Infrastructure.Security.MultiTenancy" : rawOptions.TenantNamespace,
            CurrentUserNamespace = string.IsNullOrWhiteSpace(rawOptions.CurrentUserNamespace) ? "App.Security.Auth" : rawOptions.CurrentUserNamespace,
            TypeExtensionsNamespace = string.IsNullOrWhiteSpace(rawOptions.TypeExtensionsNamespace) ? "App.Extensions" : rawOptions.TypeExtensionsNamespace,
            ListRequestNamespace = string.IsNullOrWhiteSpace(rawOptions.ListRequestNamespace) ? "App.Dto" : rawOptions.ListRequestNamespace,
            ServiceResultNamespace = string.IsNullOrWhiteSpace(rawOptions.ServiceResultNamespace) ? "App.Domain.Results" : rawOptions.ServiceResultNamespace,
            ListResultNamespace = string.IsNullOrWhiteSpace(rawOptions.ListResultNamespace) ? "App.Domain.Results" : rawOptions.ListResultNamespace,
            DataNamespace = string.IsNullOrWhiteSpace(rawOptions.DataNamespace) ? "App.Data" : rawOptions.DataNamespace,
            EnumNamespacePrefixes = NormalizePrefixes(rawOptions.EnumNamespacePrefixes, ["App.Domain.Enums", "App.Data"]),
            TypeScriptModelNamespacePrefixes = NormalizePrefixes(rawOptions.TypeScriptModelNamespacePrefixes, []),
            TypeScriptModelsOutputPath = typeScriptModelsOutputPath,
            TypeScriptEnumsOutputPath = typeScriptEnumsOutputPath,
            TypeScriptServiceScaffoldsOutputDirectory = typeScriptServiceScaffoldsOutputDirectory,
            TypeScriptCoreTypesImportPath = string.IsNullOrWhiteSpace(rawOptions.TypeScriptCoreTypesImportPath) ? "../../types/core-app-types" : rawOptions.TypeScriptCoreTypesImportPath,
            TypeScriptGeneratedModelsImportPath = string.IsNullOrWhiteSpace(rawOptions.TypeScriptGeneratedModelsImportPath) ? "../../types/generated/api-models.g" : rawOptions.TypeScriptGeneratedModelsImportPath,
            TypeScriptHttpRequestImportPath = string.IsNullOrWhiteSpace(rawOptions.TypeScriptHttpRequestImportPath) ? "../httpRequest" : rawOptions.TypeScriptHttpRequestImportPath,
            TypeScriptModelsGeneratedOnly = rawOptions.TypeScriptModelsGeneratedOnly,
            EnabledGenerators = rawOptions.EnabledGenerators ?? [],
            IncludedModels = rawOptions.IncludedModels ?? [],
            ExcludedModels = rawOptions.ExcludedModels ?? [],
            ServiceExcludedModels = rawOptions.ServiceExcludedModels ?? [],
            TypeScriptServiceIncludedModels = rawOptions.TypeScriptServiceIncludedModels ?? [],
            TypeScriptServiceExcludedModels = rawOptions.TypeScriptServiceExcludedModels ?? [],
            ControllerScaffoldExcludedModels = rawOptions.ControllerScaffoldExcludedModels ?? [],
            TypeScriptModelExcludedTypeNames = rawOptions.TypeScriptModelExcludedTypeNames ?? [],
            CleanupDirectories = ResolveOptionalRelativePathList(configDirectory, rawOptions.CleanupDirectories, rootDirectory, nameof(rawOptions.CleanupDirectories)),
            CleanupFilePatterns = rawOptions.CleanupFilePatterns ?? [],
            VerifyRequiredFiles = ResolveOptionalRelativePathList(configDirectory, rawOptions.VerifyRequiredFiles, rootDirectory, nameof(rawOptions.VerifyRequiredFiles)),
            VerifyDotnetBuildProjects = ResolveOptionalRelativePathList(configDirectory, rawOptions.VerifyDotnetBuildProjects, rootDirectory, nameof(rawOptions.VerifyDotnetBuildProjects)),
            VerifyFrontendWorkingDirectories = ResolveOptionalRelativePathList(configDirectory, rawOptions.VerifyFrontendWorkingDirectories, rootDirectory, nameof(rawOptions.VerifyFrontendWorkingDirectories)),
            VerifyFrontendBuildCommands = rawOptions.VerifyFrontendBuildCommands ?? [],
            VerifySkipBuildLockCheck = rawOptions.VerifySkipBuildLockCheck,
            DryRun = rawOptions.DryRun || dryRun
        };
    }

    private static string ResolveLegacyRootDirectory()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    private static string ResolveRequiredPath(string baseDirectory, string path, string optionName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"Required generator option '{optionName}' was not supplied.");
        }

        return ResolveAgainstBase(baseDirectory, path);
    }

    private static string ResolveAgainstBase(string baseDirectory, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private static string ResolveOptionalRelativePath(string baseDirectory, string path, string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var absolutePath = ResolveAgainstBase(baseDirectory, path);
        return Path.GetRelativePath(rootDirectory, absolutePath);
    }

    private static IReadOnlyList<string> ResolveOptionalRelativePathList(string baseDirectory, IReadOnlyList<string>? paths, string rootDirectory, string optionName)
    {
        if (paths is null || paths.Count == 0)
        {
            return [];
        }

        var resolvedPaths = new List<string>(paths.Count);
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            if (Path.IsPathRooted(path))
            {
                throw new InvalidOperationException($"Generator option '{optionName}' only supports relative paths.");
            }

            var absolutePath = ResolveAgainstBase(baseDirectory, path);
            resolvedPaths.Add(Path.GetRelativePath(rootDirectory, absolutePath));
        }

        return resolvedPaths;
    }

    private static IReadOnlyList<string> NormalizePrefixes(IReadOnlyList<string>? prefixes, IReadOnlyList<string> defaults)
    {
        if (prefixes is null || prefixes.Count == 0)
        {
            return defaults;
        }

        return prefixes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
