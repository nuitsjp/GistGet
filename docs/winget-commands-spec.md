# WinGet Commands Complete Specification

**Version**: 1.11.430  
**Generated**: 2025-08-08  
**Purpose**: .NET 8 implementation reference for complete WinGet CLI compliance

---

## Command Overview

WinGet provides 18 primary commands with hierarchical subcommands and extensive option support.

| Command | Aliases | Primary Function | Subcommands |
|---------|---------|------------------|-------------|
| install | add | Package installation | - |
| list | ls | Display installed packages | - |
| upgrade | update | Package upgrades | - |
| uninstall | remove, rm | Package removal | - |
| search | find | Package search | - |
| show | view | Package information display | - |
| source | - | Package source management | add, list, update, remove, reset, export |
| settings | config | Settings management | export, set, reset |
| export | - | Package list export | - |
| import | - | Package list import | - |
| pin | - | Package pin management | add, remove, list, reset |
| configure | configuration, dsc | System configuration | show, list, test, validate, export |
| download | - | Installer download | - |
| repair | fix | Package repair | - |
| hash | - | Hash calculation helper | - |
| validate | - | Manifest validation | - |
| features | - | Experimental features | - |
| dscv3 | - | DSC v3 resources | package, source, user-settings-file, admin-settings |

---

## Global Options

All commands support these global options:

```
  -?, --help                   ヘルプ表示
  -v, --version               バージョン表示
  --info                      一般情報表示
  --wait                      キー入力待機
  --logs, --open-logs         ログ場所を開く
  --verbose, --verbose-logs   詳細ログ有効化
  --nowarn, --ignore-warnings 警告非表示
  --disable-interactivity     対話プロンプト無効化
  --proxy                     プロキシ設定
  --no-proxy                  プロキシ使用無効化
```

---

## Command Specifications

### 1. install / add

**Purpose**: Install specified packages from configured sources or directly from manifests.

**Usage**: `winget install [[-q] <query>...] [<options>]`

**Arguments**:
- `-q, --query <text>`: Search query for package identification

**Required Parameters**: One of `--query`, `--id`, `--name`, or `--moniker` is required

**Search Options**:
```
  --id <packageid>           Package ID specification
  --name <name>              Package name specification  
  --moniker <moniker>        Moniker specification
  -e, --exact                Exact match search
  -s, --source <source>      Search source specification
```

**Installation Options**:
```
  -v, --version <version>    Version specification
  --scope <user|machine>     Installation scope
  -a, --architecture <arch>  Architecture selection
  --installer-type <type>    Installer type selection
  -i, --interactive          Interactive installation
  -h, --silent               Silent installation
  -l, --location <path>      Installation location
  --locale <locale>          Locale (BCP47 format)
  -o, --log <logfile>        Log file specification
  --custom <args>            Additional arguments
  --override <args>          Argument override
```

**Security & Behavior Options**:
```
  --ignore-security-hash     Ignore hash check failures
  --allow-reboot             Allow reboot
  --skip-dependencies        Skip dependency processing
  --ignore-local-archive-malware-scan  Ignore malware scanning
  --dependency-source <src>  Dependency search source
  --accept-package-agreements Accept package license agreements
  --accept-source-agreements  Accept source license agreements
  --no-upgrade               Skip upgrade if already installed
  -r, --rename <name>        Executable renaming (portable)
  --uninstall-previous       Remove previous version during upgrade
  --force                    Force execution
```

**Authentication Options**:
```
  --header <header>          HTTP header
  --authentication-mode <mode>  Authentication mode (silent/silentPreferred/interactive)
  --authentication-account <account>  Authentication account
```

### 2. list / ls

**Purpose**: Display installed packages and available upgrades.

**Usage**: `winget list [[-q] <query>] [<options>]`

**Arguments**:
- `-q, --query <text>`: Search query

**Filtering Options**:
```
  --id <packageid>           Package ID filter
  --name <name>              Package name filter
  --moniker <moniker>        Moniker filter
  --tag <tag>                Tag filter
  --cmd, --command <cmd>     Command filter
  -s, --source <source>      Source specification
  -e, --exact                Exact match
  --scope <user|machine>     Scope filter
  -n, --count <number>       Result limit (1-1000)
```

**Upgrade-Related Options**:
```
  --upgrade-available         Display only upgradeable packages
  -u, --unknown, --include-unknown  Include unknown version packages (requires --upgrade-available)
  --pinned, --include-pinned  Include pinned packages (requires --upgrade-available)
```

**Authentication Options**:
```
  --header <header>          HTTP header
  --authentication-mode <mode>  Authentication mode
  --authentication-account <account>  Authentication account
  --accept-source-agreements  Accept source license agreements
```

### 3. upgrade / update

**Purpose**: Upgrade installed packages to newer versions.

**Usage**: `winget upgrade [[-q] <query>...] [<options>]`

**Search & Filtering**:
```
  -q, --query <text>         Search query
  --id <packageid>           Package ID
  --name <name>              Package name
  --moniker <moniker>        Moniker
  -s, --source <source>      Search source
  -e, --exact                Exact match
```

**Upgrade Control**:
```
  -v, --version <version>    Version specification
  -r, --recurse, --all       Upgrade all packages
  -u, --unknown, --include-unknown  Include unknown versions
  --pinned, --include-pinned Include pinned packages
  --uninstall-previous       Remove previous version
  --force                    Force execution
```

**Installation Options** (same as install command):
```
  --scope, --architecture, --installer-type, --locale
  -i, --interactive, -h, --silent, -l, --location
  -o, --log, --custom, --override
  --ignore-security-hash, --allow-reboot, --skip-dependencies
  --accept-package-agreements, --accept-source-agreements
  --purge                    Remove all files/directories (portable)
```

### 4. uninstall / remove / rm

**Purpose**: Uninstall selected packages from system.

**Usage**: `winget uninstall [[-q] <query>...] [<options>]`

**Search Options**:
```
  -q, --query <text>         Search query
  --id <packageid>           Package ID filter
  --name <name>              Package name filter
  --moniker <moniker>        Moniker filter
  --product-code <code>      Product code filter
  -s, --source <source>      Search source
  -e, --exact                Exact match
  --scope <user|machine>     Scope filter
```

**Uninstall Control**:
```
  -v, --version <version>    Version to process
  --all, --all-versions      Uninstall all versions
  -i, --interactive          Interactive uninstall
  -h, --silent               Silent uninstall
  --force                    Force execution
  --purge                    Remove all files/directories (portable)
  --preserve                 Preserve all files/directories (portable)
  -o, --log <path>           Log location
```

### 5. search / find

**Purpose**: Search for packages in configured sources.

**Usage**: `winget search [[-q] <query>] [<options>]`

**Search Options**:
```
  -q, --query <text>         Search query
  --id <packageid>           ID filter
  --name <name>              Name filter
  --moniker <moniker>        Moniker filter
  --tag <tag>                Tag filter
  --cmd, --command <cmd>     Command filter
  -s, --source <source>      Search source
  -n, --count <number>       Result limit (1-1000)
  -e, --exact                Exact match
  --versions                 Show available versions
```

### 6. show / view

**Purpose**: Display detailed package information.

**Usage**: `winget show [[-q] <query>] [<options>]`

**Options**:
```
  -q, --query <text>         Search query
  -m, --manifest <path>      Manifest file path
  --id, --name, --moniker    Standard filters
  -v, --version <version>    Version specification
  -s, --source <source>      Search source
  -e, --exact                Exact match
  --scope <user|machine>     Scope selection
  -a, --architecture <arch>  Architecture selection
  --installer-type <type>    Installer type selection
  --locale <locale>          Locale specification
  --versions                 Show available versions
```

### 7. source

**Purpose**: Manage package sources with hierarchical subcommands.

**Usage**: `winget source [<subcommand>] [<options>]`

**Subcommands**:

#### source add
**Usage**: `winget source add [-n] <name> [-a] <arg> [[-t] <type>] [<options>]`
```
  -n, --name <name>          Source name
  -a, --arg <url>            Source URL/argument
  -t, --type <type>          Source type
  --trust-level <level>      Trust level (none or trusted)
  --explicit                 Exclude from discovery unless specified
  --header <header>          HTTP header
```

#### source list
**Usage**: `winget source list`

#### source update
**Usage**: `winget source update [name]`

#### source remove
**Usage**: `winget source remove <name>`

#### source reset
**Usage**: `winget source reset`

#### source export
**Usage**: `winget source export`

### 8. settings / config

**Purpose**: Settings management with subcommands.

**Usage**: `winget settings [<subcommand>] [<options>]`

**Subcommands**:
- `export`: Settings export
- `set <setting> <value>`: Administrator setting configuration
- `reset <setting>`: Setting reset

**Options**:
```
  --enable <setting>         Enable administrator setting
  --disable <setting>        Disable administrator setting
```

**Example Settings**:
- `LocalManifestFiles`: Allow local manifest files
- `BypassCertificatePinningForMicrosoftStore`: Bypass certificate pinning for MS Store
- `InstallerHashOverride`: Allow installer hash override

### 9. export

**Purpose**: Export installed packages list to file.

**Usage**: `winget export [-o] <output> [<options>]`

**Arguments**:
- `-o, --output <file>`: Output file specification

**Options**:
```
  -s, --source <source>      Export from specified source
  --include-versions         Include package versions in export
  --accept-source-agreements Accept source license agreements
```

### 10. import

**Purpose**: Install all packages listed in file.

**Usage**: `winget import [-i] <import-file> [<options>]`

**Arguments**:
- `-i, --import-file <file>`: Import file specification

**Options**:
```
  --ignore-unavailable       Ignore unavailable packages
  --ignore-versions          Ignore package versions in import file
  --no-upgrade               Skip upgrade if already installed
  --accept-package-agreements Accept package license agreements
  --accept-source-agreements  Accept source license agreements
```

### 11. pin

**Purpose**: Package pin management with subcommands.

**Usage**: `winget pin [<subcommand>] [<options>]`

**Subcommands**:
- `add`: Add new pin
- `remove`: Remove package pin
- `list`: List current pins
- `reset`: Reset pins

### 12. configure / configuration / dsc

**Purpose**: System configuration management.

**Usage**: `winget configure [<subcommand>] [[-f] <file>] [[--module-path] <module-path>] [<options>]`

**Arguments**:
```
  -f, --file <file>          Configuration file path
  --module-path <path>       Module storage location (default: %LOCALAPPDATA%\Microsoft\WinGet\Configuration\Modules)
```

**Subcommands**:
- `show`: Show configuration details
- `list`: Show configuration history
- `test`: Test system against desired state
- `validate`: Validate configuration file
- `export`: Export configuration resources to file

**Options**:
```
  --processor-path <path>    Configuration processor path
  -h, --history              Select item from history
  --accept-configuration-agreements Accept configuration warnings
  --suppress-initial-details  Suppress initial configuration details
  --enable                   Enable configuration components
  --disable                  Disable configuration components
```

### 13. download

**Purpose**: Download package installers without installation.

**Usage**: `winget download [[-q] <query>] [<options>]`

**Arguments**:
- `-q, --query <text>`: Search query

**Options**:
```
  -d, --download-directory <dir>  Download destination directory
  -m, --manifest <path>      Manifest file path
  --id, --name, --moniker    Standard search filters
  -v, --version <version>    Version specification
  -s, --source <source>      Search source
  --scope <user|machine>     Installation scope
  -a, --architecture <arch>  Architecture selection
  --installer-type <type>    Installer type selection
  -e, --exact                Exact match
  --locale <locale>          Locale specification
  --ignore-security-hash     Ignore hash check failures
  --skip-dependencies        Skip dependency processing
  --skip-license, --skip-microsoft-store-package-license Skip Microsoft Store package offline license
  --platform <platform>     Target platform selection
```

### 14. repair / fix

**Purpose**: Repair selected packages.

**Usage**: `winget repair [[-q] <query>] [<options>]`

**Options**:
```
  -q, --query <text>         Search query
  -m, --manifest <path>      Manifest file path
  --id, --name, --moniker    Standard search filters
  -v, --version <version>    Version to process
  --product-code <code>      Product code filter
  -a, --architecture <arch>  Architecture selection
  --scope <user|machine>     Scope filter
  -s, --source <source>      Search source
  -i, --interactive          Interactive repair
  -h, --silent               Silent repair
  -o, --log <path>           Log location
  --ignore-local-archive-malware-scan  Ignore malware scanning
  --accept-source-agreements Accept source license agreements
  --accept-package-agreements Accept package license agreements
  --locale <locale>          Locale specification
  --force                    Force execution
  --ignore-security-hash     Ignore hash check failures
  -e, --exact                Exact match
```

### 15. hash

**Purpose**: Calculate hash for local files suitable for manifest entries.

**Usage**: `winget hash [-f] <file> [<options>]`

**Arguments**:
- `-f, --file <path>`: File to hash

**Options**:
```
  -m, --msix                 Treat input file as MSIX; provide signature hash if signed
```

### 16. validate

**Purpose**: Validate manifest files against strict guidelines.

**Usage**: `winget validate [--manifest] <manifest> [<options>]`

**Arguments**:
- `--manifest <path>`: Path to manifest to validate

### 17. features

**Purpose**: Display experimental features status.

**Usage**: `winget features [<options>]`

**Note**: Experimental features can be enabled using "winget settings".

### 18. dscv3

**Purpose**: Desired State Configuration (DSC) v3 resources for winget and packages.

**Usage**: `winget dscv3 [<subcommand>] [<options>]`

**Subcommands**:
- `package`: Manage package state
- `source`: Manage source configuration
- `user-settings-file`: Manage user settings file
- `admin-settings`: Manage admin settings

**Options**:
```
  --manifest                 Get resource manifest
  -o, --output <dir>         Directory to write results
```

---

## Argument Dependencies and Validation Rules

### Conditional Options
1. **list command**: `--include-unknown` and `--include-pinned` require `--upgrade-available`
2. **Authentication**: `--authentication-account` typically requires `--authentication-mode`

### Mutual Exclusivity Patterns
1. **Search Parameters**: `--query`, `--id`, `--name`, `--moniker` can be combined, with priority order
2. **Installation Modes**: `--interactive` vs `--silent` are mutually exclusive
3. **Scope**: `user` vs `machine` are mutually exclusive values

### Required Parameters
1. **install**: Requires at least one of: `--query`, `--id`, `--name`, `--moniker`, `--manifest`
2. **source add**: Requires `--name` and `--arg`
3. **hash**: Requires `--file`
4. **validate**: Requires `--manifest`
5. **export**: Requires `--output`
6. **import**: Requires `--import-file`

---

## Version Compatibility

This specification is based on WinGet version 1.11.430. Command options and behaviors may vary in different versions. The .NET implementation should include version checking and graceful degradation for unsupported options.