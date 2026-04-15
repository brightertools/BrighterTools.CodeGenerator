using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class DtoRequestGenerator(TemplateRenderer templateRenderer) : GeneratorBase(templateRenderer)
{
    public override IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        foreach (var model in context.Models.Where(ShouldGenerate))
        {
            var folder = Path.Combine("App", "Dto", model.CollectionDtoFolderName, "Requests");
            yield return Render(context, model, "dto_request_create", "dto_request.scriban", Path.Combine(folder, $"Create{model.Name}Request.g.cs"));
            yield return Render(context, model, "dto_request_update", "dto_request.scriban", Path.Combine(folder, $"Update{model.Name}Request.g.cs"));
            yield return Render(context, model, "dto_request_upsert", "dto_request.scriban", Path.Combine(folder, $"Upsert{model.Name}Request.g.cs"));
        }
    }

    private static bool ShouldGenerate(ClassMetadata model) =>
        !model.IsBaseModel && !model.IsJsonModel && !model.IsJoinTable;
}
