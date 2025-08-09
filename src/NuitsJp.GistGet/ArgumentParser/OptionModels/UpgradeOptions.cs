namespace NuitsJp.GistGet.ArgumentParser.OptionModels;

/// <summary>
/// Represents options for the upgrade command
/// Corresponds to winget upgrade command options
/// </summary>
public class UpgradeOptions : BaseCommandOptions
{
    // Package identification options (mutually exclusive, unless --all is specified)
    public string? Query { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Moniker { get; set; }

    // Upgrade configuration
    public new string? Version { get; set; } // Override base class Version
    public string? Source { get; set; }
    public string? Architecture { get; set; }
    public string? InstallerType { get; set; }
    public string? Locale { get; set; }

    // Upgrade behavior
    public bool All { get; set; }
    public bool IncludeUnknown { get; set; }
    public bool Exact { get; set; }
    public bool Interactive { get; set; }
    public bool Silent { get; set; }
    public bool Force { get; set; }

    // Agreements
    public bool AcceptPackageAgreements { get; set; }
    public bool AcceptSourceAgreements { get; set; }

    /// <summary>
    /// Gets the package identification option that was specified
    /// Returns null if none specified or if --all is specified
    /// </summary>
    public (string Type, string Value)? GetPackageIdentification()
    {
        if (All)
        {
            return null; // --all overrides individual package identification
        }

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
    /// Validates the combination of upgrade options
    /// </summary>
    public List<string> ValidateOptions()
    {
        var errors = new List<string>();

        // When --all is specified, individual package identification is not allowed
        if (All)
        {
            var hasIndividualIdentification = !string.IsNullOrEmpty(Query) ||
                                            !string.IsNullOrEmpty(Id) ||
                                            !string.IsNullOrEmpty(Name) ||
                                            !string.IsNullOrEmpty(Moniker);
            
            if (hasIndividualIdentification)
            {
                errors.Add("Cannot specify individual package identification options with --all");
            }
        }
        else
        {
            // Check mutually exclusive identification options when --all is not specified
            try
            {
                GetPackageIdentification();
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(ex.Message);
            }
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