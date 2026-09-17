# Release 2.0.5 — 2026-09-17

This release updates the existing library; SQL Server defaults and host-owned
persistence registration remain unchanged. The proposed production SQLite
expansion is cancelled. Existing SQLite samples/tests do not establish a new
production-support commitment.

## Changes

- Update compatible stable dependencies and .NET 10 servicing packages.
- Preserve existing BrighterTools integrations except where explicitly noted below.
- Validate Release builds and packages before publication.

## Compatibility

- Remain on net10.0 with stable SDK selection starting at 10.0.401.
- Keep MSBuild packages at 18.9.6; 18.10.1 targets .NET 11.
- Preserve MSBuild Locator runtime-loading exclusions and command-line contracts.
- Keep generated SQL Server configuration and templates unchanged.
- Smoke-test the installed nupkg with real project loading and repeat generation.

## Publication

Use `.github/workflows/publish-tool.yml` on the release commit. The workflow
validates before publishing and uses the production environment with registry
trusted publishing. Registry policies must authorize this repository and workflow;
a locally built package is not proof of successful publication.
