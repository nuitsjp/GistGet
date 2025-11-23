# GistGet YAML Specification

This document defines the schema for `packages.yaml` used by GistGet to synchronize Windows Package Manager (winget) packages.

## Schema Overview

The YAML file consists of a map where keys are Package IDs and values are objects containing installation parameters and synchronization flags.

```yaml
<PackageId>:
  version: <string>
  uninstall: <boolean>
  pin: <boolean>
  scope: <user|machine>
  interactive: <boolean>
  silent: <boolean>
  locale: <string>
  location: <string>
  log: <string>
  custom: <string>
  override: <string>
  force: <boolean>
  allowHashMismatch: <boolean>
  skipDependencies: <boolean>
  acceptPackageAgreements: <boolean>
  acceptSourceAgreements: <boolean>
  header: <string>
  architecture: <x86|x64|arm|arm64>
  installerType: <exe|msi|msix|...>
```

## Parameters

### Core Parameters

*   **`version`**: The specific version of the package to install. If omitted, the latest version is assumed.
    *   *Maps to*: `winget install --version <value>`
*   **`uninstall`**: If `true`, the package will be uninstalled during synchronization.
    *   *Maps to*: `winget uninstall`
*   **`pin`**: If `true`, the package version will be pinned in winget to prevent accidental upgrades.
    *   *Maps to*: `winget pin add` (logic to be implemented)

### Install Options (Winget Passthrough)

These parameters map directly to `winget install` options.

*   **`scope`**: Installs the package for the current user or the entire machine.
    *   *Values*: `user`, `machine`
    *   *Maps to*: `--scope <value>`
*   **`architecture`**: Specifies the architecture to install.
    *   *Values*: `x86`, `x64`, `arm`, `arm64`
    *   *Maps to*: `--architecture <value>`
*   **`installerType`**: Specifies the installer type.
    *   *Values*: `exe`, `msi`, `msix`, etc.
    *   *Maps to*: `--installer-type <value>`
*   **`interactive`**: Requests interactive installation.
    *   *Maps to*: `--interactive`
*   **`silent`**: Requests silent installation.
    *   *Maps to*: `--silent`
*   **`locale`**: Specifies the locale (BCP47 format).
    *   *Maps to*: `--locale <value>`
*   **`location`**: Specifies the installation location.
    *   *Maps to*: `--location <value>`
*   **`log`**: Path to the log file.
    *   *Maps to*: `--log <value>`
*   **`custom`**: Additional arguments to pass to the installer.
    *   *Maps to*: `--custom "<value>"`
*   **`override`**: Overrides the arguments passed to the installer.
    *   *Maps to*: `--override "<value>"`
*   **`force`**: Forces the command execution.
    *   *Maps to*: `--force`
*   **`allowHashMismatch`**: Ignores hash mismatch errors.
    *   *Maps to*: `--ignore-security-hash`
*   **`skipDependencies`**: Skips processing dependencies.
    *   *Maps to*: `--skip-dependencies`
*   **`acceptPackageAgreements`**: Accepts all package agreements.
    *   *Maps to*: `--accept-package-agreements`
*   **`acceptSourceAgreements`**: Accepts all source agreements.
    *   *Maps to*: `--accept-source-agreements`
*   **`header`**: Custom HTTP header for REST source.
    *   *Maps to*: `--header "<value>"`

### Advanced / New Options (To be supported)

*   **`allowReboot`**: Allows reboot if applicable. (`--allow-reboot`)
*   **`noUpgrade`**: Skips upgrade if already installed. (`--no-upgrade`)
*   **`uninstallPrevious`**: Uninstalls previous version during upgrade. (`--uninstall-previous`)
*   **`rename`**: Renames the executable (portable). (`--rename`)

## Example

```yaml
Microsoft.VisualStudioCode:
  scope: user
  silent: true
  override: /VERYSILENT /MERGETASKS=!runcode

7zip.7zip:
  version: 23.01
  pin: true
  architecture: x64

DeepL.DeepL:
  uninstall: true
```
