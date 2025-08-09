namespace NuitsJp.GistGet.ArgumentParser;

/// <summary>
/// Static class containing WinGet validation rule definitions
/// Based on winget.exe behavior analysis from docs/winget-validation-rules.md
/// </summary>
public static class ValidationRules
{
    /// <summary>
    /// Package identification options that are mutually exclusive
    /// </summary>
    public static readonly string[] PackageIdentificationOptions = 
    {
        "--query", "-q", "--id", "--name", "--moniker", "-m"
    };

    /// <summary>
    /// Search-specific identification options (includes additional options)
    /// </summary>
    public static readonly string[] SearchIdentificationOptions = 
    {
        "--query", "-q", "--id", "--name", "--moniker", "-m", "--tag", "--command"
    };

    /// <summary>
    /// Valid scope values for install/upgrade operations
    /// </summary>
    public static readonly string[] ValidScopes = { "user", "machine" };

    /// <summary>
    /// Valid architecture values
    /// </summary>
    public static readonly string[] ValidArchitectures = { "x86", "x64", "arm", "arm64" };

    /// <summary>
    /// Valid installer types
    /// </summary>
    public static readonly string[] ValidInstallerTypes = 
    {
        "msi", "exe", "msix", "inno", "nullsoft", "wix", "burn", "pwa", "appx"
    };

    /// <summary>
    /// Valid trust levels for source operations
    /// </summary>
    public static readonly string[] ValidTrustLevels = { "trusted", "none" };

    /// <summary>
    /// Commands that require at least one package identification option
    /// </summary>
    public static readonly string[] CommandsRequiringPackageId = 
    {
        "show", "view", "uninstall", "remove", "rm"
    };

    /// <summary>
    /// Commands that support the --all option
    /// </summary>
    public static readonly string[] CommandsSupportingAll = 
    {
        "upgrade", "update"
    };

    /// <summary>
    /// Options that are mutually exclusive with --all
    /// </summary>
    public static readonly string[] OptionsExclusiveWithAll = 
    {
        "--query", "-q", "--id", "--name", "--moniker", "-m"
    };

    /// <summary>
    /// Interactive and silent options (mutually exclusive)
    /// </summary>
    public static readonly string[] InteractiveOptions = { "--interactive", "-i" };
    public static readonly string[] SilentOptions = { "--silent", "-h" };

    /// <summary>
    /// Proxy configuration options (mutually exclusive)
    /// </summary>
    public static readonly string[] ProxyOptions = { "--proxy" };
    public static readonly string[] NoProxyOptions = { "--no-proxy" };

    /// <summary>
    /// Commands that support version specification
    /// </summary>
    public static readonly string[] CommandsSupportingVersion = 
    {
        "install", "add", "upgrade", "update", "show", "view", "uninstall", "remove", "rm"
    };

    /// <summary>
    /// Commands that support exact match option
    /// </summary>
    public static readonly string[] CommandsSupportingExact = 
    {
        "install", "add", "upgrade", "update", "search", "find", "show", "view", 
        "uninstall", "remove", "rm", "list", "ls"
    };

    /// <summary>
    /// Conditional requirements: option -> required option
    /// </summary>
    public static readonly Dictionary<string, string> ConditionalRequirements = new()
    {
        { "--include-unknown", "--upgrade-available" }
    };

    /// <summary>
    /// Command-specific conditional requirements
    /// Key: command name, Value: dictionary of conditional requirements
    /// </summary>
    public static readonly Dictionary<string, Dictionary<string, string>> CommandConditionalRequirements = new()
    {
        {
            "list", new Dictionary<string, string>
            {
                { "--include-unknown", "--upgrade-available" }
            }
        },
        {
            "ls", new Dictionary<string, string>
            {
                { "--include-unknown", "--upgrade-available" }
            }
        }
    };

    /// <summary>
    /// Options that generate warnings when used together
    /// </summary>
    public static readonly List<(string[] Options, string Warning)> WarningCombinations = new()
    {
        (new[] { "--interactive", "--silent" }, "Both --interactive and --silent specified; --interactive takes precedence"),
        (new[] { "-i", "--silent" }, "Both --interactive and --silent specified; --interactive takes precedence"),
        (new[] { "--interactive", "-h" }, "Both --interactive and --silent specified; --interactive takes precedence"),
        (new[] { "-i", "-h" }, "Both --interactive and --silent specified; --interactive takes precedence")
    };

    /// <summary>
    /// Minimum values for numeric options
    /// </summary>
    public static readonly Dictionary<string, int> MinimumValues = new()
    {
        { "--count", 1 }
    };

    /// <summary>
    /// File-based options that require file existence
    /// </summary>
    public static readonly string[] FileExistenceOptions = 
    {
        "--import-file", "-i"
    };

    /// <summary>
    /// Required options for specific commands
    /// </summary>
    public static readonly Dictionary<string, string[]> CommandRequiredOptions = new()
    {
        {
            "export", new[] { "--output|--output" }
        },
        {
            "import", new[] { "--import-file|--import-file" }
        }
    };

    /// <summary>
    /// Checks if an option value is valid for the given constraint
    /// </summary>
    public static bool IsValidValue(string optionName, string value)
    {
        return optionName switch
        {
            "--scope" => ValidScopes.Contains(value, StringComparer.OrdinalIgnoreCase),
            "--architecture" or "--arch" => ValidArchitectures.Contains(value, StringComparer.OrdinalIgnoreCase),
            "--installer-type" => ValidInstallerTypes.Contains(value, StringComparer.OrdinalIgnoreCase),
            "--trust-level" => ValidTrustLevels.Contains(value, StringComparer.OrdinalIgnoreCase),
            _ => true // Default: all values are valid
        };
    }

    /// <summary>
    /// Gets the valid values for a specific option
    /// </summary>
    public static string[] GetValidValues(string optionName)
    {
        return optionName switch
        {
            "--scope" => ValidScopes,
            "--architecture" or "--arch" => ValidArchitectures,
            "--installer-type" => ValidInstallerTypes,
            "--trust-level" => ValidTrustLevels,
            _ => Array.Empty<string>()
        };
    }
}