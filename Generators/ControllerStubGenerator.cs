using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;
using System.Collections.Generic;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class ControllerStubGenerator(TemplateRenderer templateRenderer) : GeneratorBase(templateRenderer)
{
    public override IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        var excludedModels = new HashSet<string>(context.Options.ControllerScaffoldExcludedModels ?? [], StringComparer.OrdinalIgnoreCase);

        foreach (var model in context.Models.Where(ShouldGenerate).Where(x => !excludedModels.Contains(x.Name)))
        {
            var path = Path.Combine(context.Options.ControllerStubDirectory, $"{model.PluralName}Controller.cs");
            var file = Render(context, model, "controller-stub", "controller.stub.scriban", path);
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
