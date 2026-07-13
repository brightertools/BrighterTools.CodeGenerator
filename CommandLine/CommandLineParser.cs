namespace BrighterTools.CodeGenerator.CommandLine;

internal static class CommandLineParser
{
    public static CommandLineOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return ParseGenerate(args, usesLegacyGenerateMode: true);
        }

        var firstArg = args[0];
        if (string.Equals(firstArg, "generate", StringComparison.OrdinalIgnoreCase))
        {
            return ParseGenerate(args[1..], usesLegacyGenerateMode: false);
        }

        if (string.Equals(firstArg, "init", StringComparison.OrdinalIgnoreCase))
        {
            return ParseInit(args[1..]);
        }

        if (string.Equals(firstArg, "help", StringComparison.OrdinalIgnoreCase)
            || string.Equals(firstArg, "--help", StringComparison.OrdinalIgnoreCase)
            || string.Equals(firstArg, "-h", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandLineOptions
            {
                CommandKind = CommandKind.Help
            };
        }

        return ParseGenerate(args, usesLegacyGenerateMode: true);
    }

    private static CommandLineOptions ParseGenerate(string[] args, bool usesLegacyGenerateMode)
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

            if (string.Equals(arg, "--config", StringComparison.OrdinalIgnoreCase))
            {
                if ((i + 1) >= args.Length)
                {
                    throw new InvalidOperationException("Missing value for --config.");
                }

                configPath = args[++i];
                continue;
            }

            if (arg.StartsWith("--config=", StringComparison.OrdinalIgnoreCase))
            {
                configPath = arg["--config=".Length..];
                continue;
            }

            throw new InvalidOperationException($"Unrecognized generate argument: {arg}");
        }

        return new CommandLineOptions
        {
            CommandKind = CommandKind.Generate,
            ConfigPath = configPath,
            DryRun = dryRun,
            UsesLegacyGenerateMode = usesLegacyGenerateMode
        };
    }

    private static CommandLineOptions ParseInit(string[] args)
    {
        string? repoRootPath = null;
        string? configDirectory = null;
        var force = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--force", StringComparison.OrdinalIgnoreCase))
            {
                force = true;
                continue;
            }

            if (string.Equals(arg, "--repo-root", StringComparison.OrdinalIgnoreCase))
            {
                if ((i + 1) >= args.Length)
                {
                    throw new InvalidOperationException("Missing value for --repo-root.");
                }

                repoRootPath = args[++i];
                continue;
            }

            if (arg.StartsWith("--repo-root=", StringComparison.OrdinalIgnoreCase))
            {
                repoRootPath = arg["--repo-root=".Length..];
                continue;
            }

            if (string.Equals(arg, "--config-dir", StringComparison.OrdinalIgnoreCase))
            {
                if ((i + 1) >= args.Length)
                {
                    throw new InvalidOperationException("Missing value for --config-dir.");
                }

                configDirectory = args[++i];
                continue;
            }

            if (arg.StartsWith("--config-dir=", StringComparison.OrdinalIgnoreCase))
            {
                configDirectory = arg["--config-dir=".Length..];
                continue;
            }

            throw new InvalidOperationException($"Unrecognized init argument: {arg}");
        }

        return new CommandLineOptions
        {
            CommandKind = CommandKind.Init,
            RepoRootPath = repoRootPath,
            ConfigDirectory = configDirectory,
            Force = force
        };
    }
}
