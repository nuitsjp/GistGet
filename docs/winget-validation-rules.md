# WinGet Validation Rules Specification

**Purpose**: Comprehensive validation rule definitions for .NET argument parser implementation with exact WinGet CLI compliance.

---

## Rule Categories

### 1. Syntax Validation Rules

#### Command Structure Validation
```csharp
public class CommandStructureRules
{
    // Rule CS001: Valid command identification
    public static ValidationResult ValidateCommand(string command)
    {
        var validCommands = new[]
        {
            "install", "add",                    // install aliases
            "list", "ls",                       // list aliases  
            "upgrade", "update",                // upgrade aliases
            "uninstall", "remove", "rm",        // uninstall aliases
            "search", "find",                   // search aliases
            "show", "view",                     // show aliases
            "repair", "fix",                    // repair aliases
            "configure", "configuration", "dsc", // configure aliases
            "source", "settings", "config", "export", "import", 
            "pin", "download", "hash", "validate", "features", "dscv3"
        };
        
        return validCommands.Contains(command.ToLowerInvariant()) 
            ? ValidationResult.Success 
            : ValidationResult.Error($"Unknown command: {command}");
    }

    // Rule CS002: Subcommand validation
    public static ValidationResult ValidateSubcommand(string command, string subcommand)
    {
        var validSubcommands = new Dictionary<string, string[]>
        {
            ["source"] = new[] { "add", "list", "update", "remove", "reset", "export" },
            ["settings"] = new[] { "export", "set", "reset" },
            ["pin"] = new[] { "add", "remove", "list", "reset" },
            ["configure"] = new[] { "show", "list", "test", "validate", "export" },
            ["dscv3"] = new[] { "package", "source", "user-settings-file", "admin-settings" }
        };
        
        if (!validSubcommands.ContainsKey(command))
            return ValidationResult.Success; // No subcommands for this command
            
        return validSubcommands[command].Contains(subcommand)
            ? ValidationResult.Success
            : ValidationResult.Error($"Invalid subcommand '{subcommand}' for '{command}'");
    }
}
```

### 2. Required Parameter Rules

#### Core Parameter Requirements
```csharp
public class RequiredParameterRules
{
    // Rule RP001: Install command requirements
    public static ValidationResult ValidateInstallRequirements(InstallOptions options)
    {
        var hasSearchParameter = !string.IsNullOrEmpty(options.Query) ||
                               !string.IsNullOrEmpty(options.Id) ||
                               !string.IsNullOrEmpty(options.Name) ||
                               !string.IsNullOrEmpty(options.Moniker);
                               
        var hasManifest = !string.IsNullOrEmpty(options.Manifest);
        
        if (!hasSearchParameter && !hasManifest)
        {
            return ValidationResult.Error(
                "At least one of --query, --id, --name, --moniker, or --manifest is required");
        }
        
        return ValidationResult.Success;
    }
    
    // Rule RP002: Source add requirements
    public static ValidationResult ValidateSourceAddRequirements(SourceAddOptions options)
    {
        if (string.IsNullOrEmpty(options.Name))
            return ValidationResult.Error("source add requires --name parameter");
            
        if (string.IsNullOrEmpty(options.Arg))
            return ValidationResult.Error("source add requires --arg parameter");
            
        return ValidationResult.Success;
    }
    
    // Rule RP003: File-based command requirements
    public static ValidationResult ValidateFileRequirements(string command, string filePath)
    {
        var fileRequiredCommands = new Dictionary<string, string>
        {
            ["hash"] = "--file",
            ["validate"] = "--manifest",
            ["import"] = "--import-file",
            ["export"] = "--output"
        };
        
        if (fileRequiredCommands.ContainsKey(command) && string.IsNullOrEmpty(filePath))
        {
            return ValidationResult.Error($"{command} command requires {fileRequiredCommands[command]} parameter");
        }
        
        return ValidationResult.Success;
    }
}
```

### 3. Conditional Dependency Rules

#### Upgrade-Available Dependencies
```csharp
public class ConditionalDependencyRules
{
    // Rule CD001: List command conditional options
    public static ValidationResult ValidateListConditionalOptions(ListOptions options)
    {
        if (options.IncludeUnknown && !options.UpgradeAvailable)
        {
            return ValidationResult.Error("引数 include-unknown は upgrade-available でのみ使用できます");
        }
        
        if (options.IncludePinned && !options.UpgradeAvailable)
        {
            return ValidationResult.Error("引数 include-pinned は upgrade-available でのみ使用できます");
        }
        
        return ValidationResult.Success;
    }
    
    // Rule CD002: Authentication dependencies
    public static ValidationResult ValidateAuthenticationDependencies(AuthenticationOptions options)
    {
        // Note: Both account without mode and mode without account are valid
        // This is a soft dependency for better UX, not a hard requirement
        
        if (!string.IsNullOrEmpty(options.Account) && string.IsNullOrEmpty(options.Mode))
        {
            return ValidationResult.Warning("--authentication-account is typically used with --authentication-mode");
        }
        
        return ValidationResult.Success;
    }
    
    // Rule CD003: Manifest file exclusions
    public static ValidationResult ValidateManifestExclusions(CommandOptions options)
    {
        if (!string.IsNullOrEmpty(options.Manifest))
        {
            // When manifest is specified, search parameters are ignored, not invalid
            if (!string.IsNullOrEmpty(options.Query) || 
                !string.IsNullOrEmpty(options.Id) || 
                !string.IsNullOrEmpty(options.Name) || 
                !string.IsNullOrEmpty(options.Moniker))
            {
                return ValidationResult.Warning("Search parameters are ignored when --manifest is specified");
            }
        }
        
        return ValidationResult.Success;
    }
}
```

### 4. Mutual Exclusivity Rules

#### Installation Mode Exclusions
```csharp
public class MutualExclusivityRules
{
    // Rule ME001: Installation mode exclusivity
    public static ValidationResult ValidateInstallationMode(InstallationOptions options)
    {
        if (options.Interactive && options.Silent)
        {
            return ValidationResult.Error("--interactive and --silent cannot be used together");
        }
        
        return ValidationResult.Success;
    }
    
    // Rule ME002: Scope value exclusivity
    public static ValidationResult ValidateScope(string scope)
    {
        if (!string.IsNullOrEmpty(scope))
        {
            var validScopes = new[] { "user", "machine" };
            if (!validScopes.Contains(scope.ToLowerInvariant()))
            {
                return ValidationResult.Error($"Invalid scope value: {scope}. Valid values are: user, machine");
            }
        }
        
        return ValidationResult.Success;
    }
    
    // Rule ME003: Portable package options
    public static ValidationResult ValidatePortableOptions(UninstallOptions options)
    {
        if (options.Purge && options.Preserve)
        {
            return ValidationResult.Error("--purge and --preserve cannot be used together");
        }
        
        return ValidationResult.Success;
    }
}
```

### 5. Value Constraint Rules

#### Numeric Range Validations
```csharp
public class ValueConstraintRules
{
    // Rule VC001: Count parameter validation
    public static ValidationResult ValidateCountParameter(int? count)
    {
        if (count.HasValue)
        {
            if (count.Value < 1 || count.Value > 1000)
            {
                return ValidationResult.Error("--count parameter must be between 1 and 1000");
            }
        }
        
        return ValidationResult.Success;
    }
    
    // Rule VC002: Trust level validation
    public static ValidationResult ValidateTrustLevel(string trustLevel)
    {
        if (!string.IsNullOrEmpty(trustLevel))
        {
            var validTrustLevels = new[] { "none", "trusted" };
            if (!validTrustLevels.Contains(trustLevel.ToLowerInvariant()))
            {
                return ValidationResult.Error($"Invalid trust level: {trustLevel}. Valid values are: none, trusted");
            }
        }
        
        return ValidationResult.Success;
    }
    
    // Rule VC003: Authentication mode validation
    public static ValidationResult ValidateAuthenticationMode(string mode)
    {
        if (!string.IsNullOrEmpty(mode))
        {
            var validModes = new[] { "silent", "silentpreferred", "interactive" };
            if (!validModes.Contains(mode.ToLowerInvariant()))
            {
                return ValidationResult.Error($"Invalid authentication mode: {mode}. Valid values are: silent, silentPreferred, interactive");
            }
        }
        
        return ValidationResult.Success;
    }
    
    // Rule VC004: BCP47 locale validation
    public static ValidationResult ValidateLocale(string locale)
    {
        if (!string.IsNullOrEmpty(locale))
        {
            try
            {
                var culture = new CultureInfo(locale);
                return ValidationResult.Success;
            }
            catch (CultureNotFoundException)
            {
                return ValidationResult.Error($"Invalid locale format: {locale}. Must be valid BCP47 format (e.g., en-US, ja-JP)");
            }
        }
        
        return ValidationResult.Success;
    }
}
```

### 6. File Path Validation Rules

#### Path Existence and Accessibility
```csharp
public class FilePathValidationRules
{
    // Rule FP001: Input file validation (must exist and be readable)
    public static ValidationResult ValidateInputFile(string filePath, string parameterName)
    {
        if (string.IsNullOrEmpty(filePath))
            return ValidationResult.Success;
            
        if (!File.Exists(filePath))
        {
            return ValidationResult.Error($"{parameterName}: File does not exist: {filePath}");
        }
        
        try
        {
            using (var stream = File.OpenRead(filePath))
            {
                // Test readability
            }
        }
        catch (UnauthorizedAccessException)
        {
            return ValidationResult.Error($"{parameterName}: File is not readable: {filePath}");
        }
        catch (Exception ex)
        {
            return ValidationResult.Error($"{parameterName}: Cannot access file {filePath}: {ex.Message}");
        }
        
        return ValidationResult.Success;
    }
    
    // Rule FP002: Output file validation (parent directory must exist and be writable)
    public static ValidationResult ValidateOutputFile(string filePath, string parameterName)
    {
        if (string.IsNullOrEmpty(filePath))
            return ValidationResult.Success;
            
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory))
            directory = Directory.GetCurrentDirectory();
            
        if (!Directory.Exists(directory))
        {
            return ValidationResult.Error($"{parameterName}: Directory does not exist: {directory}");
        }
        
        try
        {
            // Test writability by creating a temp file
            var testFile = Path.Combine(directory, Path.GetRandomFileName());
            File.WriteAllText(testFile, "");
            File.Delete(testFile);
        }
        catch (UnauthorizedAccessException)
        {
            return ValidationResult.Error($"{parameterName}: Directory is not writable: {directory}");
        }
        catch (Exception ex)
        {
            return ValidationResult.Error($"{parameterName}: Cannot write to directory {directory}: {ex.Message}");
        }
        
        return ValidationResult.Success;
    }
    
    // Rule FP003: Installation path validation
    public static ValidationResult ValidateInstallationPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return ValidationResult.Success;
            
        // Check if path is rooted (absolute)
        if (!Path.IsPathRooted(path))
        {
            return ValidationResult.Error($"Installation path must be absolute: {path}");
        }
        
        // Check for invalid characters
        var invalidChars = Path.GetInvalidPathChars();
        if (path.Any(c => invalidChars.Contains(c)))
        {
            return ValidationResult.Error($"Installation path contains invalid characters: {path}");
        }
        
        return ValidationResult.Success;
    }
}
```

### 7. Package-Specific Validation Rules

#### Dynamic Content Validation
```csharp
public class PackageSpecificRules
{
    // Rule PS001: Architecture availability validation
    public static async Task<ValidationResult> ValidateArchitectureAvailability(
        string packageId, string architecture, IWinGetClient client)
    {
        if (string.IsNullOrEmpty(architecture))
            return ValidationResult.Success;
            
        try
        {
            var packageInfo = await client.ShowAsync(new ShowOptions { Id = packageId });
            var availableArchitectures = packageInfo.AvailableArchitectures;
            
            if (availableArchitectures != null && 
                !availableArchitectures.Contains(architecture, StringComparer.OrdinalIgnoreCase))
            {
                return ValidationResult.Warning(
                    $"Architecture '{architecture}' may not be available for package '{packageId}'. " +
                    $"Available architectures: {string.Join(", ", availableArchitectures)}");
            }
        }
        catch (Exception ex)
        {
            // Don't fail validation due to package lookup errors
            return ValidationResult.Warning($"Could not validate architecture availability: {ex.Message}");
        }
        
        return ValidationResult.Success;
    }
    
    // Rule PS002: Version availability validation
    public static async Task<ValidationResult> ValidateVersionAvailability(
        string packageId, string version, IWinGetClient client)
    {
        if (string.IsNullOrEmpty(version))
            return ValidationResult.Success;
            
        try
        {
            var packageInfo = await client.ShowAsync(new ShowOptions { Id = packageId, Versions = true });
            var availableVersions = packageInfo.AvailableVersions;
            
            if (availableVersions != null && 
                !availableVersions.Contains(version, StringComparer.OrdinalIgnoreCase))
            {
                return ValidationResult.Warning(
                    $"Version '{version}' may not be available for package '{packageId}'. " +
                    $"Available versions: {string.Join(", ", availableVersions.Take(5))}...");
            }
        }
        catch (Exception ex)
        {
            return ValidationResult.Warning($"Could not validate version availability: {ex.Message}");
        }
        
        return ValidationResult.Success;
    }
}
```

### 8. Source-Specific Validation Rules

#### Source Configuration Validation
```csharp
public class SourceValidationRules
{
    // Rule SV001: Source existence validation
    public static async Task<ValidationResult> ValidateSourceExists(string sourceName, IWinGetClient client)
    {
        if (string.IsNullOrEmpty(sourceName))
            return ValidationResult.Success;
            
        try
        {
            var sources = await client.SourceListAsync();
            if (!sources.Any(s => s.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase)))
            {
                return ValidationResult.Error($"Source '{sourceName}' is not configured. Use 'winget source list' to see available sources.");
            }
        }
        catch (Exception ex)
        {
            return ValidationResult.Warning($"Could not validate source existence: {ex.Message}");
        }
        
        return ValidationResult.Success;
    }
    
    // Rule SV002: Source URL format validation
    public static ValidationResult ValidateSourceUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return ValidationResult.Success;
            
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return ValidationResult.Error($"Invalid source URL format: {url}");
        }
        
        if (uri.Scheme != "https" && uri.Scheme != "http" && uri.Scheme != "file")
        {
            return ValidationResult.Warning($"Unusual URL scheme for source: {uri.Scheme}. Typically https, http, or file.");
        }
        
        return ValidationResult.Success;
    }
}
```

---

## Validation Result Classes

### ValidationResult Implementation
```csharp
public class ValidationResult
{
    public bool IsValid { get; }
    public ValidationSeverity Severity { get; }
    public string Message { get; }
    public string RuleCode { get; }
    
    private ValidationResult(bool isValid, ValidationSeverity severity, string message, string ruleCode = null)
    {
        IsValid = isValid;
        Severity = severity;
        Message = message;
        RuleCode = ruleCode;
    }
    
    public static ValidationResult Success => new ValidationResult(true, ValidationSeverity.None, null);
    
    public static ValidationResult Error(string message, string ruleCode = null) =>
        new ValidationResult(false, ValidationSeverity.Error, message, ruleCode);
        
    public static ValidationResult Warning(string message, string ruleCode = null) =>
        new ValidationResult(true, ValidationSeverity.Warning, message, ruleCode);
}

public enum ValidationSeverity
{
    None,
    Warning,
    Error
}
```

### Validation Engine Integration
```csharp
public class WinGetValidationEngine
{
    public async Task<ValidationResult> ValidateCommandAsync(ParsedCommand command)
    {
        var results = new List<ValidationResult>();
        
        // Apply all relevant validation rules
        results.Add(CommandStructureRules.ValidateCommand(command.Command));
        
        if (command.Subcommand != null)
            results.Add(CommandStructureRules.ValidateSubcommand(command.Command, command.Subcommand));
            
        // Apply command-specific rules based on command type
        switch (command.Command.ToLowerInvariant())
        {
            case "install":
            case "add":
                results.Add(RequiredParameterRules.ValidateInstallRequirements(command.InstallOptions));
                results.Add(MutualExclusivityRules.ValidateInstallationMode(command.InstallOptions));
                break;
                
            case "list":
            case "ls":
                results.Add(ConditionalDependencyRules.ValidateListConditionalOptions(command.ListOptions));
                break;
                
            // Add other command-specific validations...
        }
        
        // Combine results and return aggregate
        var hasErrors = results.Any(r => !r.IsValid);
        var hasWarnings = results.Any(r => r.Severity == ValidationSeverity.Warning);
        
        if (hasErrors)
        {
            var errorMessages = results.Where(r => !r.IsValid).Select(r => r.Message);
            return ValidationResult.Error(string.Join(Environment.NewLine, errorMessages));
        }
        
        if (hasWarnings)
        {
            var warningMessages = results.Where(r => r.Severity == ValidationSeverity.Warning).Select(r => r.Message);
            return ValidationResult.Warning(string.Join(Environment.NewLine, warningMessages));
        }
        
        return ValidationResult.Success;
    }
}
```

---

These validation rules provide comprehensive coverage for implementing WinGet-compliant argument validation in the .NET parser, ensuring exact behavioral compatibility with the official WinGet CLI.