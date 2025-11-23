# GistGet Command Specification

## Overview
GistGet is a CLI tool that wraps `winget` to provide configuration management via GitHub Gist. It supports all `winget` commands via passthrough, and adds specific commands for synchronization and authentication.

## Command Structure
```
gistget <command> [options]
```

## Core Commands (GistGet Specific)

### `sync`
Synchronizes the local package state with the configuration stored in Gist.

**Syntax:**
```bash
gistget sync [--url <gist-url>] [--dry-run]
```

**Options:**
- `--url <gist-url>`: Uses a specific Gist URL (raw or web) for this sync operation. Does not require authentication if the Gist is public.
- `--dry-run`: (Future) Shows what would happen without making changes.

**Behavior:**
1.  Retrieves package list from Gist (or URL).
2.  Compares with locally installed packages.
3.  Installs missing packages.
4.  Uninstalls packages marked with `uninstall: true` if they are present.
5.  Updates packages if version mismatch (configurable).

### `export`
Exports the current local package state to a YAML file compatible with GistGet.

**Syntax:**
```bash
gistget export [--output <file>]
```

**Options:**
- `--output <file>`: Specifies the output file path. Defaults to stdout or a default file.

### `import`
Imports a YAML file and updates the Gist.

**Syntax:**
```bash
gistget import <file> [--create-gist]
```

**Options:**
- `--create-gist`: Creates a new Gist instead of updating the existing one.

### `auth`
Manages authentication with GitHub.

**Syntax:**
```bash
gistget auth <subcommand>
```

**Subcommands:**
- `login`: Initiates Device Flow authentication.
- `logout`: Clears stored credentials.
- `status`: Checks current authentication status.

## Passthrough Commands (Winget Compatibility)
All other commands are passed through to `winget`.

- `install`
- `uninstall`
- `upgrade`
- `list`
- `search`
- `show`
- `source`
- `settings`
- `features`

**Example:**
```bash
gistget search vscode
# Equivalent to: winget search vscode
```

## Data Format (packages.yaml)
The configuration file is a YAML dictionary where keys are Package IDs.

```yaml
<PackageId>:
  version: <string>
  custom: <string> # Custom install arguments
  uninstall: <boolean> # If true, ensures package is removed
  # Other winget install parameters
  scope: <user|machine>
  interactive: <boolean>
  silent: <boolean>
  locale: <string>
  location: <string>
  architecture: <x86|x64|arm|arm64>
```
