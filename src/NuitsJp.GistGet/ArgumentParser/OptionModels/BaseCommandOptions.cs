namespace NuitsJp.GistGet.ArgumentParser.OptionModels;

/// <summary>
/// Base class for all command option models
/// Contains global options that apply to all commands
/// </summary>
public abstract class BaseCommandOptions
{
    // Global options available to all commands
    public bool Info { get; set; }
    public bool Wait { get; set; }
    public bool Logs { get; set; }
    public bool Verbose { get; set; }
    public bool IgnoreWarnings { get; set; }
    public bool DisableInteractivity { get; set; }
    public string? Proxy { get; set; }
    public bool NoProxy { get; set; }
    public bool Help { get; set; }
    public bool Version { get; set; }

    /// <summary>
    /// Validates global options that apply to all commands
    /// </summary>
    public virtual List<string> ValidateGlobalOptions()
    {
        var errors = new List<string>();

        // Proxy options are mutually exclusive
        if (!string.IsNullOrEmpty(Proxy) && NoProxy)
        {
            errors.Add("Cannot specify both --proxy and --no-proxy options");
        }

        return errors;
    }
}