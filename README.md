# GistGet

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/nuitsjp/GistGet)](https://github.com/nuitsjp/GistGet/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![CI](https://github.com/nuitsjp/GistGet/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/nuitsjp/GistGet/actions/workflows/ci.yml)
[![Coverage](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/nuitsjp/GistGet/coverage/.github/badges/coverage.json)](https://github.com/nuitsjp/GistGet/actions/workflows/ci.yml)

[Japanese](README.ja.md)

**GistGet** is a CLI tool designed to synchronize Windows Package Manager (`winget`) packages across multiple devices using GitHub Gist.
It uses a simple YAML configuration file stored in a private or public Gist to keep your installed applications and tools consistent.

## Features

-   **Cloud Sync**: Synchronize installed packages via GitHub Gist.
-   **Full Winget Compatibility**: Use standard `winget` commands directly, with integrated cloud sync capabilities (e.g., `gistget search`, `gistget install`).
-   **Cross-Device**: Keep your work and home computers in sync.
-   **Configuration as Code**: Manage your software list in a readable `GistGet.yaml` format.

## Requirements

-   Windows 10/11
-   Windows Package Manager (`winget`)

## Installation

### From GitHub Releases

1.  Download the latest release from the [Releases page](https://github.com/nuitsjp/GistGet/releases).
2.  Extract the zip file.
3.  Add the extracted folder to your system's `PATH`.

### From Winget (x64 portable)

```powershell
winget install NuitsJp.GistGet
```

After installation, you can launch it with:

```powershell
gistget --help
```

> Note: The published artifact is currently x64-only. ARM64 builds are not shipped yet.

### Troubleshooting

#### `gistget` command not found after installation

After installing via WinGet, existing terminal sessions may not recognize the `gistget` command because the PATH environment variable is not updated in running sessions. To resolve this:

1. **Restart your terminal** (Recommended)
   - Close the current terminal and open a new one

2. **Refresh PATH in PowerShell**
   ```powershell
   $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
   ```

3. **Use the full path temporarily**
   ```powershell
   & "$env:LOCALAPPDATA\Microsoft\WinGet\Links\gistget.exe" --help
   ```

## Usage

### Authentication

First, log in to your GitHub account to enable Gist access.

```powershell
gistget auth login
```

Follow the on-screen instructions to authenticate using the Device Flow.

### Initial Setup

When you install a package, it is automatically synced to Gist. Even if a package is already installed, running the `install` command will add it to Gist:

```powershell
# Install a new package and sync
gistget install --id Microsoft.PowerToys

# Add an already installed package to Gist
gistget install --id 7zip.7zip
```

By adding frequently used packages with `install`, you naturally build a synced package list in Gist.

> **Tip:** If you want to select all installed packages at once, you can use the `gistget init` command to interactively choose them.

### Sync

To sync local packages with the Gist:

```powershell
gistget sync
```

This performs the following operations:
1.  Fetches `GistGet.yaml` from the Gist.
2.  Compares it with locally installed packages.
3.  Installs missing packages and uninstalls packages marked for removal.

### Help

You can view the command list and options using the `--help` option:

```powershell
# Display all commands
gistget --help

# Display help for a specific command
gistget install --help
gistget sync --help
```

### Command List

GistGet supports both its own cloud sync features and all winget commands.

#### GistGet Native Commands

| Command | Description |
|---------|-------------|
| `auth login` | Authenticate with GitHub |
| `auth logout` | Log out from GitHub |
| `auth status` | Display current authentication status |
| `sync` | Synchronize packages with Gist |
| `init` | Interactively select installed packages to initialize Gist |
| `gist` | List packages from Gist |
| `install` | Install a package and save to Gist |
| `uninstall` | Uninstall a package and update Gist |
| `upgrade` | Upgrade a package and save to Gist |
| `pin add` | Pin a package and save to Gist |
| `pin remove` | Unpin a package and update Gist |

#### WinGet Compatible Commands (Passthrough)

The following commands are passed directly to winget. You can use them just like regular winget commands:

| Command | Description |
|---------|-------------|
| `list` | Display installed packages |
| `search` | Find and show basic package information |
| `show` | Show detailed package information |
| `source` | Manage package sources |
| `settings` | Open settings or modify administrator settings |
| `features` | Show status of experimental features |
| `hash` | Helper to hash installer files |
| `validate` | Validate a manifest file |
| `configure` | Configure the system into a desired state |
| `download` | Download installer from a package |
| `repair` | Repair the selected package |
| `pin list` | List current pins |
| `pin reset` | Reset pins |

**Usage Examples:**

```powershell
# Search for packages (same as winget)
gistget search vscode

# Show package information (same as winget)
gistget show Microsoft.PowerToys

# List installed packages (same as winget)
gistget list
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
| None | No pin. Eligible for all upgrades. | Allowed | Allowed |
| `pinning` | Default. Excluded from `upgrade --all`, but explicit upgrade is possible. | Skipped | Allowed |
| `blocking` | Excluded from `upgrade --all`. Explicit upgrade is also possible. | Skipped | Allowed |
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
# FormatCheck -> Build -> Tests -> ReSharper
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
