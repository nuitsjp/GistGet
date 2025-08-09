using System.CommandLine;
using System.CommandLine.Parsing;
using NuitsJp.GistGet.ArgumentParser.OptionModels;

namespace NuitsJp.GistGet.ArgumentParser;

/// <summary>
/// Advanced validation engine for WinGet command arguments
/// Handles complex dependency relationships and mutual exclusivity rules
/// </summary>
public class ValidationEngine
{
    /// <summary>
    /// Validates parsed arguments according to WinGet specifications
    /// </summary>
    public ValidationResult ValidateCommand(ParseResult parseResult)
    {
        var command = parseResult.CommandResult.Command;
        var errors = new List<string>();
        var warnings = new List<string>();

        // Apply command-specific validation
        switch (command.Name.ToLowerInvariant())
        {
            case "install" or "add":
                ValidateInstallCommand(parseResult, errors, warnings);
                break;
            case "list" or "ls":
                ValidateListCommand(parseResult, errors, warnings);
                break;
            case "upgrade" or "update":
                ValidateUpgradeCommand(parseResult, errors, warnings);
                break;
            case "uninstall" or "remove" or "rm":
                ValidateUninstallCommand(parseResult, errors, warnings);
                break;
            case "search" or "find":
                ValidateSearchCommand(parseResult, errors, warnings);
                break;
            case "show" or "view":
                ValidateShowCommand(parseResult, errors, warnings);
                break;
            case "export":
                ValidateExportCommand(parseResult, errors, warnings);
                break;
            case "import":
                ValidateImportCommand(parseResult, errors, warnings);
                break;
            case "source":
                ValidateSourceCommand(parseResult, errors, warnings);
                break;
            case "settings":
                ValidateSettingsCommand(parseResult, errors, warnings);
                break;
        }

        // Apply global validation rules
        ValidateGlobalOptions(parseResult, errors, warnings);

        if (errors.Count > 0)
        {
            return ValidationResult.Failure(errors).WithWarnings(warnings);
        }

        return ValidationResult.Success().WithWarnings(warnings);
    }

    private void ValidateInstallCommand(ParseResult parseResult, List<string> errors, List<string> warnings)
    {
        // Package identification mutual exclusivity
        ValidateMutualExclusivity(parseResult, ValidationRules.PackageIdentificationOptions, 
            "Package identification options are mutually exclusive", errors);

        // Interactive vs Silent warning
        ValidateWarningCombinations(parseResult, warnings);

        // Scope validation
        ValidateOptionValue(parseResult, "--scope", errors);

        // Architecture validation
        ValidateOptionValue(parseResult, "--architecture", errors);
        ValidateOptionValue(parseResult, "--arch", errors);
    }

    private void ValidateListCommand(ParseResult parseResult, List<string> errors, List<string> warnings)
    {
        // Package identification mutual exclusivity
        var identificationOptions = new[] { "--query", "-q", "--id", "--name", "--moniker", "-m" };
        var presentIdentifiers = identificationOptions
            .Where(opt => parseResult.HasOption(opt))
            .ToList();

        if (presentIdentifiers.Count > 1)
        {
            errors.Add($"Package identification options are mutually exclusive: {string.Join(", ", presentIdentifiers)}");
        }

        // --include-unknown requires --upgrade-available
        if (parseResult.HasOption("--include-unknown") && !parseResult.HasOption("--upgrade-available"))
        {
            errors.Add("--include-unknown option requires --upgrade-available");
        }
    }

    private void ValidateUpgradeCommand(ParseResult parseResult, List<string> errors, List<string> warnings)
    {
        var hasAll = parseResult.HasOption("--all");
        
        if (hasAll)
        {
            // When --all is specified, individual package identification is not allowed
            var identificationOptions = new[] { "--query", "-q", "--id", "--name", "--moniker", "-m" };
            var presentIdentifiers = identificationOptions
                .Where(opt => parseResult.HasOption(opt))
                .ToList();

            if (presentIdentifiers.Count > 0)
            {
                errors.Add($"Cannot specify individual package identification options with --all: {string.Join(", ", presentIdentifiers)}");
            }
        }
        else
        {
            // Without --all, check mutual exclusivity of identification options
            var identificationOptions = new[] { "--query", "-q", "--id", "--name", "--moniker", "-m" };
            var presentIdentifiers = identificationOptions
                .Where(opt => parseResult.HasOption(opt))
                .ToList();

            if (presentIdentifiers.Count > 1)
            {
                errors.Add($"Package identification options are mutually exclusive: {string.Join(", ", presentIdentifiers)}");
            }
        }

        // Interactive vs Silent
        if (parseResult.HasOption("--interactive") && parseResult.HasOption("--silent"))
        {
            warnings.Add("Both --interactive and --silent specified; --interactive takes precedence");
        }
    }

    private void ValidateUninstallCommand(ParseResult parseResult, List<string> errors, List<string> warnings)
    {
        // At least one package identification option is required
        var identificationOptions = new[] { "--query", "-q", "--id", "--name", "--moniker", "-m" };
        var presentIdentifiers = identificationOptions
            .Where(opt => parseResult.HasOption(opt))
            .ToList();

        if (presentIdentifiers.Count == 0)
        {
            errors.Add("Uninstall command requires at least one package identification option");
        }
        else if (presentIdentifiers.Count > 1)
        {
            errors.Add($"Package identification options are mutually exclusive: {string.Join(", ", presentIdentifiers)}");
        }

        // Interactive vs Silent
        if (parseResult.HasOption("--interactive") && parseResult.HasOption("--silent"))
        {
            warnings.Add("Both --interactive and --silent specified; --interactive takes precedence");
        }
    }

    private void ValidateSearchCommand(ParseResult parseResult, List<string> errors, List<string> warnings)
    {
        // Package identification mutual exclusivity
        var identificationOptions = new[] { "--query", "-q", "--id", "--name", "--moniker", "-m", "--tag", "--command" };
        var presentIdentifiers = identificationOptions
            .Where(opt => parseResult.HasOption(opt))
            .ToList();

        if (presentIdentifiers.Count > 1)
        {
            errors.Add($"Package identification options are mutually exclusive: {string.Join(", ", presentIdentifiers)}");
        }

        // Count validation
        var countValue = parseResult.GetValueForOption<int?>("--count");
        if (countValue.HasValue && countValue.Value <= 0)
        {
            errors.Add("Count must be a positive integer");
        }
    }

    private void ValidateShowCommand(ParseResult parseResult, List<string> errors, List<string> warnings)
    {
        // At least one package identification option is required
        var identificationOptions = new[] { "--query", "-q", "--id", "--name", "--moniker", "-m" };
        var presentIdentifiers = identificationOptions
            .Where(opt => parseResult.HasOption(opt))
            .ToList();

        if (presentIdentifiers.Count == 0)
        {
            errors.Add("Show command requires at least one package identification option");
        }
        else if (presentIdentifiers.Count > 1)
        {
            errors.Add($"Package identification options are mutually exclusive: {string.Join(", ", presentIdentifiers)}");
        }
    }

    private void ValidateExportCommand(ParseResult parseResult, List<string> errors, List<string> warnings)
    {
        // Output file is required
        var outputValue = parseResult.GetValueForOption<string>("--output") ?? 
                         parseResult.GetValueForOption<string>("-o");
        if (string.IsNullOrEmpty(outputValue))
        {
            errors.Add("Export command requires --output option to specify the output file path");
        }
    }

    private void ValidateImportCommand(ParseResult parseResult, List<string> errors, List<string> warnings)
    {
        // Import file is required
        var importFileValue = parseResult.GetValueForOption<string>("--import-file") ?? 
                             parseResult.GetValueForOption<string>("-i");
        if (string.IsNullOrEmpty(importFileValue))
        {
            errors.Add("Import command requires --import-file option to specify the input file path");
        }
        else if (!File.Exists(importFileValue))
        {
            errors.Add($"Import file does not exist: {importFileValue}");
        }
    }

    private void ValidateSourceCommand(ParseResult parseResult, List<string> errors, List<string> warnings)
    {
        // Source command validation is complex due to subcommands
        // This would need to be expanded based on the actual subcommand structure
        // For now, basic validation placeholder
    }

    private void ValidateSettingsCommand(ParseResult parseResult, List<string> errors, List<string> warnings)
    {
        // Settings command validation is complex due to subcommands
        // This would need to be expanded based on the actual subcommand structure
        // For now, basic validation placeholder
    }

    private void ValidateGlobalOptions(ParseResult parseResult, List<string> errors, List<string> warnings)
    {
        // Proxy options are mutually exclusive
        ValidateMutualExclusivity(parseResult, ValidationRules.ProxyOptions.Concat(ValidationRules.NoProxyOptions).ToArray(),
            "Cannot specify both --proxy and --no-proxy options", errors);
    }

    #region Helper Methods

    /// <summary>
    /// Validates mutual exclusivity of specified options
    /// </summary>
    private void ValidateMutualExclusivity(ParseResult parseResult, string[] options, string errorMessage, List<string> errors)
    {
        var presentOptions = options.Where(opt => parseResult.HasOption(opt)).ToList();
        if (presentOptions.Count > 1)
        {
            errors.Add($"{errorMessage}: {string.Join(", ", presentOptions)}");
        }
    }

    /// <summary>
    /// Validates that required options are present for the command
    /// </summary>
    private void ValidateRequiredOptions(ParseResult parseResult, string commandName, List<string> errors)
    {
        if (ValidationRules.CommandRequiredOptions.TryGetValue(commandName, out var requiredOptions))
        {
            foreach (var optionGroup in requiredOptions)
            {
                var groupOptions = optionGroup.Split('|'); // Support OR groups like "--output|-o"
                var hasAny = groupOptions.Any(opt => parseResult.HasOption(opt));
                
                if (!hasAny)
                {
                    var optionDisplay = groupOptions.Length == 1 ? groupOptions[0] : $"one of: {string.Join(", ", groupOptions)}";
                    errors.Add($"{commandName} command requires {optionDisplay}");
                }
            }
        }
    }

    /// <summary>
    /// Validates option values against allowed values
    /// </summary>
    private void ValidateOptionValue(ParseResult parseResult, string optionName, List<string> errors)
    {
        var value = parseResult.GetValueForOption<string>(optionName);
        if (!string.IsNullOrEmpty(value) && !ValidationRules.IsValidValue(optionName, value))
        {
            var validValues = ValidationRules.GetValidValues(optionName);
            errors.Add($"Invalid {optionName} value: {value}. Valid values are: {string.Join(", ", validValues)}");
        }
    }

    /// <summary>
    /// Validates warning combinations
    /// </summary>
    private void ValidateWarningCombinations(ParseResult parseResult, List<string> warnings)
    {
        foreach (var (options, warning) in ValidationRules.WarningCombinations)
        {
            if (options.All(opt => parseResult.HasOption(opt)))
            {
                warnings.Add(warning);
            }
        }
    }

    /// <summary>
    /// Validates conditional requirements
    /// </summary>
    private void ValidateConditionalRequirements(ParseResult parseResult, string commandName, List<string> errors)
    {
        if (ValidationRules.CommandConditionalRequirements.TryGetValue(commandName, out var requirements))
        {
            foreach (var (requiredOption, dependentOption) in requirements)
            {
                if (parseResult.HasOption(requiredOption) && !parseResult.HasOption(dependentOption))
                {
                    errors.Add($"{requiredOption} option requires {dependentOption}");
                }
            }
        }
    }

    /// <summary>
    /// Validates numeric option values
    /// </summary>
    private void ValidateNumericValues(ParseResult parseResult, List<string> errors)
    {
        foreach (var (optionName, minValue) in ValidationRules.MinimumValues)
        {
            var value = parseResult.GetValueForOption<int?>(optionName);
            if (value.HasValue && value.Value < minValue)
            {
                errors.Add($"{optionName} must be at least {minValue}");
            }
        }
    }

    /// <summary>
    /// Validates file existence for file-based options
    /// </summary>
    private void ValidateFileExistence(ParseResult parseResult, List<string> errors)
    {
        foreach (var optionName in ValidationRules.FileExistenceOptions)
        {
            var filePath = parseResult.GetValueForOption<string>(optionName);
            if (!string.IsNullOrEmpty(filePath) && !File.Exists(filePath))
            {
                errors.Add($"File does not exist: {filePath}");
            }
        }
    }

    #endregion
}

/// <summary>
/// Extension methods for ParseResult to simplify option checking
/// </summary>
public static class ParseResultExtensions
{
    public static bool HasOption(this ParseResult parseResult, string alias)
    {
        return parseResult.CommandResult.Children
            .OfType<OptionResult>()
            .Any(o => o.Option.Aliases.Contains(alias));
    }

    public static T? GetValueForOption<T>(this ParseResult parseResult, string alias)
    {
        var optionResult = parseResult.CommandResult.Children
            .OfType<OptionResult>()
            .FirstOrDefault(o => o.Option.Aliases.Contains(alias));

        return optionResult != null ? optionResult.GetValueOrDefault<T>() : default;
    }
}