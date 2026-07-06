# BrighterTools.CodeGenerator

Code generation tooling for BrighterTools projects, packaged as a dotnet tool.

## Projects
- `BrighterTools.CodeGenerator` (console tool)
- `BrighterTools.CodeGenerator.Tests` (xUnit tests)

## Development
- Build: `dotnet build BrighterTools.CodeGenerator.slnx -c Release`
- Test: `dotnet test BrighterTools.CodeGenerator.slnx -c Release`
- Pack locally: `PackageToolForNuGet.bat`

## NuGet Sources
- The repo-level `NuGet.config` clears inherited package sources and restores from `nuget.org`.
- This avoids machine-specific feeds such as Telerik affecting restore and pack.

## Tool Install
- Create a manifest in the consuming repo: `dotnet new tool-manifest`
- Install the tool: `dotnet tool install BrighterTools.CodeGenerator`
- Run it with app-owned config: `dotnet tool run brightertools-codegenerator -- --config CodeGeneration\codegen.json`

## CI Packaging
- GitHub Actions validates restore, build, test, and pack on every push and pull request.
- The packaged `.nupkg` and `.snupkg` files are uploaded as workflow artifacts.
- The `publish-tool` workflow is configured for Trusted Publishing with GitHub OIDC, not a stored NuGet API key.

## Trusted Publishing Setup
- You must configure Trusted Publishing in `nuget.org` for this GitHub repository before the publish workflow can push packages.
- The workflow already includes the GitHub OIDC permission it needs: `id-token: write`.
- No `NUGET_API_KEY` repository secret is required for the GitHub publishing workflow once Trusted Publishing is configured.

Full setup and consuming-app guidance lives in `usage.md`.
