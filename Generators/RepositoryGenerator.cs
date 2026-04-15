using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class RepositoryGenerator(TemplateRenderer templateRenderer) : GeneratorBase(templateRenderer)
{
    public override IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        foreach (var model in context.Models.Where(ShouldGenerate))
        {
            var path = Path.Combine("App", "Data", "Repositories", "Generated", $"{model.RepositoryName}.g.cs");
            yield return Render(context, model, "repository", "repository.scriban", path);
        }
    }

    private static bool ShouldGenerate(ClassMetadata model) =>
        !model.IsBaseModel && !model.IsJsonModel && !model.IsJoinTable;
}
