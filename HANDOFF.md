# Generator release handoff — 12 November 2026

Prepared package version: 2.0.6. Pins System.Security.Cryptography.Xml and Pkcs to 10.0.13, retaining the existing MSBuild/runtime loading design and generator templates. Default/scaffolded toolVersion now matches 2.0.6; explicitly configured toolVersion values remain unchanged. No generated app DTOs are edited by hand.

Release validation includes unit tests, transitive audit, NuGet pack and installation/execution of the actual packed tool. Publishing uses existing publish-tool.yml and NuGet trusted publishing. Consumer regeneration should use its existing configuration and review the generated diff. This release does not regenerate or redeploy unrelated applications.

Validation and publication evidence follow below.

## Local release validation

12 November 2026: Release tests 25 passed, 0 skipped; transitive restore audit (NU1901–NU1904 as errors) and NuGet pack passed. `pwsh -NoProfile -File tests/Test-PackedTool.ps1 -PackageDirectory <packed-artifacts> -DotNet <dotnet>` installed the actual 2.0.6 package in an isolated tool/cache directory, generated twice deterministically and preserved SQL Server defaults.

Command: `dotnet test BrighterTools.CodeGenerator.sln -c Release -m:1 -p:NuGetAuditMode=all -warnaserror:NU1901,NU1902,NU1903,NU1904`. CI repeats package installation and generation before publication.

## Published release

The trusted-publishing workflow completed successfully on 12 November 2026.

- Published source commit: `45d0ecca0a96e8699deab1d95f409e0a98b68187`.
- Workflow: [https://github.com/brightertools/BrighterTools.CodeGenerator/actions/runs/43246329398](https://github.com/brightertools/BrighterTools.CodeGenerator/actions/runs/43246329398).
- CI repeated the required tests, audits and package validation before successful registry uploads.
- Registry availability is checked separately below; workflow success alone is not a download check.

CI package SHA-256 (before any registry repository signing):

- `BrighterTools.CodeGenerator.2.0.6.nupkg`: `4450802d6da6fb27fa34ec373e7e727b7bff3cfe4314463ed9840d3f0d2c98d4`.

For rollback, pin consumers to their prior package versions and review host migrations separately; do not delete published packages or undo additive host schema as part of package rollback. Source-reference consumers use their reviewed sibling revisions, not a substituted registry reference.

Registry availability verified at 2026-11-12T15:29:13.210598+00:00: NuGet 2.0.6 downloaded successfully. All archive contents match CI, excluding the NuGet repository signature. Published package SHA-256: `bf68eab2c753b0a834d591cc021893b4e10bc2755ac911d9cdd0f764e1fa3947`.
