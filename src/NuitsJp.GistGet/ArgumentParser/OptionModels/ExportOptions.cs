namespace NuitsJp.GistGet.ArgumentParser.OptionModels;

/// <summary>
/// Represents options for the export command
/// Corresponds to winget export command options
/// </summary>
public class ExportOptions : BaseCommandOptions
{
    public string? Output { get; set; } // Output file path
    public string? Source { get; set; } // Source to export from
    public bool IncludeVersions { get; set; } // Include version information
    public bool AcceptSourceAgreements { get; set; }

    /// <summary>
    /// Validates export options
    /// </summary>
    public List<string> ValidateOptions()
    {
        var errors = new List<string>();

        // Output path is required for export command
        if (string.IsNullOrEmpty(Output))
        {
            errors.Add("Export command requires --output option to specify the output file path");
        }

        return errors;
    }
}

/// <summary>
/// Represents options for the import command
/// Corresponds to winget import command options
/// </summary>
public class ImportOptions : BaseCommandOptions
{
    public string? ImportFile { get; set; } // Input file path
    public bool IgnoreUnavailable { get; set; } // Continue import even if some packages are unavailable
    public bool IgnoreVersions { get; set; } // Ignore version specifications in import file
    public bool AcceptPackageAgreements { get; set; }
    public bool AcceptSourceAgreements { get; set; }

    /// <summary>
    /// Validates import options
    /// </summary>
    public List<string> ValidateOptions()
    {
        var errors = new List<string>();

        // Import file path is required for import command
        if (string.IsNullOrEmpty(ImportFile))
        {
            errors.Add("Import command requires --import-file option to specify the input file path");
        }
        else if (!File.Exists(ImportFile))
        {
            errors.Add($"Import file does not exist: {ImportFile}");
        }

        return errors;
    }
}