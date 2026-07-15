# BrighterTools.CodeGenerator Usage

This guide is for developers integrating `BrighterTools.CodeGenerator` into an application repo. For the high-level overview and package link, start with [README.md](./README.md).

Install the tool as a NuGet local tool in the consuming repo before wiring up code generation.

## Commands

Primary commands:

```text
brightertools-codegenerator generate --config CodeGeneration/codegen.json
brightertools-codegenerator init
```

Backward-compatible command:

```text
brightertools-codegenerator --config CodeGeneration/codegen.json
```

`generate` supports:

- `--config <path>`
- `--dry-run`

`init` supports:

- `--repo-root <path>`
- `--config-dir <path>`
- `--force`

## Path Rules

- Relative `--config` paths resolve from the current working directory.
- Relative paths inside `codegen.json` resolve from the folder containing that config file.
- Starter configs use forward-slash relative paths so the same config works on Windows and Unix-style systems.
- Generated output paths are normalized back to repo-root-relative paths before files are written.

## Init Workflow

Run `init` from the consuming repo root:

```text
dotnet tool run brightertools-codegenerator -- init
```

By default this creates:

- `CodeGeneration/codegen.json`
- `CodeGeneration/GenerateCode.ps1`
- `CodeGeneration/DeleteGeneratedCode.ps1`
- `CodeGeneration/VerifyCodeGeneration.ps1`
- `CodeGeneration/GenerateCode.bat`
- `CodeGeneration/DeleteGeneratedCode.bat`
- `CodeGeneration/VerifyCodeGeneration.bat`
- `CodeGeneration/README.md`

Detection rules:

- app project: prefer `App/App.csproj`, otherwise first `App/*.csproj`
- backend web project: prefer `Web/Web.Server/Web.Server.csproj`
- frontend directory: prefer `Web/web.client`
- controller directories: prefer `Web/Web.Server/Controllers/V1`, otherwise `Web/Web.Server/Controllers`

If a path cannot be discovered, the scaffold still completes and the generated `README.md` calls out the missing values to edit.

## Tool-Based Repo Setup

Inside the consuming repo:

```text
dotnet new tool-manifest
dotnet tool install BrighterTools.CodeGenerator
dotnet tool run brightertools-codegenerator -- init
```

Recommended foundational data packages for generated repository and service patterns:

```text
dotnet add package BrighterTools.Data.Abstractions
dotnet add package BrighterTools.Data.EFCore
```

These packages provide shared contracts such as `IEntity<TKey>`, `ListRequest`, and `ServiceResult<T>`, plus EF Core repository base classes such as `BaseRepository<TEntity,TKey,TUserId>`. They are consuming-app dependencies, not runtime dependencies of the generator itself.

Then run generation with either:

```text
pwsh ./CodeGeneration/GenerateCode.ps1
```

or:

```text
dotnet tool run brightertools-codegenerator -- generate --config CodeGeneration/codegen.json
```

Windows convenience wrappers remain available:

```text
CodeGeneration\GenerateCode.bat
CodeGeneration\VerifyCodeGeneration.bat
```

The generated Windows `.bat` wrappers prefer `pwsh` and fall back to Windows PowerShell if `pwsh` is not installed.

## Config Shape

Core fields:

```json
{
  "toolName": "MyApp.CodeGeneration",
  "toolVersion": "2.0.4",
  "rootDirectory": "..",
  "projectPath": "",
  "appProjectPath": "../App/App.csproj",
  "appDirectory": "../App",
  "templatesDirectory": "",
  "toolCommand": "brightertools-codegenerator"
}
```

Key behavior fields:

- `controllerGeneratedDirectory`
- `controllerStubDirectory`
- `typeScriptModelsOutputPath`
- `typeScriptEnumsOutputPath`
- `typeScriptServiceScaffoldsOutputDirectory`

Repo workflow fields:

- `cleanupDirectories`
- `cleanupFilePatterns`
- `verifyRequiredFiles`
- `verifyDotnetBuildProjects`
- `verifyFrontendWorkingDirectories`
- `verifyFrontendBuildCommands`
- `verifySkipBuildLockCheck`

Rules for workflow fields:

- the path lists accept relative paths only
- those paths resolve from the `codegen.json` folder
- commands such as `npm run build` stay as plain command strings

## Generated Headers And Run History

- Generated files use deterministic comment headers without a per-run timestamp.
- Generated headers include tool identity so output stays traceable and stable in git.
- Successful non-dry-run config-based generation appends a run-history entry to `CodeGeneration/generation-history.jsonl`.
- The run-history file is written by the shared tool, so direct `generate --config ...` usage and wrapper-script usage behave the same way.

## Generate Script Behavior

`GenerateCode.ps1`:

1. loads the sibling `codegen.json`
2. resolves the repo root from `rootDirectory`
3. restores local dotnet tools from the repo root
4. runs `dotnet tool run <toolCommand> -- generate --config <configPath>`

`DeleteGeneratedCode.ps1`:

1. loads cleanup directories and file patterns from config
2. resolves them relative to the config folder
3. deletes matching generated files only inside the repo root

`VerifyCodeGeneration.ps1`:

1. runs cleanup
2. runs generation
3. checks configured required generated files
4. builds configured backend projects
5. runs configured frontend build commands

Verification skips the optional Windows build-lock process inspection by default. Set `verifySkipBuildLockCheck` to `false` only if you intentionally want that extra Windows-only guard.

## Direct Project Usage

Tool-based execution is the recommended default.

If you are actively developing the generator itself, a consuming repo can still set:

- `projectPath`
- `templatesDirectory`

and run the generator project directly from its own wrapper script. That remains supported for generator development, while the tool-based workflow above is the standard integration path.

For local packaging and NuGet publishing of this repo, see [publishing.md](./publishing.md).
