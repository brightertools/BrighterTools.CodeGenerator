using BrighterTools.CodeGenerator.Generators;
using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;
using Xunit;

namespace BrighterTools.CodeGenerator.Tests;

public class TemplateRendererTests
{
    [Fact]
    public void RepositoryGenerator_UsesDeterministicHeaderWithoutRunTimestamp()
    {
        var generator = new RepositoryGenerator(new TemplateRenderer(new TemplateLoader(Path.Combine(AppContext.BaseDirectory, "Templates"))));
        var context = new GenerationContext
        {
            Options = new GeneratorOptions
            {
                ToolName = "BrighterTools.CodeGenerator",
                ToolVersion = "2.0.4",
                ListRequestNamespace = "App.Dto",
                TypeExtensionsNamespace = "App.Extensions",
                TenantNamespace = "App.Infrastructure.Security.MultiTenancy",
                DataNamespace = "App.Data"
            },
            Models =
            [
                new ClassMetadata
                {
                    Name = "Widget",
                    PluralName = "Widgets",
                    Namespace = "App.Domain.Models"
                }
            ],
            AllModels = [],
            ApiModels = [],
            Enums = []
        };

        var file = Assert.Single(generator.Generate(context));

        Assert.Contains("This file was generated using BrighterTools.CodeGenerator", file.Content, StringComparison.Ordinal);
        Assert.Contains("Generator version: 2.0.4", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("Generated on", file.Content, StringComparison.Ordinal);
    }
}
