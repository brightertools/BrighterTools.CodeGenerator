using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.Runtime;
using Xunit;

namespace BrighterTools.CodeGenerator.Tests;

public class CodeGeneratorRunnerTests
{
    [Fact]
    public async Task RunAsync_WritesRunHistory_ForSuccessfulConfigBasedGeneration()
    {
        var repoRoot = CreateTemporaryRepository();
        try
        {
            var options = CreateGeneratorOptions(repoRoot);

            var result = await CodeGeneratorRunner.RunAsync(options);

            Assert.Equal(0, result);

            var historyPath = Path.Combine(options.ConfigDirectory, "generation-history.jsonl");
            Assert.True(File.Exists(historyPath));

            var line = Assert.Single(File.ReadAllLines(historyPath));
            Assert.Contains("\"toolName\":\"BrighterTools.CodeGenerator\"", line, StringComparison.Ordinal);
            Assert.Contains("\"toolVersion\":\"2.0.3\"", line, StringComparison.Ordinal);
            Assert.Contains("\"configPath\":\"CodeGeneration/codegen.json\"", line, StringComparison.Ordinal);
            Assert.Contains("\"repoRoot\":\".\"", line, StringComparison.Ordinal);
            Assert.Contains("\"generatedFileCount\":1", line, StringComparison.Ordinal);
            Assert.Contains("\"skippedExistingCount\":0", line, StringComparison.Ordinal);
            Assert.Contains("\"userName\":\"", line, StringComparison.Ordinal);
            Assert.Contains("\"timestampUtc\":\"", line, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(repoRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_DoesNotWriteRunHistory_ForDryRun()
    {
        var repoRoot = CreateTemporaryRepository();
        try
        {
            var options = CreateGeneratorOptions(repoRoot);
            options = Copy(options, dryRun: true);

            var result = await CodeGeneratorRunner.RunAsync(options);

            Assert.Equal(0, result);
            Assert.False(File.Exists(Path.Combine(options.ConfigDirectory, "generation-history.jsonl")));
        }
        finally
        {
            Directory.Delete(repoRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_DoesNotWriteRunHistory_ForLegacyMode()
    {
        var repoRoot = CreateTemporaryRepository();
        try
        {
            var options = CreateGeneratorOptions(repoRoot);
            options = Copy(options, configPath: string.Empty, configDirectory: string.Empty);

            var result = await CodeGeneratorRunner.RunAsync(options);

            Assert.Equal(0, result);
            Assert.False(File.Exists(Path.Combine(repoRoot, "CodeGeneration", "generation-history.jsonl")));
        }
        finally
        {
            Directory.Delete(repoRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_DoesNotWriteRunHistory_WhenGenerationFails()
    {
        var repoRoot = CreateTemporaryRepository();
        try
        {
            var options = CreateGeneratorOptions(repoRoot);
            options = Copy(options, appProjectPath: Path.Combine(repoRoot, "App", "Missing.csproj"));

            await Assert.ThrowsAnyAsync<Exception>(() => CodeGeneratorRunner.RunAsync(options));
            Assert.False(File.Exists(Path.Combine(options.ConfigDirectory, "generation-history.jsonl")));
        }
        finally
        {
            Directory.Delete(repoRoot, recursive: true);
        }
    }

    private static GeneratorOptions CreateGeneratorOptions(string repoRoot)
    {
        var configDirectory = Path.Combine(repoRoot, "CodeGeneration");
        var configPath = Path.Combine(configDirectory, "codegen.json");
        Directory.CreateDirectory(configDirectory);
        File.WriteAllText(configPath, "{}");

        return new GeneratorOptions
        {
            ToolName = "BrighterTools.CodeGenerator",
            ToolVersion = "2.0.3",
            ConfigPath = configPath,
            ConfigDirectory = configDirectory,
            RootDirectory = repoRoot,
            AppProjectPath = Path.Combine(repoRoot, "App", "App.csproj"),
            TemplatesDirectory = Path.Combine(AppContext.BaseDirectory, "Templates"),
            AppDirectory = Path.Combine(repoRoot, "App"),
            ModelNamespace = "App.Domain.Models",
            RepositoryNamespace = "App.Data.Repositories",
            ServiceNamespace = "App.Services",
            DtoNamespace = "App.Dto",
            ControllerNamespace = "Web.Server.Controllers",
            TenantNamespace = "App.Infrastructure.Security.MultiTenancy",
            CurrentUserNamespace = "App.Security.Auth",
            TypeExtensionsNamespace = "App.Extensions",
            ListRequestNamespace = "App.Dto",
            ServiceResultNamespace = "App.Domain.Results",
            ListResultNamespace = "App.Domain.Results",
            DataNamespace = "App.Data",
            EnumNamespacePrefixes = ["App.Domain.Enums", "App.Data"],
            EnabledGenerators = ["repositories"]
        };
    }

    private static string CreateTemporaryRepository()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repoRoot);

        var appDirectory = Path.Combine(repoRoot, "App");
        Directory.CreateDirectory(appDirectory);
        Directory.CreateDirectory(Path.Combine(appDirectory, "Domain", "Models"));

        File.WriteAllText(
            Path.Combine(appDirectory, "App.csproj"),
            """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
""");

        File.WriteAllText(
            Path.Combine(appDirectory, "Domain", "Models", "Widget.cs"),
            """
namespace App.Domain.Models;

public class Widget
{
    public int Id { get; set; }
}
""");

        return repoRoot;
    }

    private static GeneratorOptions Copy(
        GeneratorOptions options,
        bool? dryRun = null,
        string? configPath = null,
        string? configDirectory = null,
        string? appProjectPath = null)
    {
        return new GeneratorOptions
        {
            ToolName = options.ToolName,
            ToolVersion = options.ToolVersion,
            ToolCommand = options.ToolCommand,
            ConfigPath = configPath ?? options.ConfigPath,
            ConfigDirectory = configDirectory ?? options.ConfigDirectory,
            RootDirectory = options.RootDirectory,
            ProjectPath = options.ProjectPath,
            AppProjectPath = appProjectPath ?? options.AppProjectPath,
            TemplatesDirectory = options.TemplatesDirectory,
            AppDirectory = options.AppDirectory,
            ModelNamespace = options.ModelNamespace,
            RepositoryNamespace = options.RepositoryNamespace,
            ServiceNamespace = options.ServiceNamespace,
            DtoNamespace = options.DtoNamespace,
            ControllerNamespace = options.ControllerNamespace,
            ControllerGeneratedDirectory = options.ControllerGeneratedDirectory,
            ControllerStubDirectory = options.ControllerStubDirectory,
            TenantNamespace = options.TenantNamespace,
            CurrentUserNamespace = options.CurrentUserNamespace,
            TypeExtensionsNamespace = options.TypeExtensionsNamespace,
            ListRequestNamespace = options.ListRequestNamespace,
            ServiceResultNamespace = options.ServiceResultNamespace,
            ListResultNamespace = options.ListResultNamespace,
            DataNamespace = options.DataNamespace,
            EnumNamespacePrefixes = options.EnumNamespacePrefixes,
            TypeScriptModelNamespacePrefixes = options.TypeScriptModelNamespacePrefixes,
            TypeScriptModelsOutputPath = options.TypeScriptModelsOutputPath,
            TypeScriptEnumsOutputPath = options.TypeScriptEnumsOutputPath,
            TypeScriptServiceScaffoldsOutputDirectory = options.TypeScriptServiceScaffoldsOutputDirectory,
            TypeScriptCoreTypesImportPath = options.TypeScriptCoreTypesImportPath,
            TypeScriptGeneratedModelsImportPath = options.TypeScriptGeneratedModelsImportPath,
            TypeScriptHttpRequestImportPath = options.TypeScriptHttpRequestImportPath,
            TypeScriptModelsGeneratedOnly = options.TypeScriptModelsGeneratedOnly,
            EnabledGenerators = options.EnabledGenerators,
            IncludedModels = options.IncludedModels,
            ExcludedModels = options.ExcludedModels,
            ServiceExcludedModels = options.ServiceExcludedModels,
            TypeScriptServiceIncludedModels = options.TypeScriptServiceIncludedModels,
            TypeScriptServiceExcludedModels = options.TypeScriptServiceExcludedModels,
            ControllerScaffoldExcludedModels = options.ControllerScaffoldExcludedModels,
            TypeScriptModelExcludedTypeNames = options.TypeScriptModelExcludedTypeNames,
            CleanupDirectories = options.CleanupDirectories,
            CleanupFilePatterns = options.CleanupFilePatterns,
            VerifyRequiredFiles = options.VerifyRequiredFiles,
            VerifyDotnetBuildProjects = options.VerifyDotnetBuildProjects,
            VerifyFrontendWorkingDirectories = options.VerifyFrontendWorkingDirectories,
            VerifyFrontendBuildCommands = options.VerifyFrontendBuildCommands,
            VerifySkipBuildLockCheck = options.VerifySkipBuildLockCheck,
            DryRun = dryRun ?? options.DryRun
        };
    }
}
