# BrighterTools.CodeGenerator Publishing

This guide is for maintainers packaging and publishing `BrighterTools.CodeGenerator` to NuGet.

Package page:

- [BrighterTools.CodeGenerator](https://www.nuget.org/packages/BrighterTools.CodeGenerator)

## Prerequisites

- A valid Trusted Publishing policy is active on `nuget.org` for this repository.
- The GitHub repository, workflow name, and environment match the `nuget.org` policy.
- The publish workflow uses GitHub OIDC through `NuGet/login@v1`.

## Local Packaging

Convenience script:

```text
PackageToolForNuGet.bat
```

Equivalent CLI flow:

```text
dotnet restore ./BrighterTools.CodeGenerator.slnx --configfile ./NuGet.config
dotnet build ./BrighterTools.CodeGenerator.slnx -c Release --no-restore
dotnet pack ./BrighterTools.CodeGenerator.csproj -c Release --no-build --output ./artifacts/nuget --configfile ./NuGet.config
```

Expected artifacts:

- `artifacts/nuget/BrighterTools.CodeGenerator.<version>.nupkg`
- `artifacts/nuget/BrighterTools.CodeGenerator.<version>.snupkg`

## Versioning

- Package version is set in `Directory.Build.props` through `VersionPrefix`.
- The generator also carries a default `toolVersion` used in scaffolded config and fallback metadata.
- Major versions are appropriate for workflow or CLI changes that consuming repos should treat as a new generation pattern.
- Minor versions are appropriate for backward-compatible feature additions.
- Patch versions are appropriate for fixes that do not change the expected consuming-repo workflow.

Keep the package version and scaffolded default `toolVersion` aligned for new releases unless there is an intentional compatibility reason not to.

## GitHub Actions Publishing

Publishing is handled by `.github/workflows/publish-tool.yml`.

Workflow inputs:

- `publish_to_nuget`
- `version` as an optional override

Trusted Publishing flow:

- GitHub Actions requests an OIDC token
- `NuGet/login@v1` exchanges it for a short-lived NuGet API key
- `dotnet nuget push` publishes the generated package to `nuget.org`

No long-lived NuGet API key secret is required for this workflow.

## NuGet Config Notes

- The repo-level `NuGet.config` clears inherited sources and restores from `nuget.org`.
- This prevents machine-specific or legacy feeds from affecting restore and pack.
- Keep this repo-local config in place for local packaging and CI consistency.

## Release Checklist

1. Update version metadata in `Directory.Build.props`.
2. Confirm the default scaffolded `toolVersion` still matches the intended release.
3. Run:
   - `dotnet test ./BrighterTools.CodeGenerator.slnx -c Release`
4. Pack locally and confirm both `.nupkg` and `.snupkg` are produced.
5. Commit and push the release changes.
6. Tag the release if you are using repository tags for release tracking.
7. Run `publish-tool.yml` with `publish_to_nuget = true`.
8. Verify the package appears on [nuget.org](https://www.nuget.org/packages/BrighterTools.CodeGenerator).

## Related Docs

- [README.md](./README.md) for overview and quick start
- [usage.md](./usage.md) for consuming-repo integration guidance
- [RELEASE_NOTES.md](./RELEASE_NOTES.md) for release history
