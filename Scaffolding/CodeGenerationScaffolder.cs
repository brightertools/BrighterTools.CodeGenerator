using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace BrighterTools.CodeGenerator.Scaffolding;

internal sealed class CodeGenerationScaffolder
{
    public CodeGenerationScaffoldResult Scaffold(CodeGenerationScaffoldOptions options)
    {
        var repoRootPath = ResolveRepoRootPath(options.RepoRootPath);
        var configDirectoryPath = ResolveConfigDirectoryPath(repoRootPath, options.ConfigDirectory);
        var layout = RepositoryLayoutDetector.Detect(repoRootPath);

        Directory.CreateDirectory(configDirectoryPath);

        var unresolvedItems = new List<string>();
        var createdFiles = new List<string>();
        var skippedFiles = new List<string>();

        var scaffoldModel = BuildScaffoldModel(layout, configDirectoryPath, unresolvedItems);
        var fileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(configDirectoryPath, "codegen.json")] = BuildConfigJson(scaffoldModel),
            [Path.Combine(configDirectoryPath, "GenerateCode.ps1")] = BuildGenerateScript(),
            [Path.Combine(configDirectoryPath, "DeleteGeneratedCode.ps1")] = BuildDeleteScript(),
            [Path.Combine(configDirectoryPath, "VerifyCodeGeneration.ps1")] = BuildVerifyScript(),
            [Path.Combine(configDirectoryPath, "GenerateCode.bat")] = BuildBatchShim("GenerateCode.ps1"),
            [Path.Combine(configDirectoryPath, "DeleteGeneratedCode.bat")] = BuildBatchShim("DeleteGeneratedCode.ps1"),
            [Path.Combine(configDirectoryPath, "VerifyCodeGeneration.bat")] = BuildBatchShim("VerifyCodeGeneration.ps1"),
            [Path.Combine(configDirectoryPath, "README.md")] = BuildReadme(scaffoldModel, unresolvedItems)
        };

        foreach (var pair in fileMap)
        {
            var fullPath = pair.Key;
            if (File.Exists(fullPath) && !options.Force)
            {
                skippedFiles.Add(fullPath);
                continue;
            }

            var parentDirectory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            File.WriteAllText(fullPath, pair.Value, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            createdFiles.Add(fullPath);
        }

        return new CodeGenerationScaffoldResult
        {
            CreatedFiles = createdFiles,
            SkippedFiles = skippedFiles,
            UnresolvedItems = unresolvedItems
        };
    }

    private static string ResolveRepoRootPath(string? repoRootPath)
    {
        var path = string.IsNullOrWhiteSpace(repoRootPath)
            ? Environment.CurrentDirectory
            : repoRootPath;
        return Path.GetFullPath(path);
    }

    private static string ResolveConfigDirectoryPath(string repoRootPath, string? configDirectory)
    {
        var path = string.IsNullOrWhiteSpace(configDirectory)
            ? "CodeGeneration"
            : configDirectory;

        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(repoRootPath, path));
    }

    private static CodeGenerationScaffoldModel BuildScaffoldModel(RepositoryLayout layout, string configDirectoryPath, List<string> unresolvedItems)
    {
        if (layout.AppProjectPath is null)
        {
            unresolvedItems.Add("Set appProjectPath in codegen.json to your application project.");
        }

        if (layout.ControllerStubDirectory is null)
        {
            unresolvedItems.Add("Set controllerStubDirectory and controllerGeneratedDirectory in codegen.json to match your API controller layout.");
        }

        if (layout.BackendProjectPath is null)
        {
            unresolvedItems.Add("Set verifyDotnetBuildProjects in codegen.json to include the backend projects you want to build during verification.");
        }

        if (layout.FrontendDirectory is null)
        {
            unresolvedItems.Add("Set frontend TypeScript output paths and verification commands if this repo has a frontend project.");
        }

        var appDirectory = layout.AppDirectory ?? string.Empty;
        var cleanupDirectories = new List<string>();
        if (!string.IsNullOrWhiteSpace(appDirectory))
        {
            cleanupDirectories.Add(ToConfigRelativePath(configDirectoryPath, Path.Combine(appDirectory, "Data", "Generated")));
            cleanupDirectories.Add(ToConfigRelativePath(configDirectoryPath, Path.Combine(appDirectory, "Data", "Repositories", "Generated")));
            cleanupDirectories.Add(ToConfigRelativePath(configDirectoryPath, Path.Combine(appDirectory, "Dto")));
            cleanupDirectories.Add(ToConfigRelativePath(configDirectoryPath, Path.Combine(appDirectory, "Services", "Generated")));
        }

        if (!string.IsNullOrWhiteSpace(layout.ControllerGeneratedDirectory))
        {
            cleanupDirectories.Add(ToConfigRelativePath(configDirectoryPath, layout.ControllerGeneratedDirectory));
        }

        if (!string.IsNullOrWhiteSpace(layout.FrontendDirectory))
        {
            cleanupDirectories.Add(ToConfigRelativePath(configDirectoryPath, Path.Combine(layout.FrontendDirectory, "src", "types", "generated")));
            cleanupDirectories.Add(ToConfigRelativePath(configDirectoryPath, Path.Combine(layout.FrontendDirectory, "src", "services", "generated")));
        }

        return new CodeGenerationScaffoldModel
        {
            RepoName = layout.RepoName,
            ToolName = $"{SanitizeName(layout.RepoName)}.CodeGeneration",
            ToolVersion = "2.0.0",
            RootDirectory = ToConfigRelativePath(configDirectoryPath, layout.RepoRootPath),
            ProjectPath = string.Empty,
            AppProjectPath = layout.AppProjectPath is null ? string.Empty : ToConfigRelativePath(configDirectoryPath, layout.AppProjectPath),
            AppDirectory = layout.AppDirectory is null ? string.Empty : ToConfigRelativePath(configDirectoryPath, layout.AppDirectory),
            TemplatesDirectory = string.Empty,
            ToolCommand = "brightertools-codegenerator",
            ControllerGeneratedDirectory = layout.ControllerGeneratedDirectory is null ? string.Empty : ToConfigRelativePath(configDirectoryPath, layout.ControllerGeneratedDirectory),
            ControllerStubDirectory = layout.ControllerStubDirectory is null ? string.Empty : ToConfigRelativePath(configDirectoryPath, layout.ControllerStubDirectory),
            TypeScriptModelsOutputPath = layout.TypeScriptModelsOutputPath is null ? string.Empty : ToConfigRelativePath(configDirectoryPath, layout.TypeScriptModelsOutputPath),
            TypeScriptEnumsOutputPath = layout.TypeScriptEnumsOutputPath is null ? string.Empty : ToConfigRelativePath(configDirectoryPath, layout.TypeScriptEnumsOutputPath),
            TypeScriptServiceScaffoldsOutputDirectory = layout.TypeScriptServiceScaffoldsOutputDirectory is null ? string.Empty : ToConfigRelativePath(configDirectoryPath, layout.TypeScriptServiceScaffoldsOutputDirectory),
            CleanupDirectories = cleanupDirectories,
            CleanupFilePatterns = ["*.g.cs", "*.g.ts"],
            VerifyRequiredFiles = [],
            VerifyDotnetBuildProjects = [.. new[] { layout.AppProjectPath, layout.BackendProjectPath }
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => ToConfigRelativePath(configDirectoryPath, path!))],
            VerifyFrontendWorkingDirectories = string.IsNullOrWhiteSpace(layout.FrontendDirectory)
                ? []
                : [ToConfigRelativePath(configDirectoryPath, layout.FrontendDirectory)],
            VerifyFrontendBuildCommands = string.IsNullOrWhiteSpace(layout.FrontendDirectory)
                ? []
                : ["npm run build"],
            VerifySkipBuildLockCheck = true
        };
    }

    private static string BuildConfigJson(CodeGenerationScaffoldModel scaffoldModel)
    {
        var jsonModel = new
        {
            toolName = scaffoldModel.ToolName,
            toolVersion = scaffoldModel.ToolVersion,
            rootDirectory = scaffoldModel.RootDirectory,
            projectPath = scaffoldModel.ProjectPath,
            appProjectPath = scaffoldModel.AppProjectPath,
            appDirectory = scaffoldModel.AppDirectory,
            templatesDirectory = scaffoldModel.TemplatesDirectory,
            toolCommand = scaffoldModel.ToolCommand,
            modelNamespace = "App.Domain.Models",
            repositoryNamespace = "App.Data.Repositories",
            serviceNamespace = "App.Services",
            dtoNamespace = "App.Dto",
            controllerNamespace = "Web.Server.Controllers",
            controllerGeneratedDirectory = scaffoldModel.ControllerGeneratedDirectory,
            controllerStubDirectory = scaffoldModel.ControllerStubDirectory,
            tenantNamespace = "App.Infrastructure.Security.MultiTenancy",
            currentUserNamespace = "App.Security.Auth",
            typeExtensionsNamespace = "App.Extensions",
            listRequestNamespace = "App.Dto",
            serviceResultNamespace = "App.Domain.Results",
            listResultNamespace = "App.Domain.Results",
            dataNamespace = "App.Data",
            enumNamespacePrefixes = new[] { "App.Domain.Enums", "App.Data" },
            enabledGenerators = new[]
            {
                "repositories",
                "repository-stubs",
                "services",
                "service-stubs",
                "dto-requests",
                "dto-responses",
                "data-service-registration",
                "controller-stubs",
                "controller-scaffolds",
                "typescript-enums",
                "typescript-models",
                "typescript-service-scaffolds"
            },
            typeScriptModelNamespacePrefixes = Array.Empty<string>(),
            typeScriptModelsGeneratedOnly = true,
            typeScriptModelsOutputPath = scaffoldModel.TypeScriptModelsOutputPath,
            typeScriptEnumsOutputPath = scaffoldModel.TypeScriptEnumsOutputPath,
            typeScriptServiceScaffoldsOutputDirectory = scaffoldModel.TypeScriptServiceScaffoldsOutputDirectory,
            typeScriptCoreTypesImportPath = "../../types/core-app-types",
            typeScriptGeneratedModelsImportPath = "../../types/generated/api-models.g",
            typeScriptHttpRequestImportPath = "../httpRequest",
            cleanupDirectories = scaffoldModel.CleanupDirectories,
            cleanupFilePatterns = scaffoldModel.CleanupFilePatterns,
            verifyRequiredFiles = scaffoldModel.VerifyRequiredFiles,
            verifyDotnetBuildProjects = scaffoldModel.VerifyDotnetBuildProjects,
            verifyFrontendWorkingDirectories = scaffoldModel.VerifyFrontendWorkingDirectories,
            verifyFrontendBuildCommands = scaffoldModel.VerifyFrontendBuildCommands,
            verifySkipBuildLockCheck = scaffoldModel.VerifySkipBuildLockCheck,
            includedModels = Array.Empty<string>(),
            excludedModels = Array.Empty<string>(),
            typeScriptServiceExcludedModels = Array.Empty<string>(),
            typeScriptModelExcludedTypeNames = Array.Empty<string>(),
            controllerScaffoldExcludedModels = Array.Empty<string>()
        };

        return JsonSerializer.Serialize(
            jsonModel,
            new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            }) + Environment.NewLine;
    }

    private static string BuildGenerateScript()
    {
        return """
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$GeneratorArgs
)

$ErrorActionPreference = 'Stop'

function Resolve-FullPath([string]$BaseDirectory, [string]$PathValue) {
    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return ''
    }

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BaseDirectory $PathValue))
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$configPath = Join-Path $scriptDir 'codegen.json'

if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
    throw "Code generation config not found: $configPath"
}

$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$repoRoot = Resolve-FullPath $scriptDir $config.rootDirectory

if ([string]::IsNullOrWhiteSpace($repoRoot) -or -not (Test-Path -LiteralPath $repoRoot -PathType Container)) {
    throw "Repository root not found from code generation config: $repoRoot"
}

$toolManifest = Join-Path $repoRoot '.config/dotnet-tools.json'
if (-not (Test-Path -LiteralPath $toolManifest -PathType Leaf)) {
    throw "Dotnet tool manifest not found: $toolManifest`nRun 'dotnet new tool-manifest' and 'dotnet tool install BrighterTools.CodeGenerator' from $repoRoot."
}

$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

if (-not (Test-Path -LiteralPath $env:DOTNET_CLI_HOME -PathType Container)) {
    New-Item -ItemType Directory -Path $env:DOTNET_CLI_HOME | Out-Null
}

$toolCommand = if ([string]::IsNullOrWhiteSpace($config.toolCommand)) { 'brightertools-codegenerator' } else { [string]$config.toolCommand }

Push-Location $repoRoot
try {
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw 'Dotnet tool restore failed.'
    }

    & dotnet tool run $toolCommand -- generate --config $configPath @GeneratorArgs
    if ($LASTEXITCODE -ne 0) {
        throw 'Code generation failed.'
    }
}
finally {
    Pop-Location
}
""";
    }

    private static string BuildDeleteScript()
    {
        return """
$ErrorActionPreference = 'Stop'

function Resolve-FullPath([string]$BaseDirectory, [string]$PathValue) {
    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return ''
    }

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BaseDirectory $PathValue))
}

function Test-IsChildPath([string]$RootPath, [string]$CandidatePath) {
    $root = [System.IO.Path]::GetFullPath($RootPath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $candidate = [System.IO.Path]::GetFullPath($CandidatePath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

    if ($candidate.Length -lt $root.Length) {
        return $false
    }

    if ($candidate.Equals($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    return $candidate.StartsWith($root + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$configPath = Join-Path $scriptDir 'codegen.json'

if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
    throw "Code generation config not found: $configPath"
}

$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$repoRoot = Resolve-FullPath $scriptDir $config.rootDirectory

if ([string]::IsNullOrWhiteSpace($repoRoot) -or -not (Test-Path -LiteralPath $repoRoot -PathType Container)) {
    throw "Repository root not found from code generation config: $repoRoot"
}

$cleanupDirectories = @($config.cleanupDirectories)
$cleanupPatterns = @($config.cleanupFilePatterns)

if ($cleanupPatterns.Count -eq 0) {
    $cleanupPatterns = @('*.g.cs', '*.g.ts')
}

$files = New-Object System.Collections.Generic.List[string]

foreach ($relativeDirectory in $cleanupDirectories) {
    if ([string]::IsNullOrWhiteSpace($relativeDirectory)) {
        continue
    }

    $directoryPath = Resolve-FullPath $scriptDir $relativeDirectory
    if (-not (Test-IsChildPath $repoRoot $directoryPath)) {
        throw "Cleanup directory escapes repository root: $relativeDirectory"
    }

    if (-not (Test-Path -LiteralPath $directoryPath -PathType Container)) {
        continue
    }

    Get-ChildItem -LiteralPath $directoryPath -Recurse -File | Where-Object {
        $fileName = $_.Name
        foreach ($pattern in $cleanupPatterns) {
            if ($fileName -like $pattern) {
                return $true
            }
        }

        return $false
    } | ForEach-Object {
        if (-not $files.Contains($_.FullName)) {
            [void]$files.Add($_.FullName)
        }
    }
}

if ($files.Count -eq 0) {
    Write-Host 'No generated files found.'
    return
}

foreach ($file in $files | Sort-Object) {
    Write-Host "Deleting $file"
    Remove-Item -LiteralPath $file -Force
}
""";
    }

    private static string BuildVerifyScript()
    {
        return """
$ErrorActionPreference = 'Stop'

function Resolve-FullPath([string]$BaseDirectory, [string]$PathValue) {
    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return ''
    }

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BaseDirectory $PathValue))
}

function Test-IsChildPath([string]$RootPath, [string]$CandidatePath) {
    $root = [System.IO.Path]::GetFullPath($RootPath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $candidate = [System.IO.Path]::GetFullPath($CandidatePath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

    if ($candidate.Length -lt $root.Length) {
        return $false
    }

    if ($candidate.Equals($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    return $candidate.StartsWith($root + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-IsWindowsHost {
    if ($null -ne $IsWindows) {
        return [bool]$IsWindows
    }

    return [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT
}

function Invoke-OptionalBuildLockCheck([string]$RepositoryRoot, [bool]$SkipCheck) {
    if ($SkipCheck -or -not (Test-IsWindowsHost)) {
        return
    }

    $rootPattern = [Regex]::Escape([System.IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\'))
    $candidateNames = @('MSBuild.exe', 'csc.exe', 'VBCSCompiler.exe', 'dotnet.exe')
    $buildVerbPattern = '(?i)\b(build|test|restore|run|publish|msbuild)\b'
    $processes = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
        $name = $_.Name
        if ($candidateNames -notcontains $name) { return $false }
        $commandLine = $_.CommandLine
        if ([string]::IsNullOrWhiteSpace($commandLine)) { return $false }
        if ($commandLine -notmatch $rootPattern) { return $false }
        if ($name -eq 'dotnet.exe' -and $commandLine -notmatch $buildVerbPattern) { return $false }
        return $true
    }

    if ($processes) {
        throw 'Verification cannot run while repo build processes are active. Wait for current builds/tests to finish, or set verifySkipBuildLockCheck to true.'
    }
}

function Invoke-PowerShellCommand([string]$CommandText) {
    if (Get-Command pwsh -ErrorAction SilentlyContinue) {
        & pwsh -NoProfile -Command $CommandText
        return
    }

    if (Test-IsWindowsHost) {
        & powershell -NoProfile -Command $CommandText
        return
    }

    throw 'pwsh is required to run frontend verification commands on non-Windows machines.'
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$configPath = Join-Path $scriptDir 'codegen.json'

if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
    throw "Code generation config not found: $configPath"
}

$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$repoRoot = Resolve-FullPath $scriptDir $config.rootDirectory

if ([string]::IsNullOrWhiteSpace($repoRoot) -or -not (Test-Path -LiteralPath $repoRoot -PathType Container)) {
    throw "Repository root not found from code generation config: $repoRoot"
}

$skipBuildLockCheck = $true
if ($null -ne $config.verifySkipBuildLockCheck) {
    $skipBuildLockCheck = [bool]$config.verifySkipBuildLockCheck
}

Invoke-OptionalBuildLockCheck -RepositoryRoot $repoRoot -SkipCheck $skipBuildLockCheck

Write-Host '[1/4] Cleaning generated files...'
& (Join-Path $scriptDir 'DeleteGeneratedCode.ps1')

Write-Host '[2/4] Regenerating code...'
& (Join-Path $scriptDir 'GenerateCode.ps1')

$requiredFiles = @($config.verifyRequiredFiles)
foreach ($relativeFile in $requiredFiles) {
    if ([string]::IsNullOrWhiteSpace($relativeFile)) {
        continue
    }

    $fullPath = Resolve-FullPath $scriptDir $relativeFile
    if (-not (Test-IsChildPath $repoRoot $fullPath)) {
        throw "Verification file escapes repository root: $relativeFile"
    }

    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Verification failed: expected generated file missing: $relativeFile"
    }
}

Write-Host '[3/4] Building backend...'
foreach ($relativeProject in @($config.verifyDotnetBuildProjects)) {
    if ([string]::IsNullOrWhiteSpace($relativeProject)) {
        continue
    }

    $projectPath = Resolve-FullPath $scriptDir $relativeProject
    if (-not (Test-IsChildPath $repoRoot $projectPath)) {
        throw "Build project escapes repository root: $relativeProject"
    }

    & dotnet build $projectPath --no-restore -nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Verification failed during dotnet build: $relativeProject"
    }
}

Write-Host '[4/4] Building frontend...'
$frontendDirectories = @($config.verifyFrontendWorkingDirectories)
$frontendCommands = @($config.verifyFrontendBuildCommands)

foreach ($relativeDirectory in $frontendDirectories) {
    if ([string]::IsNullOrWhiteSpace($relativeDirectory)) {
        continue
    }

    $workingDirectory = Resolve-FullPath $scriptDir $relativeDirectory
    if (-not (Test-IsChildPath $repoRoot $workingDirectory)) {
        throw "Frontend working directory escapes repository root: $relativeDirectory"
    }

    foreach ($command in $frontendCommands) {
        if ([string]::IsNullOrWhiteSpace($command)) {
            continue
        }

        Push-Location $workingDirectory
        try {
            Invoke-PowerShellCommand $command
            if ($LASTEXITCODE -ne 0) {
                throw "Verification failed during frontend command '$command' in '$relativeDirectory'."
            }
        }
        finally {
            Pop-Location
        }
    }
}

Write-Host 'Code generation verification completed successfully.'
""";
    }

    private static string BuildBatchShim(string scriptFileName)
    {
        return $"""
@echo off
setlocal

where pwsh >nul 2>nul
if %errorlevel%==0 (
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0{scriptFileName}" %*
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0{scriptFileName}" %*
)

exit /b %errorlevel%
""";
    }

    private static string BuildReadme(CodeGenerationScaffoldModel scaffoldModel, IReadOnlyList<string> unresolvedItems)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Code Generation");
        builder.AppendLine();
        builder.AppendLine("This folder was scaffolded by `brightertools-codegenerator init`.");
        builder.AppendLine();
        builder.AppendLine("## Run");
        builder.AppendLine();
        builder.AppendLine("```powershell");
        builder.AppendLine("pwsh ./CodeGeneration/GenerateCode.ps1");
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("Windows convenience wrapper:");
        builder.AppendLine();
        builder.AppendLine("```powershell");
        builder.AppendLine("CodeGeneration\\GenerateCode.bat");
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("Direct tool invocation:");
        builder.AppendLine();
        builder.AppendLine("```powershell");
        builder.AppendLine("dotnet tool run brightertools-codegenerator -- generate --config CodeGeneration/codegen.json");
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## Verify");
        builder.AppendLine();
        builder.AppendLine("```powershell");
        builder.AppendLine("pwsh ./CodeGeneration/VerifyCodeGeneration.ps1");
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## Notes");
        builder.AppendLine();
        builder.AppendLine("- Relative paths inside `codegen.json` resolve from this `CodeGeneration` folder.");
        builder.AppendLine("- `rootDirectory` points back to the repo root using a relative path.");
        builder.AppendLine("- `projectPath` and `templatesDirectory` are intentionally blank for tool-based usage.");
        builder.AppendLine("- The Windows `.bat` wrappers prefer `pwsh` and fall back to Windows PowerShell if `pwsh` is not installed.");
        builder.AppendLine("- If the repo does not yet have a local tool manifest, run `dotnet new tool-manifest` and `dotnet tool install BrighterTools.CodeGenerator` from the repo root.");

        if (unresolvedItems.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Manual Follow-Up");
            builder.AppendLine();
            foreach (var item in unresolvedItems)
            {
                builder.AppendLine($"- {item}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Current Defaults");
        builder.AppendLine();
        builder.AppendLine($"- `toolName`: `{scaffoldModel.ToolName}`");
        builder.AppendLine($"- `toolCommand`: `{scaffoldModel.ToolCommand}`");

        return builder.ToString();
    }

    private static string ToConfigRelativePath(string configDirectoryPath, string fullPath)
    {
        var relativePath = Path.GetRelativePath(configDirectoryPath, fullPath);
        return relativePath.Replace('\\', '/');
    }

    private static string SanitizeName(string name)
    {
        var parts = name
            .Split(['-', '_', '.', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..])
            .ToArray();

        return parts.Length == 0 ? "App" : string.Concat(parts);
    }

    private sealed class CodeGenerationScaffoldModel
    {
        public required string RepoName { get; init; }
        public required string ToolName { get; init; }
        public required string ToolVersion { get; init; }
        public required string RootDirectory { get; init; }
        public required string ProjectPath { get; init; }
        public required string AppProjectPath { get; init; }
        public required string AppDirectory { get; init; }
        public required string TemplatesDirectory { get; init; }
        public required string ToolCommand { get; init; }
        public required string ControllerGeneratedDirectory { get; init; }
        public required string ControllerStubDirectory { get; init; }
        public required string TypeScriptModelsOutputPath { get; init; }
        public required string TypeScriptEnumsOutputPath { get; init; }
        public required string TypeScriptServiceScaffoldsOutputDirectory { get; init; }
        public required IReadOnlyList<string> CleanupDirectories { get; init; }
        public required IReadOnlyList<string> CleanupFilePatterns { get; init; }
        public required IReadOnlyList<string> VerifyRequiredFiles { get; init; }
        public required IReadOnlyList<string> VerifyDotnetBuildProjects { get; init; }
        public required IReadOnlyList<string> VerifyFrontendWorkingDirectories { get; init; }
        public required IReadOnlyList<string> VerifyFrontendBuildCommands { get; init; }
        public required bool VerifySkipBuildLockCheck { get; init; }
    }
}
