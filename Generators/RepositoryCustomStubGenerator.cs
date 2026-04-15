using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class RepositoryCustomStubGenerator(TemplateRenderer templateRenderer) : GeneratorBase(templateRenderer)
{
    public override IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        foreach (var model in context.Models.Where(ShouldGenerate))
        {
            var path = Path.Combine("App", "Data", "Repositories", $"{model.RepositoryName}.cs");
            var file = Render(context, model, "repository-custom", "repository.custom.scriban", path);
            yield return new GeneratedFile
            {
                RelativePath = file.RelativePath,
                Content = file.Content,
                OverwriteIfExists = false
            };
        }
    }

    private static bool ShouldGenerate(ClassMetadata model) =>
        !model.IsBaseModel && !model.IsJsonModel && !model.IsJoinTable;
}