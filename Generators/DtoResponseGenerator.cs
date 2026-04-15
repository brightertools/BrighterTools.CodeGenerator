using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class DtoResponseGenerator(TemplateRenderer templateRenderer) : GeneratorBase(templateRenderer)
{
    public override IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        foreach (var model in context.Models.Where(ShouldGenerate))
        {
            var folder = Path.Combine("App", "Dto", model.CollectionDtoFolderName, "Responses");
            yield return Render(context, model, "dto_response", "dto_response.scriban", Path.Combine(folder, $"{model.Name}Response.g.cs"));
        }
    }

    private static bool ShouldGenerate(ClassMetadata model) =>
        !model.IsBaseModel && !model.IsJsonModel && !model.IsJoinTable;
}
