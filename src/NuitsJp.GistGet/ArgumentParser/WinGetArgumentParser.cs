using System.CommandLine;
using NuitsJp.GistGet.Commands;

namespace NuitsJp.GistGet.ArgumentParser;

/// <summary>
/// WinGet-compatible argument parser implementation using System.CommandLine
/// Provides complete winget.exe command structure and validation
/// </summary>
public class WinGetArgumentParser : IWinGetArgumentParser
{
    private readonly ValidationEngine _validationEngine = new();
    public Command BuildRootCommand()
    {
        // Use a named root command (Command) to allow Name to be "gistget" for tests
        var rootCommand = new Command("gistget", "WinGet-compatible package manager with GitHub Gist synchronization");

        // Add global options that apply to all commands
        AddGlobalOptions(rootCommand);

        // Add primary commands
        rootCommand.Subcommands.Add(BuildInstallCommand());
        rootCommand.Subcommands.Add(BuildListCommand());
        rootCommand.Subcommands.Add(BuildUpgradeCommand());
        rootCommand.Subcommands.Add(BuildUninstallCommand());
        rootCommand.Subcommands.Add(BuildSearchCommand());
        rootCommand.Subcommands.Add(BuildShowCommand());
        rootCommand.Subcommands.Add(BuildSourceCommand());
        rootCommand.Subcommands.Add(BuildSettingsCommand());
        rootCommand.Subcommands.Add(BuildExportCommand());
        rootCommand.Subcommands.Add(BuildImportCommand());
        rootCommand.Subcommands.Add(BuildPinCommand());
        rootCommand.Subcommands.Add(BuildConfigureCommand());
        rootCommand.Subcommands.Add(BuildDownloadCommand());
        rootCommand.Subcommands.Add(BuildRepairCommand());
        rootCommand.Subcommands.Add(BuildHashCommand());
        rootCommand.Subcommands.Add(BuildValidateCommand());
        rootCommand.Subcommands.Add(BuildFeaturesCommand());
    rootCommand.Subcommands.Add(BuildDscv3Command());

        return rootCommand;
    }

    public ValidationResult ValidateArguments(ParseResult parseResult)
    {
        // Delegate to the comprehensive validation engine
        return _validationEngine.ValidateCommand(parseResult);
    }

    private void AddGlobalOptions(Command rootCommand)
    {
        // Define options with bare names, add typical aliases starting with dashes
        var info = new Option<bool>("info") { Description = "Show general info" };
        info.Aliases.Add("--info");
        rootCommand.Options.Add(info);

        var wait = new Option<bool>("wait") { Description = "Prompts the user to press any key before exiting" };
        wait.Aliases.Add("--wait");
        rootCommand.Options.Add(wait);

        var logs = new Option<bool>("logs") { Description = "Open the default logs location" };
        logs.Aliases.Add("--logs");
        rootCommand.Options.Add(logs);

        var openLogs = new Option<bool>("open-logs") { Description = "Open the default logs location" };
        openLogs.Aliases.Add("--open-logs");
        rootCommand.Options.Add(openLogs);

        var verbose = new Option<bool>("verbose") { Description = "Enables verbose logging" };
        verbose.Aliases.Add("--verbose");
        rootCommand.Options.Add(verbose);

        var verboseLogs = new Option<bool>("verbose-logs") { Description = "Enables verbose logging" };
        verboseLogs.Aliases.Add("--verbose-logs");
        rootCommand.Options.Add(verboseLogs);

        var nowarn = new Option<bool>("nowarn") { Description = "Ignores warning messages" };
        nowarn.Aliases.Add("--nowarn");
        rootCommand.Options.Add(nowarn);

        var ignoreWarnings = new Option<bool>("ignore-warnings") { Description = "Ignores warning messages" };
        ignoreWarnings.Aliases.Add("--ignore-warnings");
        rootCommand.Options.Add(ignoreWarnings);

        var disableInteractivity = new Option<bool>("disable-interactivity") { Description = "Disable interactive prompts" };
        disableInteractivity.Aliases.Add("--disable-interactivity");
        rootCommand.Options.Add(disableInteractivity);

        var proxy = new Option<string?>("proxy") { Description = "Set proxy to use for requests" };
        proxy.Aliases.Add("--proxy");
        rootCommand.Options.Add(proxy);

        var noProxy = new Option<bool>("no-proxy") { Description = "Disable proxy usage" };
        noProxy.Aliases.Add("--no-proxy");
        rootCommand.Options.Add(noProxy);
    }

    private Command BuildInstallCommand()
    {
        var installCommand = new Command("install", "Installs packages");
        installCommand.SetAction(pr => new InstallCommandHandler().ExecuteAsync());
        
        // Add install command aliases
        installCommand.Aliases.Add("add");

        // Package identification options (mutually exclusive)
        installCommand.Options.Add(new Option<string?>("--query", "-q") { Description = "Use the given query to search" });

        installCommand.Options.Add(new Option<string?>("--id") { Description = "Filter results by id" });

        installCommand.Options.Add(new Option<string?>("--name") { Description = "Filter results by name" });

        installCommand.Options.Add(new Option<string?>("--moniker", "-m") { Description = "Filter results by moniker" });

        // Installation options
        installCommand.Options.Add(new Option<string?>("--version", "-v") { Description = "Use the specified version" });

        installCommand.Options.Add(new Option<string?>("--source", "-s") { Description = "Find package using the specified source" });

        installCommand.Options.Add(new Option<string?>("--scope") { Description = "Select install scope (user or machine)" });

        installCommand.Options.Add(new Option<bool>("--exact", "-e") { Description = "Find package using exact match" });

        installCommand.Options.Add(new Option<bool>("--interactive", "-i") { Description = "Request interactive installation" });

        installCommand.Options.Add(new Option<bool>("--silent", "-h") { Description = "Request silent installation" });

        installCommand.Options.Add(new Option<string?>("--locale") { Description = "Locale to use" });

        installCommand.Options.Add(new Option<string?>("--location", "-l") { Description = "Location to install to" });

        installCommand.Options.Add(new Option<bool>("--override") { Description = "Override arguments to be passed to installer" });

        installCommand.Options.Add(new Option<bool>("--force") { Description = "Override installer hash check" });

        installCommand.Options.Add(new Option<string?>("--architecture", "--arch") { Description = "Select the architecture" });

        installCommand.Options.Add(new Option<string?>("--installer-type") { Description = "Select the installer type" });

        installCommand.Options.Add(new Option<bool>("--accept-package-agreements") { Description = "Accept package agreements" });

        installCommand.Options.Add(new Option<bool>("--accept-source-agreements") { Description = "Accept source agreements" });

        return installCommand;
    }

    private Command BuildListCommand()
    {
        var listCommand = new Command("list", "Display installed packages");
        listCommand.SetAction(pr => new ListCommandHandler().ExecuteAsync());

        // Add list command alias
        listCommand.Aliases.Add("ls");

        // Filtering options
        listCommand.Options.Add(new Option<string?>("--query", "-q") { Description = "Use the given query to search" });

        listCommand.Options.Add(new Option<string?>("--id") { Description = "Filter results by id" });

        listCommand.Options.Add(new Option<string?>("--name") { Description = "Filter results by name" });

        listCommand.Options.Add(new Option<string?>("--moniker", "-m") { Description = "Filter results by moniker" });

        listCommand.Options.Add(new Option<string?>("--source", "-s") { Description = "Filter results by source" });

        listCommand.Options.Add(new Option<string?>("--tag") { Description = "Filter results by tag" });

        listCommand.Options.Add(new Option<bool>("--exact", "-e") { Description = "Find package using exact match" });

        listCommand.Options.Add(new Option<bool>("--upgrade-available") { Description = "Filter by packages with upgrades available" });

        listCommand.Options.Add(new Option<bool>("--include-unknown") { Description = "Include packages with unknown versions" });

        return listCommand;
    }

    private Command BuildUpgradeCommand()
    {
        var upgradeCommand = new Command("upgrade", "Upgrades packages");
        upgradeCommand.SetAction(pr => new UpgradeCommandHandler().ExecuteAsync());

        // Add upgrade command alias
        upgradeCommand.Aliases.Add("update");

        // Copy most options from install command since upgrade has similar options
        // Package identification options
        upgradeCommand.Options.Add(new Option<string?>("--query", "-q") { Description = "Use the given query to search" });

        upgradeCommand.Options.Add(new Option<string?>("--id") { Description = "Filter results by id" });

        upgradeCommand.Options.Add(new Option<string?>("--name") { Description = "Filter results by name" });

        upgradeCommand.Options.Add(new Option<string?>("--moniker", "-m") { Description = "Filter results by moniker" });

        // Upgrade-specific options
        upgradeCommand.Options.Add(new Option<bool>("--all") { Description = "Upgrade all packages" });

        upgradeCommand.Options.Add(new Option<bool>("--include-unknown") { Description = "Include packages with unknown versions" });

        // Installation options
        upgradeCommand.Options.Add(new Option<string?>("--version", "-v") { Description = "Use the specified version" });

        upgradeCommand.Options.Add(new Option<string?>("--source", "-s") { Description = "Find package using the specified source" });

        upgradeCommand.Options.Add(new Option<bool>("--exact", "-e") { Description = "Find package using exact match" });

        upgradeCommand.Options.Add(new Option<bool>("--interactive", "-i") { Description = "Request interactive installation" });

        upgradeCommand.Options.Add(new Option<bool>("--silent", "-h") { Description = "Request silent installation" });

        upgradeCommand.Options.Add(new Option<bool>("--force") { Description = "Override installer hash check" });

        upgradeCommand.Options.Add(new Option<bool>("--accept-package-agreements") { Description = "Accept package agreements" });

        upgradeCommand.Options.Add(new Option<bool>("--accept-source-agreements") { Description = "Accept source agreements" });

        return upgradeCommand;
    }

    // Placeholder implementations for basic commands
    private Command BuildUninstallCommand() { var c = new Command("uninstall", "Uninstall packages"); c.SetAction(pr => new UninstallCommandHandler().ExecuteAsync()); return c; }
    private Command BuildSearchCommand() { var c = new Command("search", "Search for packages"); c.SetAction(pr => new SearchCommandHandler().ExecuteAsync()); return c; }
    private Command BuildShowCommand() { var c = new Command("show", "Show package information"); c.SetAction(pr => new ShowCommandHandler().ExecuteAsync()); return c; }
    
    private Command BuildSourceCommand()
    {
        var sourceCommand = new Command("source", "Manage package sources");

        // Add source subcommands
        sourceCommand.Subcommands.Add(BuildSourceAddCommand());
        sourceCommand.Subcommands.Add(BuildSourceListCommand());
        sourceCommand.Subcommands.Add(BuildSourceUpdateCommand());
        sourceCommand.Subcommands.Add(BuildSourceRemoveCommand());
        sourceCommand.Subcommands.Add(BuildSourceResetCommand());
        sourceCommand.Subcommands.Add(BuildSourceExportCommand());

        return sourceCommand;
    }

    private Command BuildSettingsCommand()
    {
        var settingsCommand = new Command("settings", "Manage settings");

        // Add settings subcommands
        settingsCommand.Subcommands.Add(BuildSettingsExportCommand());
        settingsCommand.Subcommands.Add(BuildSettingsSetCommand());
        settingsCommand.Subcommands.Add(BuildSettingsResetCommand());

        return settingsCommand;
    }
    private Command BuildExportCommand() { var c = new Command("export", "Export package list"); c.SetAction(pr => new ExportCommandHandler().ExecuteAsync()); return c; }
    private Command BuildImportCommand() { var c = new Command("import", "Import package list"); c.SetAction(pr => new ImportCommandHandler().ExecuteAsync()); return c; }
    private Command BuildPinCommand() { var c = new Command("pin", "Manage package pins"); c.SetAction(pr => new PinCommandHandler().ExecuteAsync()); return c; }
    private Command BuildConfigureCommand() { var c = new Command("configure", "Configure system"); c.SetAction(pr => new ConfigureCommandHandler().ExecuteAsync()); return c; }
    private Command BuildDownloadCommand() { var c = new Command("download", "Download packages"); c.SetAction(pr => new DownloadCommandHandler().ExecuteAsync()); return c; }
    private Command BuildRepairCommand() { var c = new Command("repair", "Repair packages"); c.SetAction(pr => new RepairCommandHandler().ExecuteAsync()); return c; }
    private Command BuildHashCommand() { var c = new Command("hash", "Calculate file hash"); c.SetAction(pr => new HashCommandHandler().ExecuteAsync()); return c; }
    private Command BuildValidateCommand() { var c = new Command("validate", "Validate manifests"); c.SetAction(pr => new ValidateCommandHandler().ExecuteAsync()); return c; }
    private Command BuildFeaturesCommand() { var c = new Command("features", "Manage experimental features"); c.SetAction(pr => new FeaturesCommandHandler().ExecuteAsync()); return c; }
    private Command BuildDscv3Command() { var c = new Command("dscv3", "DSC v3 resources"); c.SetAction(pr => new Dscv3CommandHandler().ExecuteAsync()); return c; }

    #region Source Subcommands

    private Command BuildSourceAddCommand()
    {
        var addCommand = new Command("add", "Add a new source");
        addCommand.SetAction(pr => new SourceAddCommandHandler().ExecuteAsync());

        addCommand.Options.Add(new Option<string>("--name", "-n") { Description = "Name to be given to the source", Required = true });

        addCommand.Options.Add(new Option<string>("--arg", "-a") { Description = "URL or path to the source", Required = true });

        addCommand.Options.Add(new Option<string>("--type", "-t") { Description = "Type of source" });

        addCommand.Options.Add(new Option<string>("--trust-level") { Description = "Trust level of the source" });

        return addCommand;
    }

    private Command BuildSourceListCommand()
    {
        var list = new Command("list", "List configured sources");
        list.SetAction(pr => new SourceListCommandHandler().ExecuteAsync());
        return list;
    }

    private Command BuildSourceUpdateCommand()
    {
        var updateCommand = new Command("update", "Update source(s)");
        updateCommand.SetAction(pr => new SourceUpdateCommandHandler().ExecuteAsync());

        updateCommand.Options.Add(new Option<string>("--name", "-n") { Description = "Name of the source to update" });

        return updateCommand;
    }

    private Command BuildSourceRemoveCommand()
    {
        var removeCommand = new Command("remove", "Remove a source");
        removeCommand.SetAction(pr => new SourceRemoveCommandHandler().ExecuteAsync());

        removeCommand.Options.Add(new Option<string>("--name", "-n") { Description = "Name of the source to remove", Required = true });

        return removeCommand;
    }

    private Command BuildSourceResetCommand()
    {
        var resetCommand = new Command("reset", "Reset sources to default");
        resetCommand.SetAction(pr => new SourceResetCommandHandler().ExecuteAsync());

        resetCommand.Options.Add(new Option<bool>("--force") { Description = "Reset without confirmation" });

        return resetCommand;
    }

    private Command BuildSourceExportCommand()
    {
        var export = new Command("export", "Export configured sources");
        export.SetAction(pr => new SourceExportCommandHandler().ExecuteAsync());
        return export;
    }

    #endregion

    #region Settings Subcommands

    private Command BuildSettingsExportCommand()
    {
        var exportCommand = new Command("export", "Export current settings");
        exportCommand.SetAction(pr => new SettingsExportCommandHandler().ExecuteAsync());

        exportCommand.Options.Add(new Option<string>("--output", "-o") { Description = "Output file path" });

        return exportCommand;
    }

    private Command BuildSettingsSetCommand()
    {
        var setCommand = new Command("set", "Set a configuration setting");
        setCommand.SetAction(pr => new SettingsSetCommandHandler().ExecuteAsync());

        setCommand.Options.Add(new Option<string>("--name", "-n") { Description = "Name of the setting", Required = true });

        setCommand.Options.Add(new Option<string>("--value", "-v") { Description = "Value of the setting", Required = true });

        return setCommand;
    }

    private Command BuildSettingsResetCommand()
    {
        var resetCommand = new Command("reset", "Reset settings to default");
        resetCommand.SetAction(pr => new SettingsResetCommandHandler().ExecuteAsync());

        resetCommand.Options.Add(new Option<string>("--name", "-n") { Description = "Name of the setting to reset" });

        return resetCommand;
    }

    #endregion

}