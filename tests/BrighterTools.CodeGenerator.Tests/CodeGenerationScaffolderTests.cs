using BrighterTools.CodeGenerator.Scaffolding;
using System.Text.Json;
using Xunit;

namespace BrighterTools.CodeGenerator.Tests;

public class CodeGenerationScaffolderTests
{
    [Fact]
    public void Scaffold_CreatesStarterFiles_ForConventionRepo()
    {
        var repoRoot = CreateTemporaryDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(repoRoot, "App"));
            Directory.CreateDirectory(Path.Combine(repoRoot, "Web", "Web.Server", "Controllers", "V1"));
            Directory.CreateDirectory(Path.Combine(repoRoot, "Web", "web.client"));
            File.WriteAllText(Path.Combine(repoRoot, "App", "App.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(repoRoot, "Web", "Web.Server", "Web.Server.csproj"), "<Project />");

            var scaffolder = new CodeGenerationScaffolder();
            var result = scaffolder.Scaffold(new CodeGenerationScaffoldOptions
            {
                RepoRootPath = repoRoot
            });

            Assert.NotEmpty(result.CreatedFiles);
            Assert.Empty(result.UnresolvedItems);

            var configPath = Path.Combine(repoRoot, "CodeGeneration", "codegen.json");
            Assert.True(File.Exists(configPath));
            Assert.True(File.Exists(Path.Combine(repoRoot, "CodeGeneration", "GenerateCode.ps1")));
            Assert.True(File.Exists(Path.Combine(repoRoot, "CodeGeneration", "GenerateCode.bat")));
            Assert.Contains(
                "function Test-IsWindowsHost",
                File.ReadAllText(Path.Combine(repoRoot, "CodeGeneration", "VerifyCodeGeneration.ps1")));

            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            var root = document.RootElement;

            Assert.Equal("..", root.GetProperty("rootDirectory").GetString());
            Assert.Equal(string.Empty, root.GetProperty("projectPath").GetString());
            Assert.Equal(string.Empty, root.GetProperty("templatesDirectory").GetString());
            Assert.Equal("../App/App.csproj", root.GetProperty("appProjectPath").GetString());
            Assert.Equal("../Web/Web.Server/Controllers/V1", root.GetProperty("controllerStubDirectory").GetString());
        }
        finally
        {
            Directory.Delete(repoRoot, recursive: true);
        }
    }

    [Fact]
    public void Scaffold_PreservesExistingFiles_WithoutForce()
    {
        var repoRoot = CreateTemporaryDirectory();
        try
        {
            var codeGenerationDirectory = Path.Combine(repoRoot, "CodeGeneration");
            Directory.CreateDirectory(codeGenerationDirectory);
            var readmePath = Path.Combine(codeGenerationDirectory, "README.md");
            File.WriteAllText(readmePath, "keep me");

            var scaffolder = new CodeGenerationScaffolder();
            var result = scaffolder.Scaffold(new CodeGenerationScaffoldOptions
            {
                RepoRootPath = repoRoot
            });

            Assert.Contains(readmePath, result.SkippedFiles);
            Assert.Equal("keep me", File.ReadAllText(readmePath));
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
