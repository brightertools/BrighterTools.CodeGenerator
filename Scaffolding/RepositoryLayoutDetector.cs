namespace BrighterTools.CodeGenerator.Scaffolding;

internal static class RepositoryLayoutDetector
{
    public static RepositoryLayout Detect(string repoRootPath)
    {
        var appProjectPath = DetectAppProjectPath(repoRootPath);
        var appDirectory = appProjectPath is null ? null : Path.GetDirectoryName(appProjectPath);
        var backendProjectPath = DetectBackendProjectPath(repoRootPath);
        var frontendDirectory = DetectFrontendDirectory(repoRootPath);
        var controllerStubDirectory = DetectControllerStubDirectory(repoRootPath);
        var controllerGeneratedDirectory = controllerStubDirectory is null
            ? null
            : Path.Combine(controllerStubDirectory, "Generated");

        return new RepositoryLayout
        {
            RepoRootPath = repoRootPath,
            RepoName = Path.GetFileName(repoRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            AppProjectPath = appProjectPath,
            AppDirectory = appDirectory,
            BackendProjectPath = backendProjectPath,
            FrontendDirectory = frontendDirectory,
            ControllerStubDirectory = controllerStubDirectory,
            ControllerGeneratedDirectory = controllerGeneratedDirectory,
            TypeScriptModelsOutputPath = frontendDirectory is null ? null : Path.Combine(frontendDirectory, "src", "types", "generated", "api-models.g.ts"),
            TypeScriptEnumsOutputPath = frontendDirectory is null ? null : Path.Combine(frontendDirectory, "src", "types", "generated", "app-enums.g.ts"),
            TypeScriptServiceScaffoldsOutputDirectory = frontendDirectory is null ? null : Path.Combine(frontendDirectory, "src", "services", "generated")
        };
    }

    private static string? DetectAppProjectPath(string repoRootPath)
    {
        var preferredPath = Path.Combine(repoRootPath, "App", "App.csproj");
        if (File.Exists(preferredPath))
        {
            return preferredPath;
        }

        var appDirectory = Path.Combine(repoRootPath, "App");
        if (!Directory.Exists(appDirectory))
        {
            return null;
        }

        return Directory.GetFiles(appDirectory, "*.csproj", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string? DetectBackendProjectPath(string repoRootPath)
    {
        var preferredPath = Path.Combine(repoRootPath, "Web", "Web.Server", "Web.Server.csproj");
        if (File.Exists(preferredPath))
        {
            return preferredPath;
        }

        var backendDirectory = Path.Combine(repoRootPath, "Web", "Web.Server");
        if (!Directory.Exists(backendDirectory))
        {
            return null;
        }

        return Directory.GetFiles(backendDirectory, "*.csproj", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string? DetectFrontendDirectory(string repoRootPath)
    {
        var preferredDirectory = Path.Combine(repoRootPath, "Web", "web.client");
        if (Directory.Exists(preferredDirectory))
        {
            return preferredDirectory;
        }

        return null;
    }

    private static string? DetectControllerStubDirectory(string repoRootPath)
    {
        var preferredV1Directory = Path.Combine(repoRootPath, "Web", "Web.Server", "Controllers", "V1");
        if (Directory.Exists(preferredV1Directory))
        {
            return preferredV1Directory;
        }

        var preferredDirectory = Path.Combine(repoRootPath, "Web", "Web.Server", "Controllers");
        if (Directory.Exists(preferredDirectory))
        {
            return preferredDirectory;
        }

        return null;
    }
}
