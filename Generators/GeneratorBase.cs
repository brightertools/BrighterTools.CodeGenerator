using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;

namespace BrighterTools.CodeGenerator.Generators;

public abstract class GeneratorBase(TemplateRenderer templateRenderer) : ICodeGenerator
{
    public abstract IEnumerable<GeneratedFile> Generate(GenerationContext context);

    protected GeneratedFile Render(GenerationContext context, ClassMetadata model, string artifactName, string templateName, string relativeOutputPath)
    {
        var templateModel = TemplateModelFactory.CreateModel(context, model, artifactName, relativeOutputPath);
        return Render(templateName, templateModel, relativeOutputPath);
    }

    protected GeneratedFile Render(string templateName, object templateModel, string relativeOutputPath)
    {
        var content = templateRenderer.Render(templateName, templateModel).Trim() + Environment.NewLine;
        return new GeneratedFile
        {
            RelativePath = relativeOutputPath,
            Content = content
        };
    }
}