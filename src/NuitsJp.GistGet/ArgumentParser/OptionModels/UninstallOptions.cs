namespace NuitsJp.GistGet.ArgumentParser.OptionModels;

/// <summary>
/// Represents options for the uninstall command
/// Corresponds to winget uninstall command options
/// </summary>
public class UninstallOptions : BaseCommandOptions
{
    // Package identification options (mutually exclusive, at least one required)
    public string? Query { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Moniker { get; set; }

    // Uninstall configuration
    public new string? Version { get; set; } // Override base class Version
    public string? Source { get; set; }
    public string? Architecture { get; set; }
    public string? InstallerType { get; set; }
    public string? Locale { get; set; }

    // Uninstall behavior
    public bool Exact { get; set; }
    public bool Interactive { get; set; }
    public bool Silent { get; set; }
    public bool Force { get; set; }
    public bool Purge { get; set; }

    /// <summary>
    /// Gets the package identification option that was specified
    /// </summary>
    public (string Type, string Value)? GetPackageIdentification()
    {
        var identifiers = new List<(string Type, string Value)>();
        
        if (!string.IsNullOrEmpty(Query)) identifiers.Add(("query", Query));
        if (!string.IsNullOrEmpty(Id)) identifiers.Add(("id", Id));
        if (!string.IsNullOrEmpty(Name)) identifiers.Add(("name", Name));
        if (!string.IsNullOrEmpty(Moniker)) identifiers.Add(("moniker", Moniker));

        return identifiers.Count switch
        {
            0 => null,
            1 => identifiers[0],
            _ => throw new InvalidOperationException($"Multiple package identification options specified: {string.Join(", ", identifiers.Select(i => i.Type))}")
        };
    }

    /// <summary>
    /// Validates uninstall options
    /// </summary>
    public List<string> ValidateOptions()
    {
        var errors = new List<string>();

        // At least one package identification option is required
        var identification = GetPackageIdentification();
        if (identification == null)
        {
            errors.Add("Uninstall command requires at least one package identification option (--id, --name, --moniker, or --query)");
        }

        // Interactive and Silent are mutually exclusive
        if (Interactive && Silent)
        {
            errors.Add("Cannot specify both --interactive and --silent options");
        }

        // Validate architecture values
        if (!string.IsNullOrEmpty(Architecture) && !IsValidArchitecture(Architecture))
        {
            errors.Add($"Invalid architecture value: {Architecture}. Valid values are: x86, x64, arm, arm64");
        }

        return errors;
    }

    private static bool IsValidArchitecture(string architecture) =>
        new[] { "x86", "x64", "arm", "arm64" }
            .Contains(architecture, StringComparer.OrdinalIgnoreCase);
}