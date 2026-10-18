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
                version = context.Options.ToolVersion
            },
            models = context.Models,
            enums = context.Enums,
            options = context.Options
        };

        yield return new GeneratedFile
        {
            RelativePath = Path.Combine("App", "Data", "Generated", context.Options.DbContextName + ".g.cs"),
            Content = templateRenderer.Render("dbcontext.scriban", templateModel).Trim() + Environment.NewLine
        };

        yield return new GeneratedFile
        {
            RelativePath = Path.Combine("App", "Data", "Generated", context.Options.DbContextName + ".Persistence.g.cs"),
            Content = templateRenderer.Render("dbcontext.persistence.scriban", templateModel).Trim() + Environment.NewLine
        };

        yield return new GeneratedFile
        {
            RelativePath = Path.Combine("App", "Data", context.Options.DbContextName + ".cs"),
            Content = templateRenderer.Render("dbcontext.custom.scriban", templateModel).Trim() + Environment.NewLine,
            OverwriteIfExists = false
        };
    }
}

