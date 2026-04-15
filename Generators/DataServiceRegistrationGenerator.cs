using BrighterTools.CodeGenerator.Metadata;
using System.Collections.Generic;
using BrighterTools.CodeGenerator.TemplateEngine;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class DataServiceRegistrationGenerator(TemplateRenderer templateRenderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        var repositoryModels = context.Models.Where(ShouldGenerate).ToList();
        var serviceExcludedModels = new HashSet<string>(context.Options.ServiceExcludedModels ?? [], StringComparer.OrdinalIgnoreCase);
        var serviceModels = repositoryModels.Where(x => !serviceExcludedModels.Contains(x.Name)).ToList();

        var templateModel = new
        {
            tool = new
            {
                name = context.Options.ToolName,
                version = context.Options.ToolVersion,
                generated_at = context.GeneratedAt.ToString("O")
            },
            repository_models = repositoryModels,
            service_models = serviceModels,
            enums = context.Enums,
            options = context.Options
        };

        yield return new GeneratedFile
        {
            RelativePath = Path.Combine("App", "Data", "Generated", "GeneratedAppDataServiceExtensions.g.cs"),
            Content = templateRenderer.Render("generated_data_services.scriban", templateModel).Trim() + Environment.NewLine
        };
    }

    private static bool ShouldGenerate(ClassMetadata model) =>
        !model.IsBaseModel && !model.IsJsonModel && !model.IsJoinTable;
}


