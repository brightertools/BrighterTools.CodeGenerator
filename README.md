# BrighterTools.CodeGenerator

`BrighterTools.CodeGenerator` is a shared .NET tool for BrighterTools-style application repos. It scaffolds and runs a repo-owned `CodeGeneration` workflow so generated code stays controlled by the consuming app, while the generator logic stays shared and versioned independently.

NuGet package:

- [BrighterTools.CodeGenerator](https://www.nuget.org/packages/BrighterTools.CodeGenerator)

## What It Does

- Scaffolds a starter `CodeGeneration` folder with config and wrapper scripts.
- Runs generation from a repo-owned `codegen.json` file.
- Supports Windows, macOS, and Linux through PowerShell entrypoints, with Windows batch shims for convenience.
- Helps consuming repos verify generated output through cleanup, regeneration, and build steps.
- Uses deterministic generated file headers without per-run timestamps.
- Writes successful config-based generation runs to `CodeGeneration/generation-history.jsonl`.
- Aligns generated BrighterTools-style data layer patterns with the foundational `BrighterTools.Data.Abstractions` and `BrighterTools.Data.EFCore` packages.

## Install From NuGet

From the consuming repo root:

```powershell
dotnet new tool-manifest
dotnet tool install BrighterTools.CodeGenerator
dotnet tool run brightertools-codegenerator -- init
pwsh ./CodeGeneration/GenerateCode.ps1
```

## Commands

- `dotnet tool run brightertools-codegenerator -- init`
- `dotnet tool run brightertools-codegenerator -- generate --config CodeGeneration/codegen.json`
- `dotnet tool run brightertools-codegenerator -- --config CodeGeneration/codegen.json`

## Cross-Platform Workflow

- Relative `--config` paths resolve from the current working directory.
- Relative paths inside `codegen.json` resolve from the folder containing that config file.
- Consuming repos own their `CodeGeneration` folder, cleanup rules, verification rules, and wrapper scripts.
- Windows `.bat` wrappers prefer `pwsh` and fall back to Windows PowerShell when `pwsh` is not installed.
- Successful non-dry-run config-based generation writes a run-history entry beside `codegen.json`.

## Data Foundations

Generated repositories and services are intended to pair with the `BrighterTools.Data.Abstractions` and `BrighterTools.Data.EFCore` packages in consuming apps. The generator remains a build-time tool; the app still owns package references, DbContext design, tenancy, audit behavior, and persistence policy.

## More Documentation

- [usage.md](./usage.md) for consuming-repo setup, config, and workflow details
- [publishing.md](./publishing.md) for packaging and NuGet publishing
- [RELEASE_NOTES.md](./RELEASE_NOTES.md) for release history
