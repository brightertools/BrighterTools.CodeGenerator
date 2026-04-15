using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class DbContextGenerator(TemplateRenderer templateRenderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        var templateModel = new
        {
            tool = new
            {
                name = context.Options.ToolName,
                version = context.Options.ToolVersion,
                generated_at = context.GeneratedAt.ToString("O")
            },
            models = context.Models,
            enums = context.Enums,
            options = context.Options
        };

        yield return new GeneratedFile
        {
            RelativePath = Path.Combine("App", "Data", "Generated", "ApplicationDbContext.g.cs"),
            Content = templateRenderer.Render("dbcontext.scriban", templateModel).Trim() + Environment.NewLine
        };

        yield return new GeneratedFile
        {
            RelativePath = Path.Combine("App", "Data", "Generated", "ApplicationDbContext.Persistence.g.cs"),
            Content = templateRenderer.Render("dbcontext.persistence.scriban", templateModel).Trim() + Environment.NewLine
        };

        yield return new GeneratedFile
        {
            RelativePath = Path.Combine("App", "Data", "ApplicationDbContext.cs"),
            Content = templateRenderer.Render("dbcontext.custom.scriban", templateModel).Trim() + Environment.NewLine,
            OverwriteIfExists = false
        };
    }
}
