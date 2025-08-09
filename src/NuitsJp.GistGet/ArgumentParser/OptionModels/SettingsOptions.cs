namespace NuitsJp.GistGet.ArgumentParser.OptionModels;

/// <summary>
/// Represents options for the settings command and its subcommands
/// Corresponds to winget settings command options
/// </summary>
public class SettingsOptions : BaseCommandOptions
{
    public SettingsSubCommand SubCommand { get; set; }
    
    // For settings export subcommand
    public string? Output { get; set; } // File path for export
    
    // For settings set subcommand
    public string? Name { get; set; } // Setting name
    public string? Value { get; set; } // Setting value
    
    // For settings reset subcommand
    // Uses Name property (optional - if not specified, resets all)

    /// <summary>
    /// Validates settings options based on the subcommand
    /// </summary>
    public List<string> ValidateOptions()
    {
        var errors = new List<string>();

        switch (SubCommand)
        {
            case SettingsSubCommand.Export:
                // Output path is optional for export - defaults to stdout
                break;
                
            case SettingsSubCommand.Set:
                if (string.IsNullOrEmpty(Name))
                {
                    errors.Add("settings set requires --name option");
                }
                if (string.IsNullOrEmpty(Value))
                {
                    errors.Add("settings set requires --value option");
                }
                break;
                
            case SettingsSubCommand.Reset:
                // Name is optional for reset - if not specified, resets all settings
                break;
        }

        return errors;
    }
}

/// <summary>
/// Enumeration of settings subcommands
/// </summary>
public enum SettingsSubCommand
{
    Export,
    Set,
    Reset
}