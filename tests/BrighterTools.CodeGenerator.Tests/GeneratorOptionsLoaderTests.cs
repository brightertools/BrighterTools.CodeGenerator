using BrighterTools.CodeGenerator.Configuration;
using Xunit;

namespace BrighterTools.CodeGenerator.Tests;

public class GeneratorOptionsLoaderTests
{
    [Fact]
    public void LoadFromConfigPath_ResolvesConfigRelativePaths_FromWorkingDirectory()
    {
        var repoRoot = CreateTemporaryDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(repoRoot, "App"));
            Directory.CreateDirectory(Path.Combine(repoRoot, "Web", "Web.Server", "Controllers", "V1", "Generated"));
            Directory.CreateDirectory(Path.Combine(repoRoot, "Web", "web.client", "src", "services", "generated"));
            var configDirectory = Path.Combine(repoRoot, "CodeGeneration");
            Directory.CreateDirectory(configDirectory);

            File.WriteAllText(Path.Combine(repoRoot, "App", "App.csproj"), "<Project />");

            File.WriteAllText(
                Path.Combine(configDirectory, "codegen.json"),
                """
{
  "toolName": "Test.CodeGeneration",
  "toolVersion": "2.0.3",
  "toolCommand": "brightertools-codegenerator",
  "rootDirectory": "..",
  "projectPath": "",
  "appProjectPath": "../App/App.csproj",
  "appDirectory": "../App",
  "templatesDirectory": "",
  "controllerGeneratedDirectory": "../Web/Web.Server/Controllers/V1/Generated",
  "controllerStubDirectory": "../Web/Web.Server/Controllers/V1",
  "cleanupDirectories": [
    "../App/Data/Generated",
    "../Web/web.client/src/services/generated"
  ],
  "cleanupFilePatterns": [
    "*.g.cs",
    "*.g.ts"
  ],
  "verifyRequiredFiles": [
    "../Web/web.client/src/types/generated/api-models.g.ts"
  ],
  "verifyDotnetBuildProjects": [
    "../App/App.csproj"
  ],
  "verifyFrontendWorkingDirectories": [
    "../Web/web.client"
  ],
  "verifyFrontendBuildCommands": [
    "npm run build"
  ]
}
""");

            var options = GeneratorOptionsLoader.LoadFromConfigPath("CodeGeneration/codegen.json", dryRun: false, workingDirectory: repoRoot);

            Assert.Equal(repoRoot, options.RootDirectory);
            Assert.Equal(Path.Combine(repoRoot, "App", "App.csproj"), options.AppProjectPath);
            Assert.Equal(Path.Combine("Web", "Web.Server", "Controllers", "V1", "Generated"), options.ControllerGeneratedDirectory);
            Assert.Contains(Path.Combine("App", "Data", "Generated"), options.CleanupDirectories);
            Assert.Contains(Path.Combine("Web", "web.client", "src", "services", "generated"), options.CleanupDirectories);
            Assert.Contains(Path.Combine("Web", "web.client", "src", "types", "generated", "api-models.g.ts"), options.VerifyRequiredFiles);
            Assert.Contains(Path.Combine("App", "App.csproj"), options.VerifyDotnetBuildProjects);
            Assert.Contains(Path.Combine("Web", "web.client"), options.VerifyFrontendWorkingDirectories);
        }
        finally
        {
            Directory.Delete(repoRoot, recursive: true);
        }
    }

    [Fact]
    public void LoadFromConfigPath_RejectsAbsoluteCleanupPaths()
    {
        var repoRoot = CreateTemporaryDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(repoRoot, "App"));
            var configDirectory = Path.Combine(repoRoot, "CodeGeneration");
            Directory.CreateDirectory(configDirectory);
            File.WriteAllText(Path.Combine(repoRoot, "App", "App.csproj"), "<Project />");

            File.WriteAllText(
                Path.Combine(configDirectory, "codegen.json"),
                $$"""
{
  "rootDirectory": "..",
  "appProjectPath": "../App/App.csproj",
  "appDirectory": "../App",
  "cleanupDirectories": [
    "{{repoRoot.Replace("\\", "\\\\")}}"
  ]
}
""");

            var exception = Assert.Throws<InvalidOperationException>(() =>
                GeneratorOptionsLoader.LoadFromConfigPath("CodeGeneration/codegen.json", dryRun: false, workingDirectory: repoRoot));

            Assert.Contains("cleanupDirectories", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(repoRoot, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
