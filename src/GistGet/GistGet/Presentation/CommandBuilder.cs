using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace GistGet.Presentation;

public class CommandBuilder(IGistGetService gistGetService)
{
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
        var command = new Command("sync", "Synchronize packages with Gist");
        var urlOption = new Option<string?>("--url", "Gist URL to sync from (optional)");
        command.Add(urlOption);

        command.SetHandler(async url =>
        {
            var result = await gistGetService.SyncAsync(url);

            // 結果を表示
            if (result.Installed.Count > 0)
            {
                AnsiConsole.MarkupLine($"[green]Installed {result.Installed.Count} package(s):[/]");
                foreach (var pkg in result.Installed)
                {
                    AnsiConsole.MarkupLine($"  - {pkg.Id}");
                }
            }

            if (result.Uninstalled.Count > 0)
            {
                AnsiConsole.MarkupLine($"[yellow]Uninstalled {result.Uninstalled.Count} package(s):[/]");
                foreach (var pkg in result.Uninstalled)
                {
                    AnsiConsole.MarkupLine($"  - {pkg.Id}");
                }
            }

            if (result.PinUpdated.Count > 0)
            {
                AnsiConsole.MarkupLine($"[blue]Updated pin for {result.PinUpdated.Count} package(s):[/]");
                foreach (var pkg in result.PinUpdated)
                {
                    AnsiConsole.MarkupLine($"  - {pkg.Id}: {pkg.Pin}");
                }
            }

            if (result.PinRemoved.Count > 0)
            {
                AnsiConsole.MarkupLine($"[blue]Removed pin for {result.PinRemoved.Count} package(s):[/]");
                foreach (var pkg in result.PinRemoved)
                {
                    AnsiConsole.MarkupLine($"  - {pkg.Id}");
                }
            }

            if (result.Failed.Count > 0)
            {
                AnsiConsole.MarkupLine($"[red]Failed {result.Failed.Count} package(s):[/]");
                foreach (var pkg in result.Failed)
                {
                    AnsiConsole.MarkupLine($"  - {pkg.Id}");
                }
            }

            if (result.Errors.Count > 0)
            {
                AnsiConsole.MarkupLine("[red]Errors:[/]");
                foreach (var error in result.Errors)
                {
                    AnsiConsole.MarkupLine($"  - {error}");
                }
            }

            if (result.Success && 
                result.Installed.Count == 0 && 
                result.Uninstalled.Count == 0 && 
                result.PinUpdated.Count == 0 &&
                result.PinRemoved.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]Already in sync. No changes needed.[/]");
            }
            else if (result.Success)
            {
                AnsiConsole.MarkupLine("[green]Sync completed successfully.[/]");
            }
        }, urlOption);

        return command;
    }


    private Command BuildExportCommand()
    {
        var command = new Command("export", "Export current package state to YAML");
        var outputOption = new Option<string>("--output", "Output file path");
        command.Add(outputOption);

        return command;
    }

    private Command BuildImportCommand()
    {
        var command = new Command("import", "Import YAML file to Gist");
        var fileArgument = new Argument<string>("file") { Description = "YAML file to import" };
        command.Add(fileArgument);

        return command;
    }

    private Command BuildAuthCommand()
    {
        var command = new Command("auth", "Manage authentication");

        var login = new Command("login", "Login to GitHub");
        login.SetHandler(async () => await gistGetService.AuthLoginAsync());
        command.Add(login);

        var logout = new Command("logout", "Logout from GitHub");
        logout.SetHandler(gistGetService.AuthLogout);
        command.Add(logout);

        var status = new Command("status", "Check authentication status");
        status.SetHandler(gistGetService.AuthStatusAsync);
        command.Add(status);

        return command;
    }

    private Command BuildInstallCommand()
    {
        var command = new Command("install", "Install a package and save to Gist");
        var idOption = new Option<string>("--id", "Package ID (winget compatible)") { IsRequired = true };

        var versionOption = new Option<string>("--version", "Package version");
        var scopeOption = new Option<string>("--scope", "Install scope (user|machine)");
        var archOption = new Option<string>("--architecture", "Architecture (x86|x64|arm|arm64)");
        var locationOption = new Option<string>("--location", "Install location");
        var interactiveOption = new Option<bool>("--interactive", "Request interactive installation");
        var silentOption = new Option<bool>("--silent", "Request silent installation");
        var logOption = new Option<string>("--log", "Log file path");
        var overrideOption = new Option<string>("--override", "Override arguments");
        var forceOption = new Option<bool>("--force", "Force command execution");
        var skipDependenciesOption = new Option<bool>("--skip-dependencies", "Skip dependencies");
        var headerOption = new Option<string>("--header", "Custom HTTP header");
        var installerTypeOption = new Option<string>("--installer-type", "Installer type");
        var customOption = new Option<string>("--custom", "Custom arguments");
        var localeOption = new Option<string>("--locale", "Locale (BCP47 format)");
        var acceptPackageAgreementsOption = new Option<bool>("--accept-package-agreements", "Accept package license agreements");
        var acceptSourceAgreementsOption = new Option<bool>("--accept-source-agreements", "Accept source license agreements");
        var ignoreSecurityHashOption = new Option<bool>("--ignore-security-hash", "Ignore security hash mismatch");

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
                AnsiConsole.MarkupLine("[red]Package ID is required.[/]");
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

            await gistGetService.InstallAndSaveAsync(options);
        });

        return command;
    }


    private Command BuildUninstallCommand()
    {
        var command = new Command("uninstall", "Uninstall a package and update Gist");
        var idOption = new Option<string>("--id", "Package ID") { IsRequired = true };
        var scopeOption = new Option<string>("--scope", "Uninstall scope (user|machine)");
        var interactiveOption = new Option<bool>("--interactive", "Request interactive uninstall");
        var silentOption = new Option<bool>("--silent", "Request silent uninstall");
        var forceOption = new Option<bool>("--force", "Force command execution");
        
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
                AnsiConsole.MarkupLine("[red]Package ID is required.[/]");
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

            await gistGetService.UninstallAndSaveAsync(options);
        });

        return command;
    }

    private Command BuildUpgradeCommand()
    {
        var command = new Command("upgrade", "Upgrade a package and save to Gist");
        var idArgument = new Argument<string?>("package", "Package ID") { Arity = ArgumentArity.ZeroOrOne };
        var idOption = new Option<string>("--id", "Package ID (winget compatible)");
        var versionOption = new Option<string>("--version", "Package version");
        var scopeOption = new Option<string>("--scope", "Upgrade scope (user|machine)");
        var archOption = new Option<string>("--architecture", "Architecture (x86|x64|arm|arm64)");
        var locationOption = new Option<string>("--location", "Install location");
        var interactiveOption = new Option<bool>("--interactive", "Request interactive upgrade");
        var silentOption = new Option<bool>("--silent", "Request silent upgrade");
        var logOption = new Option<string>("--log", "Log file path");
        var overrideOption = new Option<string>("--override", "Override arguments");
        var forceOption = new Option<bool>("--force", "Force command execution");
        var skipDependenciesOption = new Option<bool>("--skip-dependencies", "Skip dependencies");
        var installerTypeOption = new Option<string>("--installer-type", "Installer type");
        var customOption = new Option<string>("--custom", "Custom arguments");
        var localeOption = new Option<string>("--locale", "Locale (BCP47 format)");
        var acceptPackageAgreementsOption = new Option<bool>("--accept-package-agreements", "Accept package license agreements");
        var acceptSourceAgreementsOption = new Option<bool>("--accept-source-agreements", "Accept source license agreements");
        var ignoreSecurityHashOption = new Option<bool>("--ignore-security-hash", "Ignore security hash mismatch");

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
        command.Add(installerTypeOption);
        command.Add(customOption);
        command.Add(localeOption);
        command.Add(acceptPackageAgreementsOption);
        command.Add(acceptSourceAgreementsOption);
        command.Add(ignoreSecurityHashOption);

        // Allow unmatched tokens to be collected for passthrough
        command.TreatUnmatchedTokensAsErrors = false;

        command.SetHandler(async context =>
        {
            var parseResult = context.ParseResult;
            var id = parseResult.GetValueForOption(idOption) ?? parseResult.GetValueForArgument(idArgument);

            // If ID is specified, perform managed upgrade
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
                    InstallerType = parseResult.GetValueForOption(installerTypeOption),
                    Custom = parseResult.GetValueForOption(customOption),
                    Locale = parseResult.GetValueForOption(localeOption),
                    AcceptPackageAgreements = parseResult.GetValueForOption(acceptPackageAgreementsOption),
                    AcceptSourceAgreements = parseResult.GetValueForOption(acceptSourceAgreementsOption),
                    AllowHashMismatch = parseResult.GetValueForOption(ignoreSecurityHashOption)
                };

                await gistGetService.UpgradeAndSaveAsync(options);
            }
            else
            {
                // If ID is missing, pass through everything to winget
                // Reconstruct arguments from tokens
                var tokens = parseResult.Tokens.Select(t => t.Value).ToList();
                var argsToPass = new List<string>();
                bool foundUpgrade = false;

                foreach (var token in tokens)
                {
                    if (!foundUpgrade && token.Equals("upgrade", StringComparison.OrdinalIgnoreCase))
                    {
                        foundUpgrade = true;
                        continue;
                    }
                    
                    // If we haven't found "upgrade" yet, it might be the root command or something else we want to skip.
                    // But if we are in this handler, "upgrade" must be present.
                    // Once we found "upgrade", everything else is an argument.
                    if (foundUpgrade)
                    {
                        argsToPass.Add(token);
                    }
                }

                // Fallback: if "upgrade" wasn't found in tokens (unlikely), pass all tokens? 
                // Or maybe tokens didn't include "upgrade" if it was invoked via alias? 
                // For now assuming "upgrade" is present.
                if (!foundUpgrade && tokens.Count > 0)
                {
                     // If tokens are just arguments (e.g. implied command?), pass them all.
                     argsToPass.AddRange(tokens);
                }

                await gistGetService.RunPassthroughAsync("upgrade", argsToPass.ToArray());
            }
        });

        return command;
    }


    private Command BuildPinCommand()
    {
        var command = new Command("pin", "Manage package pins");

        var add = new Command("add", "Pin a package version");
        var addId = new Argument<string>("package", "Package ID");
        var addVersion = new Option<string>("--version", "Version to pin") { IsRequired = true };
        var addBlocking = new Option<bool>("--blocking", "Use blocking pin type");
        var addGating = new Option<bool>("--gating", "Use gating pin type");
        var addForce = new Option<bool>("--force", "Force overwrite existing pin");
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

        var remove = new Command("remove", "Remove a package pin");
        var removeId = new Argument<string>("package", "Package ID");
        remove.Add(removeId);
        remove.SetHandler(async id =>
        {
            await gistGetService.PinRemoveAndSaveAsync(id);
        }, removeId);
        command.Add(remove);

        var list = new Command("list", "List pinned packages");
        var listArgs = new Argument<string[]>("args") { Arity = ArgumentArity.ZeroOrMore };
        list.Add(listArgs);
        list.SetHandler(async args =>
        {
            var allArgs = new List<string> { "list" };
            allArgs.AddRange(args);
            await gistGetService.RunPassthroughAsync("pin", allArgs.ToArray());
        }, listArgs);
        command.Add(list);

        var reset = new Command("reset", "Reset all pins");
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
        var wingetCommands = new[] {
            "list", "search", "show", "source", "settings", "features",
            "hash", "validate", "configure", "download", "repair"
        };

        foreach (var cmd in wingetCommands)
        {
            var command = new Command(cmd, $"Pass through to winget {cmd}");
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
