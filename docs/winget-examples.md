# WinGet Command Examples and Test Cases

**Purpose**: Comprehensive examples and test cases for .NET implementation validation and end-to-end testing.

---

## Basic Command Examples

### 1. install / add Commands

#### Standard Installation Examples
```bash
# Basic package installation by name
winget install "Visual Studio Code"
winget install --name "Visual Studio Code"

# Installation by exact ID
winget install --id Microsoft.VisualStudioCode

# Installation with version specification
winget install --id Microsoft.VisualStudioCode --version 1.85.0

# Installation with architecture specification
winget install --id Microsoft.VisualStudioCode --architecture x64

# Installation with scope specification
winget install --id Microsoft.VisualStudioCode --scope user
winget install --id Microsoft.VisualStudioCode --scope machine

# Silent installation
winget install --id Microsoft.VisualStudioCode --silent

# Interactive installation with custom arguments
winget install --id Microsoft.VisualStudioCode --interactive --custom "/VERYSILENT /SUPPRESSMSGBOXES"

# Installation from specific source
winget install --id Microsoft.VisualStudioCode --source msstore

# Installation with location specification
winget install --id Microsoft.VisualStudioCode --location "C:\Tools\VSCode"
```

#### Advanced Installation Examples
```bash
# Multiple packages installation (space-separated queries)
winget install vscode notepad++ 7zip

# Installation with override arguments
winget install --id Microsoft.VisualStudioCode --override "/VERYSILENT /LOADINF=""C:\temp\vscode.inf"""

# Installation with dependency management
winget install --id Microsoft.VisualStudioCode --skip-dependencies

# Installation with security overrides
winget install --id Microsoft.VisualStudioCode --ignore-security-hash --force

# Installation from manifest file
winget install --manifest "C:\manifests\vscode.yaml"

# Installation with authentication
winget install --id CompanyApp.BusinessTool --authentication-mode interactive --authentication-account "user@company.com"
```

### 2. list / ls Commands

#### Basic Listing Examples
```bash
# List all installed packages
winget list

# List packages with query filter
winget list microsoft

# List with exact ID match
winget list --id Microsoft.VisualStudioCode

# List with name filter
winget list --name "Visual Studio"

# List from specific source
winget list --source msstore

# List with result count limit
winget list --count 50
```

#### Advanced Listing Examples
```bash
# List packages with available upgrades
winget list --upgrade-available

# List upgrades including unknown versions
winget list --upgrade-available --include-unknown

# List upgrades including pinned packages
winget list --upgrade-available --include-pinned

# List with specific scope filter
winget list --scope user

# List with multiple filters
winget list --name visual --source msstore --count 10
```

### 3. upgrade / update Commands

#### Basic Upgrade Examples
```bash
# Show available upgrades
winget upgrade

# Upgrade specific package
winget upgrade --id Microsoft.VisualStudioCode

# Upgrade all packages
winget upgrade --all

# Upgrade with version specification
winget upgrade --id Microsoft.VisualStudioCode --version 1.85.1
```

#### Advanced Upgrade Examples
```bash
# Upgrade including unknown versions
winget upgrade --all --include-unknown

# Upgrade with uninstall previous version
winget upgrade --id Microsoft.VisualStudioCode --uninstall-previous

# Silent upgrade with custom arguments
winget upgrade --id Microsoft.VisualStudioCode --silent --custom "/QUIET"

# Force upgrade despite warnings
winget upgrade --id Microsoft.VisualStudioCode --force

# Upgrade from specific source
winget upgrade --id Microsoft.VisualStudioCode --source winget
```

### 4. uninstall Commands

#### Basic Uninstall Examples
```bash
# Uninstall by name
winget uninstall "Visual Studio Code"

# Uninstall by exact ID
winget uninstall --id Microsoft.VisualStudioCode

# Uninstall specific version
winget uninstall --id Microsoft.VisualStudioCode --version 1.85.0

# Uninstall all versions
winget uninstall --id Microsoft.VisualStudioCode --all-versions
```

#### Advanced Uninstall Examples
```bash
# Silent uninstall
winget uninstall --id Microsoft.VisualStudioCode --silent

# Interactive uninstall
winget uninstall --id Microsoft.VisualStudioCode --interactive

# Uninstall with purge (portable packages)
winget uninstall --id PortableApp.Example --purge

# Uninstall with preserve (portable packages)
winget uninstall --id PortableApp.Example --preserve

# Force uninstall
winget uninstall --id Microsoft.VisualStudioCode --force
```

### 5. Source Management Examples

#### Source Add Examples
```bash
# Add basic source
winget source add --name "CompanyRepo" --arg "https://repo.company.com/packages"

# Add source with type specification
winget source add --name "PrivateNuGet" --arg "https://nuget.company.com/v3/index.json" --type Microsoft.PackageManager.CompositeSource

# Add source with trust level
winget source add --name "TrustedRepo" --arg "https://trusted.repo.com" --trust-level trusted

# Add explicit source (not included in discovery)
winget source add --name "ManualRepo" --arg "https://manual.repo.com" --explicit

# Add source with authentication header
winget source add --name "AuthRepo" --arg "https://auth.repo.com" --header "Authorization: Bearer token123"
```

#### Other Source Commands
```bash
# List all sources
winget source list

# Update specific source
winget source update "CompanyRepo"

# Update all sources
winget source update

# Remove source
winget source remove "CompanyRepo"

# Reset all sources to default
winget source reset

# Export source configuration
winget source export
```

### 6. Export/Import Examples

#### Export Examples
```bash
# Basic export
winget export --output "packages.json"

# Export with versions
winget export --output "packages-with-versions.json" --include-versions

# Export from specific source
winget export --output "msstore-packages.json" --source msstore

# Export with source agreements acceptance
winget export --output "packages.json" --accept-source-agreements
```

#### Import Examples
```bash
# Basic import
winget import --import-file "packages.json"

# Import ignoring unavailable packages
winget import --import-file "packages.json" --ignore-unavailable

# Import ignoring version specifications
winget import --import-file "packages.json" --ignore-versions

# Import without upgrades
winget import --import-file "packages.json" --no-upgrade

# Import with agreements acceptance
winget import --import-file "packages.json" --accept-package-agreements --accept-source-agreements
```

### 7. Settings Management Examples

#### Settings Commands
```bash
# Open settings file
winget settings

# Export current settings
winget settings export

# Set administrator setting
winget settings --enable LocalManifestFiles

# Disable administrator setting
winget settings --disable BypassCertificatePinningForMicrosoftStore

# Reset setting to default
winget settings reset LocalManifestFiles
```

### 8. Advanced Usage Examples

#### Pin Management
```bash
# List current pins
winget pin list

# Add version range pin
winget pin add --id Microsoft.VisualStudioCode --version "1.85.*"

# Add blocking pin
winget pin add --id Microsoft.VisualStudioCode --blocking

# Remove pin
winget pin remove --id Microsoft.VisualStudioCode

# Reset all pins
winget pin reset
```

#### Download Examples
```bash
# Download package installer
winget download --id Microsoft.VisualStudioCode

# Download to specific directory
winget download --id Microsoft.VisualStudioCode --download-directory "C:\Downloads"

# Download specific version
winget download --id Microsoft.VisualStudioCode --version 1.85.0 --architecture x64

# Download with platform specification
winget download --id Microsoft.WindowsTerminal --platform win32
```

#### Repair Examples
```bash
# Repair package
winget repair --id Microsoft.VisualStudioCode

# Interactive repair
winget repair --id Microsoft.VisualStudioCode --interactive

# Silent repair with logging
winget repair --id Microsoft.VisualStudioCode --silent --log "C:\temp\repair.log"
```

---

## Error Case Examples

### 1. Missing Required Parameters

#### Install Command Errors
```bash
# ERROR: No package identifier
winget install
# Expected: At least one of --query, --id, --name, --moniker, or --manifest is required

# ERROR: source add missing parameters  
winget source add
# Expected: source add requires --name and --arg parameters
```

### 2. Conditional Dependency Errors

#### List Command Conditional Errors
```bash
# ERROR: include-unknown without upgrade-available
winget list --include-unknown
# Expected: 引数 include-unknown は upgrade-available でのみ使用できます

# ERROR: include-pinned without upgrade-available  
winget list --include-pinned
# Expected: 引数 include-pinned は upgrade-available でのみ使用できます
```

### 3. Mutual Exclusivity Errors

#### Installation Mode Conflicts
```bash
# ERROR: Interactive and silent together
winget install --id Microsoft.VisualStudioCode --interactive --silent
# Expected: --interactive and --silent cannot be used together

# ERROR: Purge and preserve together
winget uninstall --id PortableApp.Example --purge --preserve
# Expected: --purge and --preserve cannot be used together
```

### 4. Value Constraint Errors

#### Range Validation Errors
```bash
# ERROR: Count out of range
winget list --count 1500
# Expected: --count parameter must be between 1 and 1000

# ERROR: Invalid scope value
winget install --id Microsoft.VisualStudioCode --scope invalid
# Expected: Invalid scope value: invalid. Valid values are: user, machine

# ERROR: Invalid authentication mode
winget install --id CompanyApp --authentication-mode invalid
# Expected: Invalid authentication mode: invalid. Valid values are: silent, silentPreferred, interactive
```

### 5. File Path Errors

#### File Existence Errors
```bash
# ERROR: Manifest file not found
winget install --manifest "C:\nonexistent\manifest.yaml"
# Expected: --manifest: File does not exist: C:\nonexistent\manifest.yaml

# ERROR: Import file not found
winget import --import-file "missing.json"
# Expected: --import-file: File does not exist: missing.json

# ERROR: Output directory not writable
winget export --output "C:\Windows\System32\packages.json"
# Expected: --output: Directory is not writable: C:\Windows\System32
```

---

## Complex Scenario Examples

### 1. Enterprise Deployment Scenario
```bash
# Step 1: Add company private source
winget source add --name "CompanyApps" --arg "https://packages.company.com" --trust-level trusted

# Step 2: Import standard company package list
winget import --import-file "company-standard.json" --accept-package-agreements --accept-source-agreements

# Step 3: Install specific tools with custom configurations
winget install --id Company.DevTools --scope machine --custom "/COMPANY_CONFIG=standard" --silent

# Step 4: Export current state for replication
winget export --output "deployed-config.json" --include-versions
```

### 2. Developer Environment Setup
```bash
# Install development tools with specific configurations
winget install --id Microsoft.VisualStudioCode --scope user --location "C:\Dev\Tools\VSCode"
winget install --id Git.Git --scope machine --custom "/COMPONENTS=""icons,icons\desktop"""
winget install --id Microsoft.WindowsTerminal --source msstore
winget install --id Docker.DockerDesktop --interactive

# Configure package pinning to prevent auto-updates during development
winget pin add --id Microsoft.VisualStudioCode --version "1.85.*"
winget pin add --id Docker.DockerDesktop --blocking
```

### 3. System Maintenance Scenario
```bash
# Check for available upgrades
winget list --upgrade-available

# Upgrade all packages except pinned ones
winget upgrade --all --include-unknown

# Repair problematic installations
winget repair --id Microsoft.VisualStudioCode --silent --log "C:\temp\repair.log"

# Clean up and export final state
winget export --output "post-maintenance.json" --include-versions --accept-source-agreements
```

### 4. Offline Package Distribution
```bash
# Download packages for offline installation
winget download --id Microsoft.VisualStudioCode --download-directory "\\server\packages\vscode"
winget download --id Git.Git --version 2.43.0 --download-directory "\\server\packages\git"

# Create hash for integrity verification
winget hash --file "\\server\packages\vscode\VSCodeUserSetup-x64-1.85.0.exe"

# Validate manifest files
winget validate --manifest "\\server\manifests\custom-app.yaml"
```

---

## Test Case Scenarios

### 1. Argument Parsing Test Cases
```csharp
public class ArgumentParsingTestCases
{
    [Test]
    public void InstallCommand_WithAllSearchParameters_ShouldSucceed()
    {
        var args = new[] { "install", "--query", "visual", "--id", "Microsoft.VisualStudioCode", "--name", "Visual Studio Code", "--exact" };
        var result = parser.Parse(args);
        Assert.IsTrue(result.IsValid);
        // Should prioritize --id over other search parameters
        Assert.AreEqual("Microsoft.VisualStudioCode", result.InstallOptions.Id);
    }

    [Test]
    public void ListCommand_WithConditionalOptions_ShouldValidateCorrectly()
    {
        // Valid case
        var validArgs = new[] { "list", "--upgrade-available", "--include-unknown" };
        var validResult = parser.Parse(validArgs);
        Assert.IsTrue(validResult.IsValid);

        // Invalid case
        var invalidArgs = new[] { "list", "--include-unknown" };
        var invalidResult = parser.Parse(invalidArgs);
        Assert.IsFalse(invalidResult.IsValid);
        Assert.Contains("upgrade-available", invalidResult.ErrorMessage);
    }
}
```

### 2. End-to-End Integration Test Cases
```csharp
public class IntegrationTestCases
{
    [Test]
    public async Task FullWorkflow_ExportImport_ShouldMaintainPackageList()
    {
        // Export current package list
        var exportResult = await client.ExportAsync(new ExportOptions { Output = "test-export.json" });
        Assert.IsTrue(exportResult.Success);

        // Import the exported list (should be no-op if packages already installed)
        var importResult = await client.ImportAsync(new ImportOptions { ImportFile = "test-export.json", IgnoreVersions = true });
        Assert.IsTrue(importResult.Success);
    }

    [Test]
    public async Task SourceManagement_AddListRemove_ShouldWorkCorrectly()
    {
        // Add test source
        var addResult = await client.SourceAddAsync(new SourceAddOptions 
        { 
            Name = "TestSource", 
            Arg = "https://test.repo.com" 
        });
        Assert.IsTrue(addResult.Success);

        // Verify source exists in list
        var listResult = await client.SourceListAsync();
        Assert.IsTrue(listResult.Any(s => s.Name == "TestSource"));

        // Remove test source
        var removeResult = await client.SourceRemoveAsync("TestSource");
        Assert.IsTrue(removeResult.Success);
    }
}
```

---

This comprehensive example collection provides thorough test coverage for validating .NET WinGet implementation compliance and serves as a reference for expected behavior in various usage scenarios.