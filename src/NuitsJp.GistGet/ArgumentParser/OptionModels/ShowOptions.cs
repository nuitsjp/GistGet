namespace NuitsJp.GistGet.ArgumentParser.OptionModels;

/// <summary>
/// Represents options for the show command
/// Corresponds to winget show command options
/// </summary>
public class ShowOptions : BaseCommandOptions
{
    // Package identification options (mutually exclusive, at least one required)
    public string? Query { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Moniker { get; set; }

    // Show configuration
    public string? Source { get; set; }
    public new string? Version { get; set; } // Override base class Version
    public bool Exact { get; set; }

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
    /// Validates show options
    /// </summary>
    public List<string> ValidateOptions()
    {
        var errors = new List<string>();

        // At least one package identification option is required for show command
        var identification = GetPackageIdentification();
        if (identification == null)
        {
            errors.Add("Show command requires at least one package identification option (--id, --name, --moniker, or --query)");
        }

        return errors;
    }
}