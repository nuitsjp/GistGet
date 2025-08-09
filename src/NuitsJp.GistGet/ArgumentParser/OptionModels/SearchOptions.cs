namespace NuitsJp.GistGet.ArgumentParser.OptionModels;

/// <summary>
/// Represents options for the search command
/// Corresponds to winget search command options
/// </summary>
public class SearchOptions : BaseCommandOptions
{
    // Package identification options (mutually exclusive)
    public string? Query { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Moniker { get; set; }
    public string? Tag { get; set; }
    public string? Command { get; set; }

    // Search configuration
    public string? Source { get; set; }
    public int? Count { get; set; }
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
        if (!string.IsNullOrEmpty(Tag)) identifiers.Add(("tag", Tag));
        if (!string.IsNullOrEmpty(Command)) identifiers.Add(("command", Command));

        return identifiers.Count switch
        {
            0 => null,
            1 => identifiers[0],
            _ => throw new InvalidOperationException($"Multiple package identification options specified: {string.Join(", ", identifiers.Select(i => i.Type))}")
        };
    }

    /// <summary>
    /// Validates search options
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

        // Validate count value
        if (Count.HasValue && Count.Value <= 0)
        {
            errors.Add("Count must be a positive integer");
        }

        return errors;
    }
}