using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class TypeScriptEnumsGenerator(TemplateRenderer templateRenderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Options.TypeScriptEnumsOutputPath))
        {
            yield break;
        }

        var templateModel = new
        {
            tool = new
            {
                name = context.Options.ToolName,
                version = context.Options.ToolVersion,
                generated_at = context.GeneratedAt.ToString("O")
            },
            options = context.Options,
            models = context.Models,
            api_models = context.ApiModels,
            enums = context.Enums,
            artifact = new
            {
                name = "typescript_enums",
                output_path = context.Options.TypeScriptEnumsOutputPath
            }
        };

        var content = templateRenderer.Render("typescript_enums.scriban", templateModel).Trim() + Environment.NewLine;
        yield return new GeneratedFile
        {
            RelativePath = context.Options.TypeScriptEnumsOutputPath,
            Content = content
        };
    }
}
