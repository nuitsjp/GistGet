# GistGet

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub issues](https://img.shields.io/github/issues/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/pulls)

[Japanese](README.ja.md)

**GistGet** is a CLI tool designed to synchronize Windows Package Manager (`winget`) packages across multiple devices using GitHub Gist.
It uses a simple YAML configuration file stored in a private or public Gist to keep your installed applications and tools consistent.

## Features

-   **☁️ Cloud Sync**: Synchronize installed packages via GitHub Gist.
-   **🚀 Full Winget Compatibility**: Use standard `winget` commands directly, with integrated cloud sync capabilities (e.g., `gistget search`, `gistget install`).
-   **💻 Cross-Device**: Keep your work and home computers in sync.
-   **📄 Configuration as Code**: Manage your software list in a readable `GistGet.yaml` format.

## Requirements

-   Windows 10/11
-   Windows Package Manager (`winget`)

## Installation

### From GitHub Releases

1.  Download the latest release from the [Releases page](https://github.com/nuitsjp/GistGet/releases).
2.  Extract the zip file.
3.  Add the extracted folder to your system's `PATH`.

### From Winget (Coming Soon)

```powershell
winget install nuitsjp.GistGet
```

After installation, you can launch it with:

```powershell
gistget --help
```

## Usage

### Authentication

First, log in to your GitHub account to enable Gist access.

```powershell
gistget auth login
```

Follow the on-screen instructions to authenticate using the Device Flow.

### Initial Setup (init)

When setting up a new PC or starting cloud sync for an existing environment, first use the `init` command to select the packages to sync:

```powershell
gistget init
```

This will display a list of locally installed packages, allowing you to interactively select which ones to sync to the cloud. Once selection is complete, `GistGet.yaml` will be created (or overwritten) in the Gist.

### Sync

To sync local packages with the Gist:

```powershell
gistget sync
```

This performs the following operations:
1.  Fetches `GistGet.yaml` from the Gist.
2.  Compares it with locally installed packages.
3.  Installs missing packages and uninstalls packages marked for removal.

To sync from an external YAML file:

```powershell
gistget sync --url https://gist.githubusercontent.com/user/id/raw/GistGet.yaml
```

### Winget Compatible Commands

GistGet fully supports `winget`'s command system. You can manage packages using familiar commands while benefiting from cloud sync.

```powershell
gistget search vscode
gistget show Microsoft.PowerToys
```

## Configuration

GistGet uses a `GistGet.yaml` file in the Gist. It's a map where the package ID is the key, and the value contains installation options and sync flags.

```yaml
<PackageId>:
  name: <string>                   # winget display name (auto-set)
  pin: <string>                   # Pinned version (omit for no pin)
  pinType: <pinning | blocking | gating>  # Pin type (default: pinning)
  uninstall: <boolean>            # true to mark for uninstallation
  # Installation options (winget passthrough)
  scope: <user | machine>
  architecture: <x86 | x64 | arm | arm64>
  installerType: <string>
  interactive: <boolean>
  silent: <boolean>
  locale: <string>
  location: <string>
  log: <string>
  custom: <string>
  override: <string>
  force: <boolean>
  acceptPackageAgreements: <boolean>
  acceptSourceAgreements: <boolean>
  allowHashMismatch: <boolean>
  skipDependencies: <boolean>
  header: <string>
```

### Core Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `name` | string | Package name displayed by winget. Auto-set by `install` / `upgrade` / `uninstall` / `pin add` / `init`. |
| `pin` | string | Version to pin. Omit for no pin (always latest). Wildcards `*` supported (e.g., `1.7.*`). |
| `pinType` | enum | Pin type. Only effective when `pin` is specified. Default: `pinning`. |
| `uninstall` | boolean | If `true`, will be uninstalled during sync. |

### Pin Types

| Value | Description | `upgrade --all` | `upgrade <pkg>` |
|-------|-------------|-----------------|-----------------|
| None | No pin. Eligible for all upgrades. | ✅ Allowed | ✅ Allowed |
| `pinning` | Default. Excluded from `upgrade --all`, but explicit upgrade is possible. | ❌ Skipped | ✅ Allowed |
| `blocking` | Excluded from `upgrade --all`. Explicit upgrade is also possible. | ❌ Skipped | ✅ Allowed |
| `gating` | Upgrade only within specified version range (e.g., `1.7.*`). | Within range only | Within range only |

### Installation Options (winget passthrough)

| Parameter | winget Option | Description |
|-----------|---------------|-------------|
| `scope` | `--scope` | `user` or `machine` |
| `architecture` | `--architecture` | `x86`, `x64`, `arm`, `arm64` |
| `installerType` | `--installer-type` | Installer type |
| `interactive` | `--interactive` | Interactive installation |
| `silent` | `--silent` | Silent installation |
| `locale` | `--locale` | Locale (BCP47 format) |
| `location` | `--location` | Installation path |
| `log` | `--log` | Log file path |
| `custom` | `--custom` | Additional installer arguments |
| `override` | `--override` | Override installer arguments |
| `force` | `--force` | Force execution |
| `acceptPackageAgreements` | `--accept-package-agreements` | Accept package agreements |
| `acceptSourceAgreements` | `--accept-source-agreements` | Accept source agreements |
| `allowHashMismatch` | `--ignore-security-hash` | Ignore hash mismatch |
| `skipDependencies` | `--skip-dependencies` | Skip dependencies |
| `header` | `--header` | Custom HTTP header |

### Configuration Examples

```yaml
# Install latest version, upgradeable (no pin)
Microsoft.VisualStudioCode:
  name: Visual Studio Code
  scope: user
  silent: true
  override: /VERYSILENT /MERGETASKS=!runcode

# Pin to version 23.01 (excluded from upgrade --all)
7zip.7zip:
  name: 7-Zip
  pin: "23.01"
  architecture: x64

# Restrict to version 1.7.x range (gating)
jqlang.jq:
  name: jq
  pin: "1.7.*"
  pinType: gating

# Mark for uninstallation
DeepL.DeepL:
  name: DeepL
  uninstall: true
```


## For Developers

This section provides information for developers contributing to the GistGet project.

### Development Environment

- **OS**: Windows 10/11 (Windows 10.0.26100.0 or later)
- **.NET SDK**: .NET 10.0 or later
- **Windows SDK**: 10.0.26100.0 or later (including UAP Platform)
- **IDE**: Visual Studio 2022 or Visual Studio Code (recommended)
- **Windows Package Manager**: winget (via Windows App Installer)
- **PowerShell**: 5.1 or later (for script execution)

### References

- Implementation samples: `external/winget-cli/samples/WinGetClientSample/`
- GitHub: [microsoft/winget-cli](https://github.com/microsoft/winget-cli)

### Development Scripts

> [!IMPORTANT]
> Integration tests use the actual GitHub API. Be sure to complete authentication with `Run-AuthLogin.ps1` before running tests.

#### 1. Run-AuthLogin.ps1 (First run / Auth expiration)

Script to execute GitHub authentication and save credentials to Windows Credential Manager:

```powershell
.\scripts\Run-AuthLogin.ps1
```

Credentials are persisted, so this only needs to be run on first use or when authentication expires.

#### 2. Run-CodeQuality.ps1 (Daily development)

Integrated script to run the code quality pipeline:

```powershell
# Run all steps (default)
# FormatCheck → Build → Tests → ReSharper
.\scripts\Run-CodeQuality.ps1

# Run specific steps only
.\scripts\Run-CodeQuality.ps1 -Build           # Build only
.\scripts\Run-CodeQuality.ps1 -Build -Tests    # Build and Tests only
.\scripts\Run-CodeQuality.ps1 -Tests           # Tests only

# Run with Release build
.\scripts\Run-CodeQuality.ps1 -Configuration Release

# Change coverage threshold
.\scripts\Run-CodeQuality.ps1 -CoverageThreshold 95
```

### Release

Releases are automated via GitHub Actions. When you push a tag, build, upload to GitHub Releases, and PR creation to WinGet are automatically executed.

#### Official Release

```powershell
# 1. Update version in csproj and commit
git add .
git commit -m "chore: bump version to 0.2.0"

# 2. Create and push tag
git tag v0.2.0
git push origin main --tags
```

#### Pre-release

Using pre-release tags (`-alpha`, `-beta`, `-rc`, etc.) will skip PR creation to WinGet:

```powershell
git tag v0.2.0-beta.1
git push origin --tags
```

## License

MIT License
