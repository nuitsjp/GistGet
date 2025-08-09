namespace NuitsJp.GistGet.ArgumentParser.OptionModels;

/// <summary>
/// Represents options for the list command
/// Corresponds to winget list command options
/// </summary>
public class ListOptions : BaseCommandOptions
{
    // Package identification options (mutually exclusive)
    public string? Query { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Moniker { get; set; }
    
    // Filtering options
    public string? Source { get; set; }
    public string? Tag { get; set; }
    
    // Display options
    public bool Exact { get; set; }
    public bool UpgradeAvailable { get; set; }
    public bool IncludeUnknown { get; set; }

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
    /// Validates the combination of list options
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

        // --include-unknown requires --upgrade-available
        if (IncludeUnknown && !UpgradeAvailable)
        {
            errors.Add("--include-unknown option requires --upgrade-available");
        }

        return errors;
    }
}