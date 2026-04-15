using BrighterTools.CodeGenerator.Metadata;
using System.Collections.Generic;
using BrighterTools.CodeGenerator.TemplateEngine;
using Humanizer;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class TypeScriptServiceScaffoldGenerator(TemplateRenderer templateRenderer) : GeneratorBase(templateRenderer)
{
    public override IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        var includedModels = new HashSet<string>(context.Options.TypeScriptServiceIncludedModels ?? [], StringComparer.OrdinalIgnoreCase);
        var excludedModels = new HashSet<string>(context.Options.TypeScriptServiceExcludedModels ?? [], StringComparer.OrdinalIgnoreCase);

        foreach (var model in context.AllModels
                     .Where(ShouldGenerate)
                     .Where(x => includedModels.Count == 0 || includedModels.Contains(x.Name))
                     .Where(x => !excludedModels.Contains(x.Name)))
        {
            var path = Path.Combine(context.Options.TypeScriptServiceScaffoldsOutputDirectory, $"{model.Name.Camelize()}Service.g.ts");
            var templateModel = new
            {
                tool = new
                {
                    name = context.Options.ToolName,
                    version = context.Options.ToolVersion,
                    generated_at = context.GeneratedAt.ToString("O")
                },
                options = context.Options,
                model,
                artifact = new
                {
                    service_export_name = $"{model.Name.Camelize()}Service",
                    custom_service_file = $"{model.Name.Camelize()}Service.ts",
                    route_segment = model.PluralName.ToLowerInvariant()
                }
            };

            yield return Render("typescript_service_scaffold.scriban", templateModel, path);
        }
    }

    private static bool ShouldGenerate(ClassMetadata model) =>
        !model.IsBaseModel && !model.IsJsonModel && !model.IsJoinTable;
}


