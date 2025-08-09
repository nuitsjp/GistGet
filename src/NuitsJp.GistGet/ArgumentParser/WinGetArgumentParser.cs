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
    private readonly ValidationEngine _validationEngine = new();
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
        // Delegate to the comprehensive validation engine
        return _validationEngine.ValidateCommand(parseResult);
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

    // Placeholder implementations for basic commands
    private Command BuildUninstallCommand() => new Command("uninstall", "Uninstall packages") { Handler = new UninstallCommandHandler() };
    private Command BuildSearchCommand() => new Command("search", "Search for packages") { Handler = new SearchCommandHandler() };
    private Command BuildShowCommand() => new Command("show", "Show package information") { Handler = new ShowCommandHandler() };
    
    private Command BuildSourceCommand()
    {
        var sourceCommand = new Command("source", "Manage package sources");

        // Add source subcommands
        sourceCommand.AddCommand(BuildSourceAddCommand());
        sourceCommand.AddCommand(BuildSourceListCommand());
        sourceCommand.AddCommand(BuildSourceUpdateCommand());
        sourceCommand.AddCommand(BuildSourceRemoveCommand());
        sourceCommand.AddCommand(BuildSourceResetCommand());
        sourceCommand.AddCommand(BuildSourceExportCommand());

        return sourceCommand;
    }

    private Command BuildSettingsCommand()
    {
        var settingsCommand = new Command("settings", "Manage settings");

        // Add settings subcommands
        settingsCommand.AddCommand(BuildSettingsExportCommand());
        settingsCommand.AddCommand(BuildSettingsSetCommand());
        settingsCommand.AddCommand(BuildSettingsResetCommand());

        return settingsCommand;
    }
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

    #region Source Subcommands

    private Command BuildSourceAddCommand()
    {
        var addCommand = new Command("add", "Add a new source")
        {
            Handler = new SourceAddCommandHandler()
        };

        addCommand.AddOption(new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Name to be given to the source") { IsRequired = true });

        addCommand.AddOption(new Option<string>(
            aliases: new[] { "--arg", "-a" },
            description: "URL or path to the source") { IsRequired = true });

        addCommand.AddOption(new Option<string>(
            aliases: new[] { "--type", "-t" },
            description: "Type of source"));

        addCommand.AddOption(new Option<string>(
            aliases: new[] { "--trust-level" },
            description: "Trust level of the source"));

        return addCommand;
    }

    private Command BuildSourceListCommand()
    {
        return new Command("list", "List configured sources")
        {
            Handler = new SourceListCommandHandler()
        };
    }

    private Command BuildSourceUpdateCommand()
    {
        var updateCommand = new Command("update", "Update source(s)")
        {
            Handler = new SourceUpdateCommandHandler()
        };

        updateCommand.AddOption(new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Name of the source to update"));

        return updateCommand;
    }

    private Command BuildSourceRemoveCommand()
    {
        var removeCommand = new Command("remove", "Remove a source")
        {
            Handler = new SourceRemoveCommandHandler()
        };

        removeCommand.AddOption(new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Name of the source to remove") { IsRequired = true });

        return removeCommand;
    }

    private Command BuildSourceResetCommand()
    {
        var resetCommand = new Command("reset", "Reset sources to default")
        {
            Handler = new SourceResetCommandHandler()
        };

        resetCommand.AddOption(new Option<bool>(
            aliases: new[] { "--force" },
            description: "Reset without confirmation"));

        return resetCommand;
    }

    private Command BuildSourceExportCommand()
    {
        return new Command("export", "Export configured sources")
        {
            Handler = new SourceExportCommandHandler()
        };
    }

    #endregion

    #region Settings Subcommands

    private Command BuildSettingsExportCommand()
    {
        var exportCommand = new Command("export", "Export current settings")
        {
            Handler = new SettingsExportCommandHandler()
        };

        exportCommand.AddOption(new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output file path"));

        return exportCommand;
    }

    private Command BuildSettingsSetCommand()
    {
        var setCommand = new Command("set", "Set a configuration setting")
        {
            Handler = new SettingsSetCommandHandler()
        };

        setCommand.AddOption(new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Name of the setting") { IsRequired = true });

        setCommand.AddOption(new Option<string>(
            aliases: new[] { "--value", "-v" },
            description: "Value of the setting") { IsRequired = true });

        return setCommand;
    }

    private Command BuildSettingsResetCommand()
    {
        var resetCommand = new Command("reset", "Reset settings to default")
        {
            Handler = new SettingsResetCommandHandler()
        };

        resetCommand.AddOption(new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Name of the setting to reset"));

        return resetCommand;
    }

    #endregion

}