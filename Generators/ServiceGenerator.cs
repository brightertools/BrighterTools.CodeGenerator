using BrighterTools.CodeGenerator.Metadata;
using System.Collections.Generic;
using BrighterTools.CodeGenerator.TemplateEngine;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class ServiceGenerator(TemplateRenderer templateRenderer) : GeneratorBase(templateRenderer)
{
    public override IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        var excludedModels = new HashSet<string>(context.Options.ServiceExcludedModels ?? [], StringComparer.OrdinalIgnoreCase);

        foreach (var model in context.Models.Where(ShouldGenerate).Where(x => !excludedModels.Contains(x.Name)))
        {
            var path = Path.Combine("App", "Services", "Generated", $"{model.ServiceName}.g.cs");
            yield return Render(context, model, "service", context.Options.PersistenceMode == "scoped-read" ? "service.scoped-read.scriban" : "service.scriban", path);
        }
    }

    private static bool ShouldGenerate(ClassMetadata model) =>
        !model.IsBaseModel && !model.IsJsonModel && !model.IsJoinTable;
}



