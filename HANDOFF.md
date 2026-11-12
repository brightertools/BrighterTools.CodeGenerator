# Generator release handoff — 12 November 2026

Prepared package version: 2.0.6. Pins System.Security.Cryptography.Xml and Pkcs to 10.0.13, retaining the existing MSBuild/runtime loading design and generator templates. Default/scaffolded toolVersion now matches 2.0.6; explicitly configured toolVersion values remain unchanged. No generated app DTOs are edited by hand.

Release validation includes unit tests, transitive audit, NuGet pack and installation/execution of the actual packed tool. Publishing uses existing publish-tool.yml and NuGet trusted publishing. Consumer regeneration should use its existing configuration and review the generated diff. This release does not regenerate or redeploy unrelated applications.

Validation and publication evidence follow below.

## Local release validation

12 November 2026: Release tests 25 passed, 0 skipped; transitive restore audit (NU1901–NU1904 as errors) and NuGet pack passed. `pwsh -NoProfile -File tests/Test-PackedTool.ps1 -PackageDirectory <packed-artifacts> -DotNet <dotnet>` installed the actual 2.0.6 package in an isolated tool/cache directory, generated twice deterministically and preserved SQL Server defaults.

Command: `dotnet test BrighterTools.CodeGenerator.sln -c Release -m:1 -p:NuGetAuditMode=all -warnaserror:NU1901,NU1902,NU1903,NU1904`. CI repeats package installation and generation before publication.
