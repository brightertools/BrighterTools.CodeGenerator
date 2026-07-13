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
