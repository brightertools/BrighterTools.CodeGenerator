using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class GeneratedControllerGenerator(TemplateRenderer templateRenderer) : GeneratorBase(templateRenderer)
{
    public override IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        foreach (var model in context.Models.Where(ShouldGenerate))
        {
            var path = Path.Combine(context.Options.ControllerGeneratedDirectory, $"Generated{model.PluralName}Controller.g.cs");
            yield return Render(context, model, "generated-controller", "controller.generated.scriban", path);
        }
    }

    private static bool ShouldGenerate(ClassMetadata model) =>
        !model.IsBaseModel && !model.IsJsonModel && !model.IsJoinTable;
}
