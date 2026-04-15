using BrighterTools.CodeGenerator.Generators;
using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;
using Xunit;

namespace BrighterTools.CodeGenerator.Tests;

public class ControllerStubGeneratorTests
{
    [Fact]
    public void Generate_Skips_Models_Excluded_From_ControllerScaffoldExcludedModels()
    {
        var generator = CreateGenerator();
        var context = CreateContext(
            [
                new ClassMetadata { Name = "UserRefreshToken", PluralName = "UserRefreshTokens" },
                new ClassMetadata { Name = "Post", PluralName = "Posts" }
            ],
            controllerScaffoldExcludedModels: ["UserRefreshToken"]);

        var files = generator.Generate(context).ToList();

        Assert.DoesNotContain(files, file => file.RelativePath.EndsWith("UserRefreshTokensController.cs", StringComparison.Ordinal));
        Assert.Contains(files, file => file.RelativePath.EndsWith("PostsController.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void Generate_Still_Skips_JoinTable_And_JsonModel()
    {
        var generator = CreateGenerator();
        var context = CreateContext(
            [
                new ClassMetadata { Name = "PostTag", PluralName = "PostTags", IsJoinTable = true },
                new ClassMetadata { Name = "SettingsJson", PluralName = "SettingsJson", IsJsonModel = true },
                new ClassMetadata { Name = "Post", PluralName = "Posts" }
            ],
            controllerScaffoldExcludedModels: []);

        var files = generator.Generate(context).ToList();

        Assert.DoesNotContain(files, file => file.RelativePath.EndsWith("PostTagsController.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(files, file => file.RelativePath.EndsWith("SettingsJsonController.cs", StringComparison.Ordinal));
        Assert.Contains(files, file => file.RelativePath.EndsWith("PostsController.cs", StringComparison.Ordinal));
    }

    private static ControllerStubGenerator CreateGenerator()
    {
        var templatesDirectory = Path.Combine(AppContext.BaseDirectory, "Templates");
        var templateRenderer = new TemplateRenderer(new TemplateLoader(templatesDirectory));
        return new ControllerStubGenerator(templateRenderer);
    }

    private static GenerationContext CreateContext(IReadOnlyList<ClassMetadata> models, IReadOnlyList<string> controllerScaffoldExcludedModels)
    {
        return new GenerationContext
        {
            Options = new GeneratorOptions
            {
                ControllerStubDirectory = Path.Combine("Web.Server", "Controllers"),
                ControllerScaffoldExcludedModels = controllerScaffoldExcludedModels
            },
            Models = models,
            AllModels = [],
            ApiModels = [],
            Enums = [],
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}
