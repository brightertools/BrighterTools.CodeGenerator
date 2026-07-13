# v2.0.3 - Documentation Refresh and Publishing Follow-Up

## Summary

Patch release that tightens the user-facing documentation and aligns publish metadata for the next NuGet release.

## Included

- Updated README and usage guidance to describe the current workflow directly.
- Kept scaffolded defaults, workflow examples, and package metadata aligned with the current release.

## Breaking Changes

- None.

# v2.0.2 - Deterministic Headers and Run History

## Summary

Follow-up release that makes generated output more stable in git and records successful generation runs in a repo-owned history file.

## Included

- Removed per-run timestamps from generated file headers.
- Added deterministic generator version markers in generated headers.
- Added `CodeGeneration/generation-history.jsonl` logging for successful config-based runs.

## Breaking Changes

- Generated file headers no longer include the old run-date line.

# v2.0.1 - Documentation and Publishing Follow-Up

## Summary

Follow-up release to tighten the documentation split and clarify the NuGet publishing workflow after the v2 rollout.

## Included

- Split documentation into audience-focused guides.
- Added a dedicated maintainer publishing guide.
- Clarified the GitHub Actions publishing path for NuGet.

## Breaking Changes

- None.

# v2.0.0 - Cross-Platform Code Generation

## Summary

Major release that moves the code generation workflow to a reusable cross-platform tool plus repo-scaffolded wrappers.

## Included

- Added explicit `generate` and `init` CLI commands.
- Added convention-based `init` scaffolding for `CodeGeneration` starter files.
- Standardized config-relative path resolution from the `codegen.json` folder.
- Added config-driven cleanup and verification settings for consuming repos.
- Added cross-platform PowerShell wrapper scripts with Windows `.bat` shims.
- Expanded CI coverage across Windows, Ubuntu, and macOS.

## Breaking Changes

- Consuming repos should move to the scaffolded `CodeGeneration` wrapper workflow for the supported cross-platform path.
- Newly scaffolded configs default to the v2 conventions for relative paths and verification metadata.
