// System.CommandLine command definitions for the GistGet CLI.

using System.CommandLine;
using Spectre.Console;

namespace GistGet.Presentation;

/// <summary>
/// Builds the root command and subcommands for the CLI.
/// </summary>
public class CommandBuilder(IGistGetService gistGetService, IAnsiConsole console)
{
    /// <summary>
    /// Builds the root command tree.
    /// </summary>
    public RootCommand Build()
    {
        var rootCommand = new RootCommand("GistGet - Windows Package Manager Cloud Sync Tool");

        rootCommand.Add(BuildSyncCommand());
        rootCommand.Add(BuildExportCommand());
        rootCommand.Add(BuildImportCommand());
        rootCommand.Add(BuildAuthCommand());

        rootCommand.Add(BuildInstallCommand());
        rootCommand.Add(BuildUninstallCommand());
        rootCommand.Add(BuildUpgradeCommand());
        rootCommand.Add(BuildPinCommand());

        foreach (var cmd in BuildWingetPassthroughCommands())
        {
            rootCommand.Add(cmd);
        }

        return rootCommand;
    }

    private Command BuildSyncCommand()
    {
        var command = new Command("sync", "Synchronizes packages with Gist");
        var urlOption = new Option<string?>("--url", "URL to sync from");
        var fileOption = new Option<string?>("--file", "Local YAML file path to sync from");
        fileOption.AddAlias("-f");
        command.Add(urlOption);
        command.Add(fileOption);

        command.SetHandler(async (url, filePath) =>
        {
            var result = await gistGetService.SyncAsync(url, filePath);
            if (result.Installed.Count > 0)
            {
                console.MarkupLine($"[green]Installed {result.Installed.Count} package(s):[/]");
                foreach (var pkg in result.Installed)
                {
                    console.MarkupLine($"  - {pkg.Id}");
                }
            }

            if (result.Uninstalled.Count > 0)
            {
                console.MarkupLine($"[yellow]Uninstalled {result.Uninstalled.Count} package(s):[/]");
                foreach (var pkg in result.Uninstalled)
                {
                    console.MarkupLine($"  - {pkg.Id}");
                }
            }

            if (result.PinUpdated.Count > 0)
            {
                console.MarkupLine($"[blue]Updated pin for {result.PinUpdated.Count} package(s):[/]");
                foreach (var pkg in result.PinUpdated)
                {
                    console.MarkupLine($"  - {pkg.Id}: {pkg.Pin}");
                }
            }

            if (result.PinRemoved.Count > 0)
            {
                console.MarkupLine($"[blue]Removed pin for {result.PinRemoved.Count} package(s):[/]");
                foreach (var pkg in result.PinRemoved)
                {
                    console.MarkupLine($"  - {pkg.Id}");
                }
            }

            if (result.Failed.Count > 0)
            {
                console.MarkupLine($"[red]Failed {result.Failed.Count} package(s):[/]");
                foreach (var pkg in result.Failed)
                {
                    console.MarkupLine($"  - {pkg.Id}");
                }
            }

            if (result.Errors.Count > 0)
            {
                console.MarkupLine("[red]Errors:[/]");
                foreach (var error in result.Errors)
                {
                    console.MarkupLine($"  - {error}");
                }
            }

            if (result.Success &&
                result.Installed.Count == 0 &&
                result.Uninstalled.Count == 0 &&
                result.PinUpdated.Count == 0 &&
                result.PinRemoved.Count == 0)
            {
                console.MarkupLine("[green]Already in sync. No changes needed.[/]");
            }
            else if (result.Success)
            {
                console.MarkupLine("[green]Sync completed successfully.[/]");
            }
        }, urlOption, fileOption);

        return command;
    }


    private Command BuildExportCommand()
    {
        var command = new Command("export", "Exports installed packages to Gist");
        var outputOption = new Option<string?>("--output", "File to write the result to");
        outputOption.AddAlias("-o");
        command.Add(outputOption);

        command.SetHandler(async output =>
        {
            await gistGetService.ExportAsync(output);
        }, outputOption);

        return command;
    }

    private Command BuildImportCommand()
    {
        var command = new Command("import", "Imports packages from a file");
        var fileArgument = new Argument<string>("file") { Description = "Path to the YAML file to import" };
        command.Add(fileArgument);

        command.SetHandler(async file =>
        {
            await gistGetService.ImportAsync(file);
        }, fileArgument);

        return command;
    }

    private Command BuildAuthCommand()
    {
        var command = new Command("auth", "Manage GitHub authentication");

        var login = new Command("login", "Authenticate with GitHub");
        login.SetHandler(async () => await gistGetService.AuthLoginAsync());
        command.Add(login);

        var logout = new Command("logout", "Log out from GitHub");
        logout.SetHandler(gistGetService.AuthLogout);
        command.Add(logout);

        var status = new Command("status", "Shows current authentication status");
        status.SetHandler(gistGetService.AuthStatusAsync);
        command.Add(status);

        return command;
    }

    private Command BuildInstallCommand()
    {
        var command = new Command("install", "Installs the given package and saves to Gist");
        var idOption = new Option<string>("--id", "Filter results by id") { IsRequired = true };

        var versionOption = new Option<string>("--version", "Use the specified version");
        var scopeOption = new Option<string>("--scope", "Select install scope (user or machine)");
        var archOption = new Option<string>("--architecture", "Select the architecture to install");
        var locationOption = new Option<string>("--location", "Location to install to");
        var interactiveOption = new Option<bool>("--interactive", "Request interactive installation");
        var silentOption = new Option<bool>("--silent", "Request silent installation");
        var logOption = new Option<string>("--log", "Log location");
        var overrideOption = new Option<string>("--override", "Override arguments to be passed on to the installer");
        var forceOption = new Option<bool>("--force", "Override the installer hash check");
        var skipDependenciesOption = new Option<bool>("--skip-dependencies", "Skips processing package dependencies");
        var headerOption = new Option<string>("--header", "Optional Windows-Package-Manager REST source HTTP header");
        var installerTypeOption = new Option<string>("--installer-type", "Select the installer type");
        var customOption = new Option<string>("--custom", "Arguments to be passed on to the installer in addition to the defaults");
        var localeOption = new Option<string>("--locale", "Locale to use (BCP47 format)");
        var acceptPackageAgreementsOption = new Option<bool>("--accept-package-agreements", "Accept all license agreements required for the package");
        var acceptSourceAgreementsOption = new Option<bool>("--accept-source-agreements", "Accept all source agreements required for the source");
        var ignoreSecurityHashOption = new Option<bool>("--ignore-security-hash", "Ignore the installer hash check failure");

        command.Add(idOption);
        command.Add(versionOption);
        command.Add(scopeOption);
        command.Add(archOption);
        command.Add(locationOption);
        command.Add(interactiveOption);
        command.Add(silentOption);
        command.Add(logOption);
        command.Add(overrideOption);
        command.Add(forceOption);
        command.Add(skipDependenciesOption);
        command.Add(headerOption);
        command.Add(installerTypeOption);
        command.Add(customOption);
        command.Add(localeOption);
        command.Add(acceptPackageAgreementsOption);
        command.Add(acceptSourceAgreementsOption);
        command.Add(ignoreSecurityHashOption);

        command.SetHandler(async context =>
        {
            var parseResult = context.ParseResult;
            var id = parseResult.GetValueForOption(idOption) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                console.MarkupLine("[red]Package ID is required.[/]");
                return;
            }

            var options = new InstallOptions
            {
                Id = id,
                Version = parseResult.GetValueForOption(versionOption),
                Scope = parseResult.GetValueForOption(scopeOption),
                Architecture = parseResult.GetValueForOption(archOption),
                Location = parseResult.GetValueForOption(locationOption),
                Interactive = parseResult.GetValueForOption(interactiveOption),
                Silent = parseResult.GetValueForOption(silentOption),
                Log = parseResult.GetValueForOption(logOption),
                Override = parseResult.GetValueForOption(overrideOption),
                Force = parseResult.GetValueForOption(forceOption),
                SkipDependencies = parseResult.GetValueForOption(skipDependenciesOption),
                Header = parseResult.GetValueForOption(headerOption),
                InstallerType = parseResult.GetValueForOption(installerTypeOption),
                Custom = parseResult.GetValueForOption(customOption),
                Locale = parseResult.GetValueForOption(localeOption),
                AcceptPackageAgreements = parseResult.GetValueForOption(acceptPackageAgreementsOption),
                AcceptSourceAgreements = parseResult.GetValueForOption(acceptSourceAgreementsOption),
                AllowHashMismatch = parseResult.GetValueForOption(ignoreSecurityHashOption)
            };

            context.ExitCode = await gistGetService.InstallAndSaveAsync(options);
        });

        return command;
    }


    private Command BuildUninstallCommand()
    {
        var command = new Command("uninstall", "Uninstalls the given package and updates Gist");
        var idOption = new Option<string>("--id", "Filter results by id") { IsRequired = true };
        var scopeOption = new Option<string>("--scope", "Select install scope (user or machine)");
        var interactiveOption = new Option<bool>("--interactive", "Request interactive uninstall");
        var silentOption = new Option<bool>("--silent", "Request silent uninstall");
        var forceOption = new Option<bool>("--force", "Direct run the command and continue with non security related issues");

        command.Add(idOption);
        command.Add(scopeOption);
        command.Add(interactiveOption);
        command.Add(silentOption);
        command.Add(forceOption);

        command.SetHandler(async context =>
        {
            var parseResult = context.ParseResult;
            var id = parseResult.GetValueForOption(idOption) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                console.MarkupLine("[red]Package ID is required.[/]");
                return;
            }

            var options = new UninstallOptions
            {
                Id = id,
                Scope = parseResult.GetValueForOption(scopeOption),
                Interactive = parseResult.GetValueForOption(interactiveOption),
                Silent = parseResult.GetValueForOption(silentOption),
                Force = parseResult.GetValueForOption(forceOption)
            };

            context.ExitCode = await gistGetService.UninstallAndSaveAsync(options);
        });

        return command;
    }

    private Command BuildUpgradeCommand()
    {
        var command = new Command("upgrade", "Upgrades the given package and saves to Gist");
        var idArgument = new Argument<string?>("package", "Package to upgrade") { Arity = ArgumentArity.ZeroOrOne };
        var idOption = new Option<string>("--id", "Filter results by id");
        var versionOption = new Option<string>("--version", "Use the specified version");
        var scopeOption = new Option<string>("--scope", "Select install scope (user or machine)");
        var archOption = new Option<string>("--architecture", "Select the architecture to install");
        var locationOption = new Option<string>("--location", "Location to install to");
        var interactiveOption = new Option<bool>("--interactive", "Request interactive upgrade");
        var silentOption = new Option<bool>("--silent", "Request silent upgrade");
        var logOption = new Option<string>("--log", "Log location");
        var overrideOption = new Option<string>("--override", "Override arguments to be passed on to the installer");
        var forceOption = new Option<bool>("--force", "Override the installer hash check");
        var skipDependenciesOption = new Option<bool>("--skip-dependencies", "Skips processing package dependencies");
        var headerOption = new Option<string>("--header", "Optional Windows-Package-Manager REST source HTTP header");
        var installerTypeOption = new Option<string>("--installer-type", "Select the installer type");
        var customOption = new Option<string>("--custom", "Arguments to be passed on to the installer in addition to the defaults");
        var localeOption = new Option<string>("--locale", "Locale to use (BCP47 format)");
        var acceptPackageAgreementsOption = new Option<bool>("--accept-package-agreements", "Accept all license agreements required for the package");
        var acceptSourceAgreementsOption = new Option<bool>("--accept-source-agreements", "Accept all source agreements required for the source");
        var ignoreSecurityHashOption = new Option<bool>("--ignore-security-hash", "Ignore the installer hash check failure");

        command.Add(idArgument);
        command.Add(idOption);
        command.Add(versionOption);
        command.Add(scopeOption);
        command.Add(archOption);
        command.Add(locationOption);
        command.Add(interactiveOption);
        command.Add(silentOption);
        command.Add(logOption);
        command.Add(overrideOption);
        command.Add(forceOption);
        command.Add(skipDependenciesOption);
        command.Add(headerOption);
        command.Add(installerTypeOption);
        command.Add(customOption);
        command.Add(localeOption);
        command.Add(acceptPackageAgreementsOption);
        command.Add(acceptSourceAgreementsOption);
        command.Add(ignoreSecurityHashOption);
        command.TreatUnmatchedTokensAsErrors = false;

        command.SetHandler(async context =>
        {
            var parseResult = context.ParseResult;
            var id = parseResult.GetValueForOption(idOption) ?? parseResult.GetValueForArgument(idArgument);
            if (!string.IsNullOrWhiteSpace(id))
            {
                var options = new UpgradeOptions
                {
                    Id = id,
                    Version = parseResult.GetValueForOption(versionOption),
                    Scope = parseResult.GetValueForOption(scopeOption),
                    Architecture = parseResult.GetValueForOption(archOption),
                    Location = parseResult.GetValueForOption(locationOption),
                    Interactive = parseResult.GetValueForOption(interactiveOption),
                    Silent = parseResult.GetValueForOption(silentOption),
                    Log = parseResult.GetValueForOption(logOption),
                    Override = parseResult.GetValueForOption(overrideOption),
                    Force = parseResult.GetValueForOption(forceOption),
                    SkipDependencies = parseResult.GetValueForOption(skipDependenciesOption),
                    Header = parseResult.GetValueForOption(headerOption),
                    InstallerType = parseResult.GetValueForOption(installerTypeOption),
                    Custom = parseResult.GetValueForOption(customOption),
                    Locale = parseResult.GetValueForOption(localeOption),
                    AcceptPackageAgreements = parseResult.GetValueForOption(acceptPackageAgreementsOption),
                    AcceptSourceAgreements = parseResult.GetValueForOption(acceptSourceAgreementsOption),
                    AllowHashMismatch = parseResult.GetValueForOption(ignoreSecurityHashOption)
                };

                context.ExitCode = await gistGetService.UpgradeAndSaveAsync(options);
            }
            else
            {
                var argsToPass = parseResult.UnmatchedTokens.ToArray();
                context.ExitCode = await gistGetService.RunPassthroughAsync("upgrade", argsToPass);
            }
        });

        return command;
    }


    private Command BuildPinCommand()
    {
        var command = new Command("pin", "Manage package pins");

        var add = new Command("add", "Adds a package pin and saves to Gist");
        var addId = new Argument<string>("package", "Package to pin");
        var addVersion = new Option<string>("--version", "The version to pin") { IsRequired = true };
        var addBlocking = new Option<bool>("--blocking", "Block the given version from being upgraded");
        var addGating = new Option<bool>("--gating", "The given version is the maximum allowed version");
        var addForce = new Option<bool>("--force", "Force running the command even if there is an existing pin");
        add.Add(addId);
        add.Add(addVersion);
        add.Add(addBlocking);
        add.Add(addGating);
        add.Add(addForce);
        add.SetHandler(async context =>
        {
            var parseResult = context.ParseResult;
            var id = parseResult.GetValueForArgument(addId);
            var version = parseResult.GetValueForOption(addVersion) ?? string.Empty;
            var blocking = parseResult.GetValueForOption(addBlocking);
            var gating = parseResult.GetValueForOption(addGating);
            var force = parseResult.GetValueForOption(addForce);

            string? pinType = null;
            if (blocking) pinType = "blocking";
            else if (gating) pinType = "gating";

            await gistGetService.PinAddAndSaveAsync(id, version, pinType, force);
        });
        command.Add(add);

        var remove = new Command("remove", "Removes a package pin and updates Gist");
        var removeId = new Argument<string>("package", "Package to unpin");
        remove.Add(removeId);
        remove.SetHandler(async id =>
        {
            await gistGetService.PinRemoveAndSaveAsync(id);
        }, removeId);
        command.Add(remove);

        var list = new Command("list", "List current pins [Passthrough]");
        var listArgs = new Argument<string[]>("args") { Arity = ArgumentArity.ZeroOrMore };
        list.Add(listArgs);
        list.SetHandler(async args =>
        {
            var allArgs = new List<string> { "list" };
            allArgs.AddRange(args);
            await gistGetService.RunPassthroughAsync("pin", allArgs.ToArray());
        }, listArgs);
        command.Add(list);

        var reset = new Command("reset", "Resets pins [Passthrough]");
        var resetArgs = new Argument<string[]>("args") { Arity = ArgumentArity.ZeroOrMore };
        reset.Add(resetArgs);
        reset.SetHandler(async args =>
        {
            var allArgs = new List<string> { "reset" };
            allArgs.AddRange(args);
            await gistGetService.RunPassthroughAsync("pin", allArgs.ToArray());
        }, resetArgs);
        command.Add(reset);

        return command;
    }

    private Command[] BuildWingetPassthroughCommands()
    {
        var commands = new List<Command>();
        var wingetCommands = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["list"] = "Displays installed packages [Passthrough]",
            ["search"] = "Finds and shows basic information of packages [Passthrough]",
            ["show"] = "Shows information about a package [Passthrough]",
            ["source"] = "Manage sources of packages [Passthrough]",
            ["settings"] = "Open settings or set administrator settings [Passthrough]",
            ["features"] = "Shows the status of experimental features [Passthrough]",
            ["hash"] = "Helper to hash installer files [Passthrough]",
            ["validate"] = "Validates a manifest file [Passthrough]",
            ["configure"] = "Configures the system into a desired state [Passthrough]",
            ["download"] = "Downloads the installer from a given package [Passthrough]",
            ["repair"] = "Repairs the selected package [Passthrough]",
            ["dscv3"] = "DSC v3 command based resource [Passthrough]",
            ["mcp"] = "Model Context Protocol server [Passthrough]"
        };

        foreach (var (cmd, description) in wingetCommands)
        {
            var command = new Command(cmd, description);
            var argsArgument = new Argument<string[]>("arguments") { Arity = ArgumentArity.ZeroOrMore };
            command.Add(argsArgument);

            command.SetHandler(async arguments =>
            {
                await gistGetService.RunPassthroughAsync(cmd, arguments);
            }, argsArgument);

            commands.Add(command);
        }

        return commands.ToArray();
    }
}
