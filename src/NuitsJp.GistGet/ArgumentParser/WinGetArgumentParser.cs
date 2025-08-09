using System.CommandLine;
using System.CommandLine.Parsing;
using NuitsJp.GistGet.Commands;

namespace NuitsJp.GistGet.ArgumentParser;

/// <summary>
/// WinGet-compatible argument parser implementation using System.CommandLine
/// Provides complete winget.exe command structure and validation
/// </summary>
public class WinGetArgumentParser : IWinGetArgumentParser
{
    public RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("WinGet-compatible package manager with GitHub Gist synchronization")
        {
            Name = "gistget"
        };

        // Add global options that apply to all commands
        AddGlobalOptions(rootCommand);

        // Add primary commands
        rootCommand.AddCommand(BuildInstallCommand());
        rootCommand.AddCommand(BuildListCommand());
        rootCommand.AddCommand(BuildUpgradeCommand());
        rootCommand.AddCommand(BuildUninstallCommand());
        rootCommand.AddCommand(BuildSearchCommand());
        rootCommand.AddCommand(BuildShowCommand());
        rootCommand.AddCommand(BuildSourceCommand());
        rootCommand.AddCommand(BuildSettingsCommand());
        rootCommand.AddCommand(BuildExportCommand());
        rootCommand.AddCommand(BuildImportCommand());
        rootCommand.AddCommand(BuildPinCommand());
        rootCommand.AddCommand(BuildConfigureCommand());
        rootCommand.AddCommand(BuildDownloadCommand());
        rootCommand.AddCommand(BuildRepairCommand());
        rootCommand.AddCommand(BuildHashCommand());
        rootCommand.AddCommand(BuildValidateCommand());
        rootCommand.AddCommand(BuildFeaturesCommand());
        rootCommand.AddCommand(BuildDscv3Command());

        return rootCommand;
    }

    public ValidationResult ValidateArguments(ParseResult parseResult)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Implement comprehensive validation logic based on winget specifications
        ValidateMutualExclusivity(parseResult, errors);
        ValidateConditionalRequirements(parseResult, errors);
        ValidateOptionCombinations(parseResult, errors, warnings);

        if (errors.Count > 0)
        {
            return ValidationResult.Failure(errors).WithWarnings(warnings);
        }

        return ValidationResult.Success().WithWarnings(warnings);
    }

    private void AddGlobalOptions(RootCommand rootCommand)
    {
        // Use WinGet-specific global options that don't conflict with command-specific options
        rootCommand.AddGlobalOption(new Option<bool>(
            aliases: new[] { "--info" },
            description: "Show general info"));

        rootCommand.AddGlobalOption(new Option<bool>(
            aliases: new[] { "--wait" },
            description: "Prompts the user to press any key before exiting"));

        rootCommand.AddGlobalOption(new Option<bool>(
            aliases: new[] { "--logs", "--open-logs" },
            description: "Open the default logs location"));

        rootCommand.AddGlobalOption(new Option<bool>(
            aliases: new[] { "--verbose", "--verbose-logs" },
            description: "Enables verbose logging"));

        rootCommand.AddGlobalOption(new Option<bool>(
            aliases: new[] { "--nowarn", "--ignore-warnings" },
            description: "Ignores warning messages"));

        rootCommand.AddGlobalOption(new Option<bool>(
            aliases: new[] { "--disable-interactivity" },
            description: "Disable interactive prompts"));

        rootCommand.AddGlobalOption(new Option<string?>(
            aliases: new[] { "--proxy" },
            description: "Set proxy to use for requests"));

        rootCommand.AddGlobalOption(new Option<bool>(
            aliases: new[] { "--no-proxy" },
            description: "Disable proxy usage"));
    }

    private Command BuildInstallCommand()
    {
        var installCommand = new Command("install", "Installs packages")
        {
            Handler = new InstallCommandHandler()
        };
        
        // Add install command aliases
        installCommand.AddAlias("add");

        // Package identification options (mutually exclusive)
        installCommand.AddOption(new Option<string?>(
            aliases: new[] { "-q", "--query" },
            description: "Use the given query to search"));

        installCommand.AddOption(new Option<string?>(
            aliases: new[] { "--id" },
            description: "Filter results by id"));

        installCommand.AddOption(new Option<string?>(
            aliases: new[] { "--name" },
            description: "Filter results by name"));

        installCommand.AddOption(new Option<string?>(
            aliases: new[] { "-m", "--moniker" },
            description: "Filter results by moniker"));

        // Installation options
        installCommand.AddOption(new Option<string?>(
            aliases: new[] { "-v", "--version" },
            description: "Use the specified version"));

        installCommand.AddOption(new Option<string?>(
            aliases: new[] { "-s", "--source" },
            description: "Find package using the specified source"));

        installCommand.AddOption(new Option<string?>(
            aliases: new[] { "--scope" },
            description: "Select install scope (user or machine)"));

        installCommand.AddOption(new Option<bool>(
            aliases: new[] { "-e", "--exact" },
            description: "Find package using exact match"));

        installCommand.AddOption(new Option<bool>(
            aliases: new[] { "-i", "--interactive" },
            description: "Request interactive installation"));

        installCommand.AddOption(new Option<bool>(
            aliases: new[] { "-h", "--silent" },
            description: "Request silent installation"));

        installCommand.AddOption(new Option<string?>(
            aliases: new[] { "--locale" },
            description: "Locale to use"));

        installCommand.AddOption(new Option<string?>(
            aliases: new[] { "-l", "--location" },
            description: "Location to install to"));

        installCommand.AddOption(new Option<bool>(
            aliases: new[] { "--override" },
            description: "Override arguments to be passed to installer"));

        installCommand.AddOption(new Option<bool>(
            aliases: new[] { "--force" },
            description: "Override installer hash check"));

        installCommand.AddOption(new Option<string?>(
            aliases: new[] { "--architecture", "--arch" },
            description: "Select the architecture"));

        installCommand.AddOption(new Option<string?>(
            aliases: new[] { "--installer-type" },
            description: "Select the installer type"));

        installCommand.AddOption(new Option<bool>(
            aliases: new[] { "--accept-package-agreements" },
            description: "Accept package agreements"));

        installCommand.AddOption(new Option<bool>(
            aliases: new[] { "--accept-source-agreements" },
            description: "Accept source agreements"));

        return installCommand;
    }

    private Command BuildListCommand()
    {
        var listCommand = new Command("list", "Display installed packages")
        {
            Handler = new ListCommandHandler()
        };

        // Add list command alias
        listCommand.AddAlias("ls");

        // Filtering options
        listCommand.AddOption(new Option<string?>(
            aliases: new[] { "-q", "--query" },
            description: "Use the given query to search"));

        listCommand.AddOption(new Option<string?>(
            aliases: new[] { "--id" },
            description: "Filter results by id"));

        listCommand.AddOption(new Option<string?>(
            aliases: new[] { "--name" },
            description: "Filter results by name"));

        listCommand.AddOption(new Option<string?>(
            aliases: new[] { "-m", "--moniker" },
            description: "Filter results by moniker"));

        listCommand.AddOption(new Option<string?>(
            aliases: new[] { "-s", "--source" },
            description: "Filter results by source"));

        listCommand.AddOption(new Option<string?>(
            aliases: new[] { "--tag" },
            description: "Filter results by tag"));

        listCommand.AddOption(new Option<bool>(
            aliases: new[] { "-e", "--exact" },
            description: "Find package using exact match"));

        listCommand.AddOption(new Option<bool>(
            aliases: new[] { "--upgrade-available" },
            description: "Filter by packages with upgrades available"));

        listCommand.AddOption(new Option<bool>(
            aliases: new[] { "--include-unknown" },
            description: "Include packages with unknown versions"));

        return listCommand;
    }

    private Command BuildUpgradeCommand()
    {
        var upgradeCommand = new Command("upgrade", "Upgrades packages")
        {
            Handler = new UpgradeCommandHandler()
        };

        // Add upgrade command alias
        upgradeCommand.AddAlias("update");

        // Copy most options from install command since upgrade has similar options
        // Package identification options
        upgradeCommand.AddOption(new Option<string?>(
            aliases: new[] { "-q", "--query" },
            description: "Use the given query to search"));

        upgradeCommand.AddOption(new Option<string?>(
            aliases: new[] { "--id" },
            description: "Filter results by id"));

        upgradeCommand.AddOption(new Option<string?>(
            aliases: new[] { "--name" },
            description: "Filter results by name"));

        upgradeCommand.AddOption(new Option<string?>(
            aliases: new[] { "-m", "--moniker" },
            description: "Filter results by moniker"));

        // Upgrade-specific options
        upgradeCommand.AddOption(new Option<bool>(
            aliases: new[] { "--all" },
            description: "Upgrade all packages"));

        upgradeCommand.AddOption(new Option<bool>(
            aliases: new[] { "--include-unknown" },
            description: "Include packages with unknown versions"));

        // Installation options
        upgradeCommand.AddOption(new Option<string?>(
            aliases: new[] { "-v", "--version" },
            description: "Use the specified version"));

        upgradeCommand.AddOption(new Option<string?>(
            aliases: new[] { "-s", "--source" },
            description: "Find package using the specified source"));

        upgradeCommand.AddOption(new Option<bool>(
            aliases: new[] { "-e", "--exact" },
            description: "Find package using exact match"));

        upgradeCommand.AddOption(new Option<bool>(
            aliases: new[] { "-i", "--interactive" },
            description: "Request interactive installation"));

        upgradeCommand.AddOption(new Option<bool>(
            aliases: new[] { "-h", "--silent" },
            description: "Request silent installation"));

        upgradeCommand.AddOption(new Option<bool>(
            aliases: new[] { "--force" },
            description: "Override installer hash check"));

        upgradeCommand.AddOption(new Option<bool>(
            aliases: new[] { "--accept-package-agreements" },
            description: "Accept package agreements"));

        upgradeCommand.AddOption(new Option<bool>(
            aliases: new[] { "--accept-source-agreements" },
            description: "Accept source agreements"));

        return upgradeCommand;
    }

    // Placeholder implementations for remaining commands
    private Command BuildUninstallCommand() => new Command("uninstall", "Uninstall packages") { Handler = new UninstallCommandHandler() };
    private Command BuildSearchCommand() => new Command("search", "Search for packages") { Handler = new SearchCommandHandler() };
    private Command BuildShowCommand() => new Command("show", "Show package information") { Handler = new ShowCommandHandler() };
    private Command BuildSourceCommand() => new Command("source", "Manage package sources") { Handler = new SourceCommandHandler() };
    private Command BuildSettingsCommand() => new Command("settings", "Manage settings") { Handler = new SettingsCommandHandler() };
    private Command BuildExportCommand() => new Command("export", "Export package list") { Handler = new ExportCommandHandler() };
    private Command BuildImportCommand() => new Command("import", "Import package list") { Handler = new ImportCommandHandler() };
    private Command BuildPinCommand() => new Command("pin", "Manage package pins") { Handler = new PinCommandHandler() };
    private Command BuildConfigureCommand() => new Command("configure", "Configure system") { Handler = new ConfigureCommandHandler() };
    private Command BuildDownloadCommand() => new Command("download", "Download packages") { Handler = new DownloadCommandHandler() };
    private Command BuildRepairCommand() => new Command("repair", "Repair packages") { Handler = new RepairCommandHandler() };
    private Command BuildHashCommand() => new Command("hash", "Calculate file hash") { Handler = new HashCommandHandler() };
    private Command BuildValidateCommand() => new Command("validate", "Validate manifests") { Handler = new ValidateCommandHandler() };
    private Command BuildFeaturesCommand() => new Command("features", "Manage experimental features") { Handler = new FeaturesCommandHandler() };
    private Command BuildDscv3Command() => new Command("dscv3", "DSC v3 resources") { Handler = new Dscv3CommandHandler() };

    private void ValidateMutualExclusivity(ParseResult parseResult, List<string> errors)
    {
        // Implement mutual exclusivity validation
        // Example: --id, --name, --moniker, --query are mutually exclusive for install/upgrade
        
        var command = parseResult.CommandResult.Command;
        if (command.Name == "install" || command.Name == "upgrade" || command.Name == "add" || command.Name == "update")
        {
            var mutuallyExclusiveOptions = new[] { "--id", "--name", "--moniker", "--query" };
            var presentOptions = mutuallyExclusiveOptions
                .Where(opt => parseResult.HasOption(opt))
                .ToList();

            if (presentOptions.Count > 1)
            {
                errors.Add($"The following options are mutually exclusive: {string.Join(", ", presentOptions)}");
            }
        }
    }

    private void ValidateConditionalRequirements(ParseResult parseResult, List<string> errors)
    {
        // Implement conditional requirement validation
        // Example: --include-unknown requires --upgrade-available for list command
        
        var command = parseResult.CommandResult.Command;
        if (command.Name == "list" || command.Name == "ls")
        {
            if (parseResult.HasOption("--include-unknown") && !parseResult.HasOption("--upgrade-available"))
            {
                errors.Add("--include-unknown option requires --upgrade-available");
            }
        }
    }

    private void ValidateOptionCombinations(ParseResult parseResult, List<string> errors, List<string> warnings)
    {
        // Implement validation for option combinations that are invalid or produce warnings
        
        var command = parseResult.CommandResult.Command;
        if (command.Name == "install" || command.Name == "add")
        {
            // Warning for potentially conflicting installation options
            if (parseResult.HasOption("--interactive") && parseResult.HasOption("--silent"))
            {
                warnings.Add("Both --interactive and --silent specified; --interactive takes precedence");
            }
        }
    }
}

// Extension methods for ParseResult
public static class ParseResultExtensions
{
    public static bool HasOption(this ParseResult parseResult, string alias)
    {
        return parseResult.CommandResult.Children
            .OfType<OptionResult>()
            .Any(o => o.Option.Aliases.Contains(alias));
    }
}