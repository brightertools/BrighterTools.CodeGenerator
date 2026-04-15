using BrighterTools.CodeGenerator.Generators;
using BrighterTools.CodeGenerator.Inspectors;
using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;
using Microsoft.Build.Locator;
using System.Text.Json;

namespace BrighterTools.CodeGenerator;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        RegisterMsBuild();

        var commandLineOptions = ParseCommandLine(args);
        var options = ResolveGeneratorOptions(commandLineOptions);

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

    private static GeneratorOptions ResolveGeneratorOptions(CommandLineOptions commandLineOptions)
    {
        if (!string.IsNullOrWhiteSpace(commandLineOptions.ConfigPath))
        {
            return LoadGeneratorOptions(commandLineOptions.ConfigPath!, commandLineOptions.DryRun);
        }

        var rootDirectory = ResolveLegacyRootDirectory();
        return new GeneratorOptions
        {
            RootDirectory = rootDirectory,
            ProjectPath = Path.Combine(rootDirectory, "BrighterTools.CodeGenerator", "BrighterTools.CodeGenerator.csproj"),
            AppProjectPath = Path.Combine(rootDirectory, "App", "App.csproj"),
            TemplatesDirectory = Path.Combine(rootDirectory, "BrighterTools.CodeGenerator", "Templates"),
            AppDirectory = Path.Combine(rootDirectory, "App"),
            ControllerGeneratedDirectory = Path.Combine("Web.Server", "Controllers", "Generated"),
            ControllerStubDirectory = Path.Combine("Web.Server", "Controllers"),
            DryRun = commandLineOptions.DryRun
        };
    }

    private static GeneratorOptions LoadGeneratorOptions(string configPath, bool dryRun)
    {
        var fullConfigPath = ResolveAgainstBase(Environment.CurrentDirectory, configPath);
        if (!File.Exists(fullConfigPath))
        {
            throw new FileNotFoundException($"Code generation config file not found: {fullConfigPath}");
        }

        var rawOptions = JsonSerializer.Deserialize<GeneratorOptions>(
            File.ReadAllText(fullConfigPath),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
            ?? throw new InvalidOperationException($"Unable to deserialize code generation config: {fullConfigPath}");

        var configDirectory = Path.GetDirectoryName(fullConfigPath)
            ?? throw new InvalidOperationException($"Unable to determine config directory for: {fullConfigPath}");

        var rootDirectory = ResolveRequiredPath(configDirectory, rawOptions.RootDirectory, nameof(rawOptions.RootDirectory));
        var appProjectPath = ResolveRequiredPath(configDirectory, rawOptions.AppProjectPath, nameof(rawOptions.AppProjectPath));
        var appDirectory = string.IsNullOrWhiteSpace(rawOptions.AppDirectory)
            ? Path.GetDirectoryName(appProjectPath) ?? throw new InvalidOperationException($"Unable to determine App directory from project path: {appProjectPath}")
            : ResolveAgainstBase(configDirectory, rawOptions.AppDirectory);

        var templatesDirectory = string.IsNullOrWhiteSpace(rawOptions.TemplatesDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Templates")
            : ResolveAgainstBase(configDirectory, rawOptions.TemplatesDirectory);

        var projectPath = string.IsNullOrWhiteSpace(rawOptions.ProjectPath)
            ? string.Empty
            : ResolveAgainstBase(configDirectory, rawOptions.ProjectPath);

        var controllerGeneratedDirectory = string.IsNullOrWhiteSpace(rawOptions.ControllerGeneratedDirectory)
            ? Path.Combine("Web.Server", "Controllers", "Generated")
            : ResolveOptionalRelativePath(configDirectory, rawOptions.ControllerGeneratedDirectory, rootDirectory);
        var controllerStubDirectory = string.IsNullOrWhiteSpace(rawOptions.ControllerStubDirectory)
            ? Path.Combine("Web.Server", "Controllers")
            : ResolveOptionalRelativePath(configDirectory, rawOptions.ControllerStubDirectory, rootDirectory);
        var typeScriptModelsOutputPath = ResolveOptionalRelativePath(configDirectory, rawOptions.TypeScriptModelsOutputPath, rootDirectory);
        var typeScriptEnumsOutputPath = ResolveOptionalRelativePath(configDirectory, rawOptions.TypeScriptEnumsOutputPath, rootDirectory);
        var typeScriptServiceScaffoldsOutputDirectory = ResolveOptionalRelativePath(configDirectory, rawOptions.TypeScriptServiceScaffoldsOutputDirectory, rootDirectory);

        return new GeneratorOptions
        {
            ToolName = string.IsNullOrWhiteSpace(rawOptions.ToolName) ? "BrighterTools.CodeGenerator" : rawOptions.ToolName,
            ToolVersion = string.IsNullOrWhiteSpace(rawOptions.ToolVersion) ? "7.0.0" : rawOptions.ToolVersion,
            RootDirectory = rootDirectory,
            ProjectPath = projectPath,
            AppProjectPath = appProjectPath,
            TemplatesDirectory = templatesDirectory,
            AppDirectory = appDirectory,
            ModelNamespace = string.IsNullOrWhiteSpace(rawOptions.ModelNamespace) ? "App.Domain.Models" : rawOptions.ModelNamespace,
            RepositoryNamespace = string.IsNullOrWhiteSpace(rawOptions.RepositoryNamespace) ? "App.Data.Repositories" : rawOptions.RepositoryNamespace,
            ServiceNamespace = string.IsNullOrWhiteSpace(rawOptions.ServiceNamespace) ? "App.Services" : rawOptions.ServiceNamespace,
            DtoNamespace = string.IsNullOrWhiteSpace(rawOptions.DtoNamespace) ? "App.Dto" : rawOptions.DtoNamespace,
            ControllerNamespace = string.IsNullOrWhiteSpace(rawOptions.ControllerNamespace) ? "Web.Server.Controllers" : rawOptions.ControllerNamespace,
            ControllerGeneratedDirectory = controllerGeneratedDirectory,
            ControllerStubDirectory = controllerStubDirectory,
            TenantNamespace = string.IsNullOrWhiteSpace(rawOptions.TenantNamespace) ? "App.Infrastructure.Security.MultiTenancy" : rawOptions.TenantNamespace,
            CurrentUserNamespace = string.IsNullOrWhiteSpace(rawOptions.CurrentUserNamespace) ? "App.Security.Auth" : rawOptions.CurrentUserNamespace,
            TypeExtensionsNamespace = string.IsNullOrWhiteSpace(rawOptions.TypeExtensionsNamespace) ? "App.Extensions" : rawOptions.TypeExtensionsNamespace,
            ListRequestNamespace = string.IsNullOrWhiteSpace(rawOptions.ListRequestNamespace) ? "App.Dto" : rawOptions.ListRequestNamespace,
            ServiceResultNamespace = string.IsNullOrWhiteSpace(rawOptions.ServiceResultNamespace) ? "App.Domain.Results" : rawOptions.ServiceResultNamespace,
            ListResultNamespace = string.IsNullOrWhiteSpace(rawOptions.ListResultNamespace) ? "App.Domain.Results" : rawOptions.ListResultNamespace,
            DataNamespace = string.IsNullOrWhiteSpace(rawOptions.DataNamespace) ? "App.Data" : rawOptions.DataNamespace,
            EnumNamespacePrefixes = NormalizePrefixes(rawOptions.EnumNamespacePrefixes, ["App.Domain.Enums", "App.Data"]),
            TypeScriptModelNamespacePrefixes = NormalizePrefixes(rawOptions.TypeScriptModelNamespacePrefixes, []),
            TypeScriptModelsOutputPath = typeScriptModelsOutputPath,
            TypeScriptEnumsOutputPath = typeScriptEnumsOutputPath,
            TypeScriptServiceScaffoldsOutputDirectory = typeScriptServiceScaffoldsOutputDirectory,
            TypeScriptCoreTypesImportPath = string.IsNullOrWhiteSpace(rawOptions.TypeScriptCoreTypesImportPath) ? "../../types/core-app-types" : rawOptions.TypeScriptCoreTypesImportPath,
            TypeScriptGeneratedModelsImportPath = string.IsNullOrWhiteSpace(rawOptions.TypeScriptGeneratedModelsImportPath) ? "../../types/generated/api-models.g" : rawOptions.TypeScriptGeneratedModelsImportPath,
            TypeScriptHttpRequestImportPath = string.IsNullOrWhiteSpace(rawOptions.TypeScriptHttpRequestImportPath) ? "../httpRequest" : rawOptions.TypeScriptHttpRequestImportPath,
            TypeScriptModelsGeneratedOnly = rawOptions.TypeScriptModelsGeneratedOnly,
            EnabledGenerators = rawOptions.EnabledGenerators ?? [],
            IncludedModels = rawOptions.IncludedModels ?? [],
            ExcludedModels = rawOptions.ExcludedModels ?? [],
            ServiceExcludedModels = rawOptions.ServiceExcludedModels ?? [],
            TypeScriptServiceIncludedModels = rawOptions.TypeScriptServiceIncludedModels ?? [],
            TypeScriptServiceExcludedModels = rawOptions.TypeScriptServiceExcludedModels ?? [],
            ControllerScaffoldExcludedModels = rawOptions.ControllerScaffoldExcludedModels ?? [],
            TypeScriptModelExcludedTypeNames = rawOptions.TypeScriptModelExcludedTypeNames ?? [],
            DryRun = rawOptions.DryRun || dryRun
        };
    }

    private static void RegisterMsBuild()
    {
        if (MSBuildLocator.IsRegistered)
        {
            return;
        }

        MSBuildLocator.RegisterDefaults();
    }

    private static CommandLineOptions ParseCommandLine(string[] args)
    {
        string? configPath = null;
        var dryRun = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--dry-run", StringComparison.OrdinalIgnoreCase))
            {
                dryRun = true;
                continue;
            }

            if (string.Equals(arg, "--config", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
            {
                configPath = args[++i];
                continue;
            }

            if (arg.StartsWith("--config=", StringComparison.OrdinalIgnoreCase))
            {
                configPath = arg["--config=".Length..];
            }
        }

        return new CommandLineOptions
        {
            ConfigPath = configPath,
            DryRun = dryRun
        };
    }

    private static string ResolveLegacyRootDirectory()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    private static string ResolveRequiredPath(string baseDirectory, string path, string optionName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"Required generator option '{optionName}' was not supplied.");
        }

        return ResolveAgainstBase(baseDirectory, path);
    }

    private static string ResolveAgainstBase(string baseDirectory, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private static string ResolveOptionalRelativePath(string baseDirectory, string path, string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var absolutePath = ResolveAgainstBase(baseDirectory, path);
        return Path.GetRelativePath(rootDirectory, absolutePath);
    }

    private static IReadOnlyList<string> NormalizePrefixes(IReadOnlyList<string>? prefixes, IReadOnlyList<string> defaults)
    {
        if (prefixes is null || prefixes.Count == 0)
        {
            return defaults;
        }

        return prefixes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
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

    private sealed class CommandLineOptions
    {
        public string? ConfigPath { get; init; }
        public bool DryRun { get; init; }
    }
}






