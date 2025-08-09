namespace NuitsJp.GistGet.ArgumentParser.OptionModels;

/// <summary>
/// Represents options for the source command and its subcommands
/// Corresponds to winget source command options
/// </summary>
public class SourceOptions : BaseCommandOptions
{
    public SourceSubCommand SubCommand { get; set; }
    
    // For source add subcommand
    public string? Name { get; set; }
    public string? Arg { get; set; } // URL or path
    public string? Type { get; set; } // Microsoft.PreIndexed.Package, etc.
    public string? TrustLevel { get; set; } // trusted, none
    
    // For source remove subcommand
    // Uses Name property
    
    // For source update subcommand
    // Uses Name property (optional - if not specified, updates all)
    
    // For source reset subcommand
    public bool Force { get; set; }
    
    // For source export subcommand
    // No additional options

    /// <summary>
    /// Validates source options based on the subcommand
    /// </summary>
    public List<string> ValidateOptions()
    {
        var errors = new List<string>();

        switch (SubCommand)
        {
            case SourceSubCommand.Add:
                if (string.IsNullOrEmpty(Name))
                {
                    errors.Add("source add requires --name option");
                }
                if (string.IsNullOrEmpty(Arg))
                {
                    errors.Add("source add requires --arg option (URL or path)");
                }
                if (!string.IsNullOrEmpty(TrustLevel) && !IsValidTrustLevel(TrustLevel))
                {
                    errors.Add($"Invalid trust level: {TrustLevel}. Valid values are: trusted, none");
                }
                break;
                
            case SourceSubCommand.Remove:
                if (string.IsNullOrEmpty(Name))
                {
                    errors.Add("source remove requires --name option");
                }
                break;
                
            case SourceSubCommand.Update:
                // Name is optional for update - if not specified, updates all sources
                break;
                
            case SourceSubCommand.Reset:
                // No required options for reset, but --force can be used
                break;
                
            case SourceSubCommand.List:
            case SourceSubCommand.Export:
                // No required options for list or export
                break;
        }

        return errors;
    }

    private static bool IsValidTrustLevel(string trustLevel) =>
        new[] { "trusted", "none" }.Contains(trustLevel, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Enumeration of source subcommands
/// </summary>
public enum SourceSubCommand
{
    Add,
    List,
    Update,
    Remove,
    Reset,
    Export
}