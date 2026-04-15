using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class ServiceCustomStubGenerator(TemplateRenderer templateRenderer) : GeneratorBase(templateRenderer)
{
    public override IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        var excludedModels = new HashSet<string>(context.Options.ServiceExcludedModels ?? [], StringComparer.OrdinalIgnoreCase);

        foreach (var model in context.Models.Where(model => ShouldGenerate(model, excludedModels)))
        {
            var path = Path.Combine("App", "Services", $"{model.ServiceName}.cs");
            var file = Render(context, model, "service-custom", "service.custom.scriban", path);
            yield return new GeneratedFile
            {
                RelativePath = file.RelativePath,
                Content = file.Content,
                OverwriteIfExists = false
            };
        }
    }

    private static bool ShouldGenerate(ClassMetadata model, HashSet<string> excludedModels) =>
        !model.IsBaseModel && !model.IsJsonModel && !model.IsJoinTable && !excludedModels.Contains(model.Name);
}
