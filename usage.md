# BrighterTools.CodeGenerator Usage

## Purpose

`BrighterTools.CodeGenerator` is a shared executable code generator for .NET application repos.

It currently generates:

- base repositories and generated repository implementations
- generated services and custom service stubs
- request/response DTOs
- split `ApplicationDbContext` partials
- generated DI registration for repositories and services
- safe controller stubs plus generated controller scaffold references with commented example endpoints
- TypeScript API model bundles from generated C# API DTOs
- TypeScript enum bundles from configured enum namespaces
- TypeScript service scaffold bundles with commented endpoint examples

The intended integration model is:

- keep generator source in a shared repo
- keep app-specific paths and scripts inside each consuming app repo

## Current execution model

The generator entrypoint supports:

- `--config <path>`
- `--dry-run`

The config file belongs to the consuming app, not to this repo.

Example:

```text
Skilledly\CodeGeneration\codegen.json
```

## Recommended consuming-app setup

Inside the app repo, add a `CodeGeneration` folder containing:

- `codegen.json`
- `GenerateCode.bat`
- `DeleteGeneratedCode.bat`
- optional `README.md`

This keeps the app in control of:

- where generated files go
- which app project is inspected
- which generator project/tool is used
- how generation is run in local dev and CI

## Example app config

Example `codegen.json` for direct project execution:

```json
{
  "toolName": "BrighterTools",
  "toolVersion": "7.0.0",
  "rootDirectory": "..",
  "projectPath": "..\\..\\BrighterTools\\BrighterTools.CodeGenerator\\BrighterTools.CodeGenerator.csproj",
  "appProjectPath": "..\\App\\App.csproj",
  "appDirectory": "..\\App",
  "templatesDirectory": "..\\..\\BrighterTools\\BrighterTools.CodeGenerator\\Templates",
  "toolCommand": "brightertools-codegenerator",
  "typeScriptModelNamespacePrefixes": [
    "App.Api.Models"
  ],
  "typeScriptModelsGeneratedOnly": true,
  "typeScriptModelsOutputPath": "..\\reactapp\\src\\types\\generated\\api-models.g.ts",
  "typeScriptEnumsOutputPath": "..\\reactapp\\src\\types\\generated\\app-enums.g.ts",
  "typeScriptServiceScaffoldsOutputDirectory": "..\\reactapp\\src\\services\\generated",
  "typeScriptCoreTypesImportPath": "../../types/core-app-types",
  "typeScriptGeneratedModelsImportPath": "../../types/generated/api-models.g",
  "typeScriptHttpRequestImportPath": "../httpRequest"
}
```

Meaning:

- `rootDirectory`
  - the consuming app repo root
- `projectPath`
  - path to this generator project when using direct project execution
- `appProjectPath`
  - app project to inspect for models and enums
- `appDirectory`
  - root app folder where generated files are written
- `templatesDirectory`
  - template folder to load when using direct project execution
- `toolCommand`
  - reserved for future dotnet tool execution
- `enumNamespacePrefixes`
  - optional namespace prefixes to scan when building the TypeScript enum bundle
- `typeScriptModelNamespacePrefixes`
  - namespace prefixes to scan when building the TypeScript API model bundle
- `typeScriptModelsGeneratedOnly`
  - when `true`, only classes marked as generated or coming from `.g.cs` files are included in the TypeScript API model bundle
- `typeScriptModelsOutputPath`
  - output path for the generated TypeScript API model bundle, relative to the app repo root or config file
- `typeScriptEnumsOutputPath`
  - output path for the generated TypeScript enum bundle, relative to the app repo root or config file
- `typeScriptServiceScaffoldsOutputDirectory`
  - output directory for generated TypeScript service scaffolds, relative to the app repo root or config file
- `typeScriptCoreTypesImportPath`
  - import path written into scaffold comments for app-specific core request/response types
- `typeScriptGeneratedModelsImportPath`
  - import path written into scaffold comments for generated TS DTOs
- `typeScriptHttpRequestImportPath`
  - import path written into scaffold comments for the shared HTTP helper

## Recommended app-side scripts

### GenerateCode.bat

Recommended behavior:

1. load `codegen.json`
2. if `projectPath` exists:
   - `dotnet build <projectPath> --no-restore`
   - `dotnet run --project <projectPath> --no-build -- --config <configPath>`
3. otherwise, if `toolCommand` is present:
   - `dotnet tool run <toolCommand> -- --config <configPath>`

### DeleteGeneratedCode.bat

Recommended behavior:

- delete generated `*.g.*` files under the app directory
- delete untouched `!!!CODE_GEN_REPLACE!!!` placeholder files
- skip `bin` and `obj`

## Typical generated output in the app repo

For a standard app layout, this generator writes to areas such as:

- `App/Data/Repositories/Generated`
- `App/Services/Generated`
- `App/Dto/*/Requests`
- `App/Dto/*/Responses`
- `App/Data/Generated`
- `Web.Server/Controllers`
- `reactapp/src/types/generated/api-models.g.ts`
- `reactapp/src/types/generated/app-enums.g.ts`

## Current architecture expectations

The current template set assumes conventions similar to:

- `App/App.csproj` as the app project
- domain models under the app project
- generated repository base support in `App/Data/Repositories/BaseRepository.cs`
- generated DI extension registration
- split `ApplicationDbContext`
- custom/non-generated partial stubs marked with `!!!CODE_GEN_REPLACE!!!`

## Direct project reference now

This is the recommended setup while iterating on the generator:

- point the consuming app's `projectPath` at this `.csproj`
- point `templatesDirectory` at this repo's `Templates` folder
- run generation through the consuming app's own `CodeGeneration\GenerateCode.bat`

This keeps the app repo stable while allowing rapid changes in the shared generator repo.

## Future packaging as a dotnet tool

The clean long-term distribution is a dotnet tool package.

When moving to that model in a consuming app:

1. package this generator as a dotnet tool
2. install it in the app repo's tool manifest or globally
3. clear `projectPath` in `codegen.json`
4. clear `templatesDirectory` in `codegen.json`
5. set `toolCommand` to the installed tool command name

Then the same app script can run:

```text
dotnet tool run <toolCommand> -- --config CodeGeneration\codegen.json
```

This avoids hard-wiring the app repo to a local checkout of the generator source.

## Important operational rule

The generator can still fall back to older same-repo assumptions if it is run directly without `--config`.

For consuming apps, the safe rule is:

- always run the app-owned generation script
- let the app pass `--config`

## Suggested app workflow

1. update or inspect `CodeGeneration\codegen.json`
2. run `CodeGeneration\DeleteGeneratedCode.bat` if a clean regeneration is needed
3. run `CodeGeneration\GenerateCode.bat`
4. build the app projects
5. review generated/custom split changes

## What this generator currently supports well

- generated repository and service surfaces driven by model conventions
- shared list/filter/query patterns
- split generated/custom EF setup
- app-owned scripts and configuration
- shared TypeScript model and enum bundle generation for generated API DTOs
- external shared generator repo now, dotnet tool later

## Controller generation modes

For controllers, the recommended mode for new apps is:

- `controller-stubs`
- `controller-scaffolds`

In that mode:

- the real controller lives in the app repo and is never generated as a base class
- the real controller is an explicit, non-derived controller stub marked with `!!!CODE_GEN_REPLACE!!!`
- the scaffold file in `Generated/` contains commented endpoint examples to copy into the real controller

`generated-controllers` should now be treated as a legacy migration mode only.
