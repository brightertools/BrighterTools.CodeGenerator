using BrighterTools.CodeGenerator.Metadata;
using System.Collections.Generic;
using BrighterTools.CodeGenerator.TemplateEngine;

namespace BrighterTools.CodeGenerator.Generators;

public sealed class TypeScriptModelsGenerator(TemplateRenderer templateRenderer) : ICodeGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Options.TypeScriptModelsOutputPath))
        {
            yield break;
        }

        var excludedTypeNames = new HashSet<string>(context.Options.TypeScriptModelExcludedTypeNames ?? [], StringComparer.OrdinalIgnoreCase);
        var apiModels = context.ApiModels.Where(x => !excludedTypeNames.Contains(x.Name)).ToList();

        var usesEnums = apiModels
            .SelectMany(x => x.Properties)
            .Any(x => x.TypeScriptType.Contains("Enums.", StringComparison.Ordinal));

        if (usesEnums && string.IsNullOrWhiteSpace(context.Options.TypeScriptEnumsOutputPath))
        {
            throw new InvalidOperationException("TypeScript model generation requires TypeScriptEnumsOutputPath when enum-backed properties are present.");
        }

        var templateModel = new
        {
            tool = new
            {
                name = context.Options.ToolName,
                version = context.Options.ToolVersion,
                generated_at = context.GeneratedAt.ToString("O")
            },
            options = context.Options,
            models = context.Models,
            api_models = apiModels,
            enums = context.Enums,
            artifact = new
            {
                name = "typescript_models",
                output_path = context.Options.TypeScriptModelsOutputPath,
                enums_import_path = usesEnums
                    ? ResolveImportPath(context.Options.TypeScriptModelsOutputPath, context.Options.TypeScriptEnumsOutputPath)
                    : string.Empty
            }
        };

        var content = templateRenderer.Render("typescript_models.scriban", templateModel).Trim() + Environment.NewLine;
        yield return new GeneratedFile
        {
            RelativePath = context.Options.TypeScriptModelsOutputPath,
            Content = content
        };
    }

    private static string ResolveImportPath(string fromOutputPath, string toOutputPath)
    {
        var fromDirectory = Path.GetDirectoryName(fromOutputPath);
        var baseDirectory = string.IsNullOrWhiteSpace(fromDirectory) ? "." : fromDirectory;
        var relativePath = Path.GetRelativePath(baseDirectory, toOutputPath).Replace('\\', '/');
        var importPath = Path.ChangeExtension(relativePath, null) ?? relativePath;

        return importPath.StartsWith(".", StringComparison.Ordinal)
            ? importPath
            : $"./{importPath}";
    }
}


