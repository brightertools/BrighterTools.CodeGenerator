using BrighterTools.CodeGenerator.Generators;
using BrighterTools.CodeGenerator.Inspectors;
using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;
using Microsoft.Build.Locator;

namespace BrighterTools.CodeGenerator.Runtime;

internal static class CodeGeneratorRunner
{
    public static async Task<int> RunAsync(GeneratorOptions options)
    {
        RegisterMsBuild();

        var modelInspector = new ModelInspector(options.AppProjectPath, options.ModelNamespace);
        var enumInspector = new EnumInspector(options.AppProjectPath, options.EnumNamespacePrefixes);

        var allModels = await modelInspector.InspectAsync();
        var models = FilterModels(allModels, options);
        var enums = await enumInspector.InspectAsync();
        var apiModels = await InspectApiModelsAsync(options);

        var context = new GenerationContext
        {
            Options = options,
            Models = models,
            AllModels = allModels,
            ApiModels = apiModels,
            Enums = enums,
            GeneratedAt = DateTimeOffset.Now
        };

        var templateLoader = new TemplateLoader(options.TemplatesDirectory);
        var templateRenderer = new TemplateRenderer(templateLoader);

        var generators = CreateGenerators(templateRenderer, options).ToList();

        var firstPassGenerators = generators.Where(x => x is not TypeScriptModelsGenerator).ToList();
        var firstPassFiles = firstPassGenerators.SelectMany(x => x.Generate(context)).ToList();
        var totalFiles = WriteGeneratedFiles(options, firstPassFiles);

        if (generators.Any(x => x is TypeScriptModelsGenerator))
        {
            var refreshedApiModels = await InspectApiModelsAsync(options);
            var refreshedContext = new GenerationContext
            {
                Options = options,
                Models = models,
                AllModels = allModels,
                ApiModels = refreshedApiModels,
                Enums = enums,
                GeneratedAt = context.GeneratedAt
            };

            var typeScriptFiles = generators
                .Where(x => x is TypeScriptModelsGenerator)
                .SelectMany(x => x.Generate(refreshedContext))
                .ToList();

            totalFiles += WriteGeneratedFiles(options, typeScriptFiles);
        }

        Console.WriteLine($"Generated {totalFiles} file(s).");
        return 0;
    }

    private static int WriteGeneratedFiles(GeneratorOptions options, IEnumerable<GeneratedFile> generatedFiles)
    {
        var count = 0;
        foreach (var generatedFile in generatedFiles)
        {
            count++;
            var fullPath = Path.Combine(options.RootDirectory, generatedFile.RelativePath);
            if (!generatedFile.OverwriteIfExists && File.Exists(fullPath))
            {
                Console.WriteLine($"Skipped existing {generatedFile.RelativePath}");
                continue;
            }

            if (options.DryRun)
            {
                Console.WriteLine($"[dry-run] {generatedFile.RelativePath}");
                continue;
            }

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, generatedFile.Content);
            Console.WriteLine($"Generated {generatedFile.RelativePath}");
        }

        return count;
    }

    private static IEnumerable<ICodeGenerator> CreateGenerators(TemplateRenderer templateRenderer, GeneratorOptions options)
    {
        var generators = new List<ICodeGenerator>();

        if (IsGeneratorEnabled(options, "repositories"))
        {
            generators.Add(new RepositoryGenerator(templateRenderer));
        }

        if (IsGeneratorEnabled(options, "repository-stubs", "repository-custom-stubs"))
        {
            generators.Add(new RepositoryCustomStubGenerator(templateRenderer));
        }

        if (IsGeneratorEnabled(options, "services"))
        {
            generators.Add(new ServiceGenerator(templateRenderer));
        }

        if (IsGeneratorEnabled(options, "service-stubs", "service-custom-stubs"))
        {
            generators.Add(new ServiceCustomStubGenerator(templateRenderer));
        }

        if (IsGeneratorEnabled(options, "dto-requests"))
        {
            generators.Add(new DtoRequestGenerator(templateRenderer));
        }

        if (IsGeneratorEnabled(options, "dto-responses"))
        {
            generators.Add(new DtoResponseGenerator(templateRenderer));
        }

        if (IsGeneratorEnabled(options, "dbcontext"))
        {
            generators.Add(new DbContextGenerator(templateRenderer));
        }

        if (IsGeneratorEnabled(options, "data-service-registration", "service-registrations"))
        {
            generators.Add(new DataServiceRegistrationGenerator(templateRenderer));
        }

        if (IsGeneratorEnabled(options, "generated-controllers"))
        {
            generators.Add(new GeneratedControllerGenerator(templateRenderer));
        }

        if (IsGeneratorEnabled(options, "controller-scaffolds", "controller-scaffold"))
        {
            generators.Add(new ControllerScaffoldGenerator(templateRenderer));
        }

        if (IsGeneratorEnabled(options, "controller-stubs"))
        {
            generators.Add(new ControllerStubGenerator(templateRenderer));
        }

        if (!string.IsNullOrWhiteSpace(options.TypeScriptEnumsOutputPath)
            && IsGeneratorEnabled(options, "typescript-enums"))
        {
            generators.Add(new TypeScriptEnumsGenerator(templateRenderer));
        }

        if (!string.IsNullOrWhiteSpace(options.TypeScriptModelsOutputPath)
            && IsGeneratorEnabled(options, "typescript-models"))
        {
            generators.Add(new TypeScriptModelsGenerator(templateRenderer));
        }

        if (!string.IsNullOrWhiteSpace(options.TypeScriptServiceScaffoldsOutputDirectory)
            && IsGeneratorEnabled(options, "typescript-service-scaffolds", "typescript-services"))
        {
            generators.Add(new TypeScriptServiceScaffoldGenerator(templateRenderer));
        }

        return generators;
    }

    private static IReadOnlyList<ClassMetadata> FilterModels(IReadOnlyList<ClassMetadata> allModels, GeneratorOptions options)
    {
        var includedModels = new HashSet<string>(options.IncludedModels ?? [], StringComparer.OrdinalIgnoreCase);
        var excludedModels = new HashSet<string>(options.ExcludedModels ?? [], StringComparer.OrdinalIgnoreCase);

        if (includedModels.Count == 0 && excludedModels.Count == 0)
        {
            return allModels;
        }

        return allModels
            .Where(model => includedModels.Count == 0 || includedModels.Contains(model.Name))
            .Where(model => !excludedModels.Contains(model.Name))
            .ToList();
    }

    private static bool IsGeneratorEnabled(GeneratorOptions options, params string[] names)
    {
        if (options.EnabledGenerators == null || options.EnabledGenerators.Count == 0)
        {
            return true;
        }

        return names.Any(name => options.EnabledGenerators.Contains(name, StringComparer.OrdinalIgnoreCase));
    }

    private static void RegisterMsBuild()
    {
        if (MSBuildLocator.IsRegistered)
        {
            return;
        }

        MSBuildLocator.RegisterDefaults();
    }

    private static async Task<IReadOnlyList<ApiModelMetadata>> InspectApiModelsAsync(GeneratorOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TypeScriptModelsOutputPath)
            || options.TypeScriptModelNamespacePrefixes.Count == 0)
        {
            return [];
        }

        var inspector = new ApiModelInspector(
            options.AppProjectPath,
            options.TypeScriptModelNamespacePrefixes,
            options.EnumNamespacePrefixes,
            options.TypeScriptModelsGeneratedOnly);

        return await inspector.InspectAsync();
    }
}
