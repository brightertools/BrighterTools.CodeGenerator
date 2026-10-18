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

Build/test this repository with stable .NET SDK 10.0.401 or a newer stable .NET 10
feature band (selected by global.json). CI audits direct and transitive dependencies
and fails on vulnerability warnings. MSBuild stays at 18.9.6 because 18.10 targets
.NET 11; this is intentional, not an overlooked security update.

After packing, validate the actual tool artifact with
`pwsh ./tests/Test-PackedTool.ps1 -PackageDirectory ./artifacts/nuget`.
It uses an isolated local feed/cache and temporary fixture, checks SQL Server defaults
and repeatable generation, and never writes generated output into a consuming app.

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

## Host context and identifier compatibility

`dbContextName` defaults to `ApplicationDbContext`; it controls context template class/file names and repository/registration references. `registerDbContext` defaults to `true`; set it to `false` when the host already registers its EF context. This does not disable repositories/services registration. Omit the `dbcontext` generator when the host owns its mappings and migrations.

Scalar repository/service/request key metadata preserves `int`, `long`, `string` and `Guid`. Unsupported scalar types are rejected rather than converted to `int`. String/GUID update and upsert templates avoid numeric nullable-key operations. This is template compatibility, not a claim that a consumer's base repository supports every key type: consumer base infrastructure must implement the generated contract, including correct assigned-key creation semantics.

Persistence preflight rejects non-join models without an `Id` property. Composite keys require an explicit supported persistence contract; do not add artificial integer IDs to satisfy generation. Rendering is completed before files are written, so this diagnostic does not leave partial output.

### Scoped identity reads

Opt-in `persistenceMode: "scoped-read"` generates read repositories/services against `BrighterTools.Data.EFCore.Repositories.ScopedReadRepository<TEntity,TKey>`. `persistenceKeys` maps model names to ordered key property names, e.g. `{ "TenantUser": ["TenantId", "UserId"] }`; otherwise `Id` is used. Generated key records are code contracts, not mapped database entities. Register an `IReadScope<TEntity>` for every generated model; the host owns eligibility and tenant authorization. Domain commands remain explicit custom operations and must preserve their unit-of-work boundary. Do not enable CRUD DTO/controller generators for security-owned models.

Use `--check` to detect missing or stale generated output without writing files or generation history. Existing custom partials are preserved. Compile and test generated output separately; a successful check does not establish correct authorization or query translation.
