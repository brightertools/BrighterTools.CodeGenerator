using BrighterTools.CodeGenerator.CommandLine;
using BrighterTools.CodeGenerator.Configuration;
using BrighterTools.CodeGenerator.Runtime;
using BrighterTools.CodeGenerator.Scaffolding;

namespace BrighterTools.CodeGenerator;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var commandLineOptions = CommandLineParser.Parse(args);
            return commandLineOptions.CommandKind switch
            {
                CommandKind.Generate => await RunGenerateAsync(commandLineOptions),
                CommandKind.Init => RunInit(commandLineOptions),
                CommandKind.Help => RunHelp(),
                _ => throw new InvalidOperationException($"Unsupported command kind: {commandLineOptions.CommandKind}")
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static async Task<int> RunGenerateAsync(CommandLineOptions commandLineOptions)
    {
        if (commandLineOptions.UsesLegacyGenerateMode && string.IsNullOrWhiteSpace(commandLineOptions.ConfigPath))
        {
            Console.WriteLine("Warning: running without --config uses deprecated legacy same-repo assumptions.");
        }

        var options = GeneratorOptionsLoader.Resolve(commandLineOptions);
        return await CodeGeneratorRunner.RunAsync(options);
    }

    private static int RunInit(CommandLineOptions commandLineOptions)
    {
        var scaffolder = new CodeGenerationScaffolder();
        var result = scaffolder.Scaffold(new CodeGenerationScaffoldOptions
        {
            RepoRootPath = commandLineOptions.RepoRootPath,
            ConfigDirectory = commandLineOptions.ConfigDirectory,
            Force = commandLineOptions.Force
        });

        foreach (var file in result.CreatedFiles)
        {
            Console.WriteLine($"Created {file}");
        }

        foreach (var file in result.SkippedFiles)
        {
            Console.WriteLine($"Skipped existing {file}");
        }

        if (result.UnresolvedItems.Count > 0)
        {
            Console.WriteLine("Initialization completed with manual follow-up needed:");
            foreach (var item in result.UnresolvedItems)
            {
                Console.WriteLine($" - {item}");
            }
        }
        else
        {
            Console.WriteLine("Initialization completed.");
        }

        return 0;
    }

    private static int RunHelp()
    {
        Console.WriteLine("""
BrighterTools.CodeGenerator

Usage:
  brightertools-codegenerator generate --config <path> [--dry-run]
  brightertools-codegenerator init [--repo-root <path>] [--config-dir <path>] [--force]

Compatibility:
  brightertools-codegenerator --config <path> [--dry-run]

Notes:
  - Relative --config paths resolve from the current working directory.
  - Relative paths inside codegen.json resolve from the folder containing codegen.json.
  - The init command scaffolds cross-platform CodeGeneration scripts for a consuming repo.
""");
        return 0;
    }
}
