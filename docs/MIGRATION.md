# Migration Guide

## Overview
This document guides users migrating from the PowerShell version of GistGet to the new C# version.

## Compatibility
The C# version is designed to be fully compatible with the `packages.yaml` format used by the PowerShell version. You do not need to modify your existing Gist files.

## Migration Steps

1.  **Install the new GistGet**
    (Installation instructions to be added)

2.  **Authenticate**
    Run `gistget auth login` to authenticate with your GitHub account. This replaces the Personal Access Token (PAT) method used in the PowerShell version.

3.  **Verify Configuration**
    Run `gistget sync --dry-run` (if available) or check your Gist content.

4.  **Uninstall PowerShell Version**
    Remove the PowerShell module or alias if you no longer need it to avoid command conflicts.

## Key Changes
-   **Performance**: The C# version is significantly faster and more robust.
-   **Authentication**: Uses secure Device Flow instead of raw PATs.
-   **Output**: Improved output formatting and progress indication.
-   **Error Handling**: Better error reporting and resilience.

## Troubleshooting
If you encounter issues during migration, please check `KNOWN_ISSUES.md` or report an issue on GitHub.
