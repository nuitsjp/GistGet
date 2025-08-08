# WinGet Arguments Relationship Matrix

**Purpose**: Detailed analysis of argument dependencies, mutual exclusivity, and conditional relationships for .NET parser implementation.

---

## Global Argument Categories

### Search/Filter Arguments
| Argument | Commands | Priority | Combinable | Notes |
|----------|----------|----------|------------|--------|
| `--query, -q` | install, list, upgrade, uninstall, search, show, download, repair | 1 | Yes | Primary search parameter |
| `--id` | install, list, upgrade, uninstall, search, show, download, repair | 2 | Yes | Higher priority than query |
| `--name` | install, list, upgrade, uninstall, search, show, download, repair | 3 | Yes | Medium priority |
| `--moniker` | install, list, upgrade, uninstall, search, show, download, repair | 4 | Yes | Lower priority |
| `--exact, -e` | install, list, upgrade, uninstall, search, show, download, repair | - | Yes | Modifier for search behavior |

### Package Selection Arguments
| Argument | Commands | Mutual Exclusion | Dependencies |
|----------|----------|------------------|--------------|
| `--manifest, -m` | install, upgrade, uninstall, show, download, repair | Exclusive with search args | Requires valid file path |
| `--version, -v` | install, upgrade, uninstall, show, download, repair | - | Compatible with search args |
| `--product-code` | uninstall, repair | - | Alternative to standard search |

---

## Command-Specific Relationship Analysis

### 1. install Command

#### Required Parameters Matrix
```
At least ONE required:
├── --query, -q
├── --id
├── --name  
├── --moniker
└── --manifest, -m

If --manifest specified:
├── Search arguments (--query, --id, --name, --moniker) are IGNORED
└── Package selection is based on manifest content
```

#### Installation Mode Mutual Exclusivity
```
EXCLUSIVE:
├── --interactive, -i
└── --silent, -h

DEFAULT: Automatic mode (neither interactive nor silent)
```

#### Scope Mutual Exclusivity
```
EXCLUSIVE VALUES:
├── --scope user
└── --scope machine

DEFAULT: System default or package manifest preference
```

#### Override vs Custom Arguments
```
RELATIONSHIP:
├── --custom <args>    → Additional arguments (additive)
└── --override <args>  → Complete replacement (exclusive of defaults)

VALIDATION: Both can be specified simultaneously
```

### 2. list Command

#### Conditional Dependencies
```
REQUIRES --upgrade-available:
├── --unknown, -u, --include-unknown
└── --pinned, --include-pinned

ERROR if specified without --upgrade-available:
"引数 include-unknown は upgrade-available でのみ使用できます"
```

#### Count Limitations
```
--count, -n:
├── Range: 1-1000
├── Applied after filtering
└── Compatible with all filter arguments
```

### 3. upgrade Command

#### All Packages vs Specific Selection
```
BEHAVIOR:
├── --recurse, -r, --all → Upgrade all available packages
├── Search arguments      → Upgrade specific packages
└── No arguments         → List available upgrades (no action)

COMBINATION: --all can be combined with filters for subset operation
```

#### Include Options Dependencies
```
CONDITIONAL (same as list):
├── --unknown, -u, --include-unknown → Requires upgrade operation context
└── --pinned, --include-pinned       → Requires upgrade operation context
```

### 4. source Command Hierarchy

#### Subcommand Parameter Requirements
```
source add:
├── REQUIRED: --name, -n <name>
├── REQUIRED: --arg, -a <arg>
├── OPTIONAL: --type, -t <type>
└── OPTIONAL: --trust-level, --explicit, --header

source list:
└── No additional parameters

source update:
└── OPTIONAL: [name] (specific source or all)

source remove:
└── REQUIRED: <name>

source reset:
└── No additional parameters

source export:
└── No additional parameters
```

### 5. Authentication Arguments

#### Authentication Mode Dependencies
```
--authentication-mode <mode>:
├── VALUES: silent | silentPreferred | interactive
├── OPTIONAL COMPANION: --authentication-account <account>
└── CONTEXT: REST source operations

VALIDATION: account without mode is valid, mode without account is valid
```

### 6. Security & Safety Arguments

#### Hash and Security Overrides
```
INDEPENDENT FLAGS:
├── --ignore-security-hash       → Skip hash validation
├── --allow-reboot              → Allow system reboot
├── --skip-dependencies         → Skip dependency processing
├── --ignore-local-archive-malware-scan → Skip malware scanning
└── --force                     → Force execution despite warnings

NO MUTUAL EXCLUSIVITY: All can be combined
```

---

## Advanced Dependency Patterns

### 1. Version-Specific Validations

#### Architecture Dependencies
```
--architecture <arch>:
├── CONTEXT-DEPENDENT: Available architectures vary by package
├── VALIDATION: Must check against package manifest
└── FALLBACK: Use system default if unsupported
```

#### Installer Type Dependencies
```
--installer-type <type>:
├── PACKAGE-DEPENDENT: Available types vary by package
├── COMMON TYPES: msi, exe, msix, inno, nullsoft, burn, appx
└── VALIDATION: Must check against package manifest
```

### 2. Locale and Internationalization
```
--locale <locale>:
├── FORMAT: BCP47 (e.g., en-US, ja-JP, zh-CN)
├── VALIDATION: Must validate against BCP47 standard
├── FALLBACK: Use system locale if unsupported
└── PACKAGE-DEPENDENT: Available locales vary by package
```

### 3. Source Dependencies
```
--source, -s <source>:
├── VALIDATION: Must exist in configured sources
├── CONTEXT: Affects search scope and authentication
├── DEFAULT: All configured sources if not specified
└── DEPENDENCY: May require source-specific authentication
```

---

## Error Patterns and Validation Rules

### 1. Missing Required Parameters
```
ERROR TEMPLATES:
├── "At least one of --query, --id, --name, --moniker, or --manifest is required"
├── "source add requires --name and --arg parameters"
├── "hash command requires --file parameter"
└── "validate command requires --manifest parameter"
```

### 2. Conditional Parameter Errors
```
ERROR TEMPLATES:
├── "引数 include-unknown は upgrade-available でのみ使用できます"
├── "引数 include-pinned は upgrade-available でのみ使用できます"
├── "--authentication-account requires --authentication-mode"
└── "--custom and --override cannot be used with --manifest"
```

### 3. Value Range Errors
```
VALIDATION RULES:
├── --count: 1 ≤ value ≤ 1000
├── --trust-level: "none" | "trusted"
├── --scope: "user" | "machine"
├── --authentication-mode: "silent" | "silentPreferred" | "interactive"
└── --locale: Valid BCP47 format
```

### 4. File Path Validations
```
PATH REQUIREMENTS:
├── --manifest: Must exist and be readable
├── --import-file: Must exist and be valid format
├── --output: Parent directory must exist and be writable
├── --log: Parent directory must be writable
├── --location: Must be valid installation path
└── --download-directory: Must exist and be writable
```

---

## Implementation Guidance

### 1. Argument Parsing Order
1. **Global Options**: Parse and apply first
2. **Command Identification**: Determine primary command and subcommand
3. **Required Parameters**: Validate mandatory arguments
4. **Conditional Dependencies**: Check conditional requirements
5. **Mutual Exclusivity**: Validate exclusive options
6. **Value Validation**: Check parameter value constraints
7. **Context Validation**: Verify command-specific rules

### 2. Error Handling Priority
1. **Syntax Errors**: Malformed arguments, unknown options
2. **Required Parameters**: Missing mandatory arguments
3. **Mutual Exclusivity**: Conflicting options
4. **Conditional Dependencies**: Missing required dependencies
5. **Value Constraints**: Out-of-range or invalid values
6. **Context Validation**: Command-specific validation failures

### 3. Compatibility Considerations
- **Version Tolerance**: Handle unknown options gracefully in older WinGet versions
- **Platform Differences**: Account for Windows version-specific behavior
- **Source Variations**: Different source types may have different validation rules
- **Locale Support**: Proper handling of non-English locales and error messages

---

This matrix provides the foundation for implementing robust argument validation in the .NET WinGet wrapper, ensuring complete compliance with WinGet CLI behavior.