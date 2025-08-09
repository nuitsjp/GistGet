namespace NuitsJp.GistGet.ArgumentParser.OptionModels;

/// <summary>
/// Represents options for the install command
/// Corresponds to winget install command options
/// </summary>
public class InstallOptions : BaseCommandOptions
{
    // Package identification options (mutually exclusive)
    public string? Query { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Moniker { get; set; }

    // Installation configuration
    public new string? Version { get; set; }
    public string? Source { get; set; }
    public string? Scope { get; set; } // user or machine
    public string? Architecture { get; set; } // x86, x64, arm, arm64
    public string? InstallerType { get; set; } // msi, exe, msix, etc.
    public string? Locale { get; set; }
    public string? Location { get; set; }

    // Installation behavior
    public bool Exact { get; set; }
    public bool Interactive { get; set; }
    public bool Silent { get; set; }
    public bool Override { get; set; }
    public bool Force { get; set; }

    // Agreements
    public bool AcceptPackageAgreements { get; set; }
    public bool AcceptSourceAgreements { get; set; }

    /// <summary>
    /// Gets the package identification option that was specified
    /// Returns null if none specified, throws if multiple specified
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
    /// Validates the combination of installation options
    /// </summary>
    public List<string> ValidateOptions()
    {
        var errors = new List<string>();

        // Check mutually exclusive identification options
        try
        {
            GetPackageIdentification();
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }

        // Validate scope values
        if (!string.IsNullOrEmpty(Scope) && !IsValidScope(Scope))
        {
            errors.Add($"Invalid scope value: {Scope}. Valid values are: user, machine");
        }

        // Validate architecture values
        if (!string.IsNullOrEmpty(Architecture) && !IsValidArchitecture(Architecture))
        {
            errors.Add($"Invalid architecture value: {Architecture}. Valid values are: x86, x64, arm, arm64");
        }

        // Interactive and Silent are mutually exclusive
        if (Interactive && Silent)
        {
            errors.Add("Cannot specify both --interactive and --silent options");
        }

        return errors;
    }

    private static bool IsValidScope(string scope) => 
        scope.Equals("user", StringComparison.OrdinalIgnoreCase) || 
        scope.Equals("machine", StringComparison.OrdinalIgnoreCase);

    private static bool IsValidArchitecture(string architecture) =>
        new[] { "x86", "x64", "arm", "arm64" }
            .Contains(architecture, StringComparer.OrdinalIgnoreCase);
}