# BrighterTools.CodeGenerator

Code generation tooling for BrighterTools projects, packaged as a dotnet tool.

## Projects
- `BrighterTools.CodeGenerator` (console tool)
- `BrighterTools.CodeGenerator.Tests` (xUnit tests)

## Commands
- Scaffold a consuming repo: `dotnet tool run brightertools-codegenerator -- init`
- Generate from app-owned config: `dotnet tool run brightertools-codegenerator -- generate --config CodeGeneration/codegen.json`
- Legacy-compatible invocation: `dotnet tool run brightertools-codegenerator -- --config CodeGeneration/codegen.json`

## Cross-Platform Workflow
- Relative `--config` paths resolve from the current working directory.
- Relative paths inside `codegen.json` resolve from the folder containing that config file.
- Consuming repos should keep a repo-owned `CodeGeneration` folder with `pwsh` entrypoints and thin Windows `.bat` shims.
- The generated Windows `.bat` shims prefer `pwsh` and fall back to Windows PowerShell when `pwsh` is not installed.
- `init` scaffolds those starter files for convention-based repos and keeps `projectPath` / `templatesDirectory` empty for tool-based usage.

## Development
- Build: `dotnet build BrighterTools.CodeGenerator.slnx -c Release`
- Test: `dotnet test BrighterTools.CodeGenerator.slnx -c Release`
- Pack locally: `PackageToolForNuGet.bat`

## CI Packaging
- GitHub Actions validates restore, build, and test on Windows, Linux, and macOS.
- Packing still runs on Ubuntu and uploads the `.nupkg` and `.snupkg` artifacts.
- The packaged `.nupkg` and `.snupkg` files are uploaded as workflow artifacts.
- The `publish-tool` workflow is configured for Trusted Publishing with GitHub OIDC, not a stored NuGet API key.

## NuGet Sources
- The repo-level `NuGet.config` clears inherited package sources and restores from `nuget.org`.
- This avoids machine-specific feeds such as Telerik affecting restore and pack.

## Trusted Publishing Setup
- You must configure Trusted Publishing in `nuget.org` for this GitHub repository before the publish workflow can push packages.
- The workflow already includes the GitHub OIDC permission it needs: `id-token: write`.
- The workflow uses `NuGet/login@v1` to exchange the GitHub OIDC token for a short-lived NuGet API key during the publish job.
- No long-lived `NUGET_API_KEY` repository secret is required for the GitHub publishing workflow once Trusted Publishing is configured.

Full setup and consuming-app guidance lives in `usage.md`.
