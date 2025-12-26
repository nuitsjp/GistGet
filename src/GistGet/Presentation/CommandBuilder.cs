// System.CommandLine command definitions for the GistGet CLI.

using System.CommandLine;
using System.Reflection;
using GistGet.Resources;
using Spectre.Console;

namespace GistGet.Presentation;

/// <summary>
/// Builds the root command and subcommands for the CLI.
/// </summary>
public class CommandBuilder(IGistGetService gistGetService, IAnsiConsole console)
{
    static CommandBuilder()
    {
        CommandLineLocalizationResources.EnsureRegistered();
    }

    /// <summary>
    /// Builds the root command tree.
    /// </summary>
    public RootCommand Build()
    {
        var rootCommand = new RootCommand(Messages.RootCommandDescription)
        {
            BuildSyncCommand(),
            BuildInitCommand(),
            BuildAuthCommand(),
            BuildInstallCommand(),
            BuildUninstallCommand(),
            BuildUpgradeCommand(),
            BuildPinCommand()
        };

        foreach (var cmd in BuildWingetPassthroughCommands())
        {
            rootCommand.Add(cmd);
        }

        return rootCommand;
    }

    private Command BuildSyncCommand()
    {
        var command = new Command("sync", Messages.SyncCommandDescription);
        var urlOption = new Option<string?>("--url", Messages.SyncUrlOptionDescription);
        var fileOption = new Option<string?>("--file", Messages.SyncFileOptionDescription);
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
                    console.MarkupLine($"  - {EscapeMarkup(pkg.Id)}");
                }
            }

            if (result.Uninstalled.Count > 0)
            {
                console.MarkupLine($"[yellow]Uninstalled {result.Uninstalled.Count} package(s):[/]");
                foreach (var pkg in result.Uninstalled)
                {
                    console.MarkupLine($"  - {EscapeMarkup(pkg.Id)}");
                }
            }

            if (result.PinUpdated.Count > 0)
            {
                console.MarkupLine($"[blue]Updated pin for {result.PinUpdated.Count} package(s):[/]");
                foreach (var pkg in result.PinUpdated)
                {
                    console.MarkupLine($"  - {EscapeMarkup(pkg.Id)}: {EscapeMarkup(pkg.Pin)}");
                }
            }

            if (result.PinRemoved.Count > 0)
            {
                console.MarkupLine($"[blue]Removed pin for {result.PinRemoved.Count} package(s):[/]");
                foreach (var pkg in result.PinRemoved)
                {
                    console.MarkupLine($"  - {EscapeMarkup(pkg.Id)}");
                }
            }

            if (result.Failed.Count > 0)
            {
                console.MarkupLine($"[red]Failed {result.Failed.Count} package(s):[/]");
                foreach (var (pkg, exitCode) in result.Failed)
                {
                    console.MarkupLine($"  - {EscapeMarkup(pkg.ToDisplayString())}: exit code {exitCode}");
                }
            }

            if (result is { Success: true, Installed.Count: 0, Uninstalled.Count: 0, PinUpdated.Count: 0, PinRemoved.Count: 0 })
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

    private static string EscapeMarkup(string? value)
    {
        return Markup.Escape(value ?? string.Empty);
    }

    private Command BuildInitCommand()
    {
        var command = new Command("init", Messages.InitCommandDescription);
        command.SetHandler(async () =>
        {
            await gistGetService.InitAsync();
        });
        return command;
    }

    private Command BuildAuthCommand()
    {
        var command = new Command("auth", Messages.AuthCommandDescription);

        var login = new Command("login", Messages.AuthLoginDescription);
        login.SetHandler(async () => await gistGetService.AuthLoginAsync());
        command.Add(login);

        var logout = new Command("logout", Messages.AuthLogoutDescription);
        logout.SetHandler(gistGetService.AuthLogout);
        command.Add(logout);

        var status = new Command("status", Messages.AuthStatusDescription);
        status.SetHandler(gistGetService.AuthStatusAsync);
        command.Add(status);

        return command;
    }

    private Command BuildInstallCommand()
    {
        var command = new Command("install", Messages.InstallCommandDescription);
        var idOption = new Option<string>("--id", Messages.OptionDescriptionFilterById) { IsRequired = true };

        var versionOption = new Option<string>("--version", Messages.OptionDescriptionVersion);
        var scopeOption = new Option<string>("--scope", Messages.OptionDescriptionScope);
        var archOption = new Option<string>("--architecture", Messages.OptionDescriptionArchitecture);
        var locationOption = new Option<string>("--location", Messages.OptionDescriptionLocation);
        var interactiveOption = new Option<bool>("--interactive", Messages.OptionDescriptionInteractiveInstall);
        var silentOption = new Option<bool>("--silent", Messages.OptionDescriptionSilentInstall);
        var logOption = new Option<string>("--log", Messages.OptionDescriptionLogLocation);
        var overrideOption = new Option<string>("--override", Messages.OptionDescriptionOverrideArguments);
        var forceOption = new Option<bool>("--force", Messages.OptionDescriptionForceOverrideHash);
        var skipDependenciesOption = new Option<bool>("--skip-dependencies", Messages.OptionDescriptionSkipDependencies);
        var headerOption = new Option<string>("--header", Messages.OptionDescriptionHeader);
        var installerTypeOption = new Option<string>("--installer-type", Messages.OptionDescriptionInstallerType);
        var customOption = new Option<string>("--custom", Messages.OptionDescriptionCustomArguments);
        var localeOption = new Option<string>("--locale", Messages.OptionDescriptionLocale);
        var acceptPackageAgreementsOption = new Option<bool>("--accept-package-agreements", Messages.OptionDescriptionAcceptPackageAgreements);
        var acceptSourceAgreementsOption = new Option<bool>("--accept-source-agreements", Messages.OptionDescriptionAcceptSourceAgreements);
        var ignoreSecurityHashOption = new Option<bool>("--ignore-security-hash", Messages.OptionDescriptionIgnoreSecurityHash);

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
            var id = parseResult.GetValueForOption(idOption)!;

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
        var command = new Command("uninstall", Messages.UninstallCommandDescription);
        var idOption = new Option<string>("--id", Messages.OptionDescriptionFilterById) { IsRequired = true };
        var scopeOption = new Option<string>("--scope", Messages.OptionDescriptionScope);
        var interactiveOption = new Option<bool>("--interactive", Messages.OptionDescriptionInteractiveUninstall);
        var silentOption = new Option<bool>("--silent", Messages.OptionDescriptionSilentUninstall);
        var forceOption = new Option<bool>("--force", Messages.UninstallForceOptionDescription);

        command.Add(idOption);
        command.Add(scopeOption);
        command.Add(interactiveOption);
        command.Add(silentOption);
        command.Add(forceOption);

        command.SetHandler(async context =>
        {
            var parseResult = context.ParseResult;
            var id = parseResult.GetValueForOption(idOption)!;

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
        var command = new Command("upgrade", Messages.UpgradeCommandDescription);
        var idArgument = new Argument<string?>("package", Messages.UpgradePackageArgumentDescription) { Arity = ArgumentArity.ZeroOrOne };
        var idOption = new Option<string>("--id", Messages.OptionDescriptionFilterById);
        var versionOption = new Option<string>("--version", Messages.OptionDescriptionVersion);
        var scopeOption = new Option<string>("--scope", Messages.OptionDescriptionScope);
        var archOption = new Option<string>("--architecture", Messages.OptionDescriptionArchitecture);
        var locationOption = new Option<string>("--location", Messages.OptionDescriptionLocation);
        var interactiveOption = new Option<bool>("--interactive", Messages.OptionDescriptionInteractiveUpgrade);
        var silentOption = new Option<bool>("--silent", Messages.OptionDescriptionSilentUpgrade);
        var logOption = new Option<string>("--log", Messages.OptionDescriptionLogLocation);
        var overrideOption = new Option<string>("--override", Messages.OptionDescriptionOverrideArguments);
        var forceOption = new Option<bool>("--force", Messages.OptionDescriptionForceOverrideHash);
        var skipDependenciesOption = new Option<bool>("--skip-dependencies", Messages.OptionDescriptionSkipDependencies);
        var headerOption = new Option<string>("--header", Messages.OptionDescriptionHeader);
        var installerTypeOption = new Option<string>("--installer-type", Messages.OptionDescriptionInstallerType);
        var customOption = new Option<string>("--custom", Messages.OptionDescriptionCustomArguments);
        var localeOption = new Option<string>("--locale", Messages.OptionDescriptionLocale);
        var acceptPackageAgreementsOption = new Option<bool>("--accept-package-agreements", Messages.OptionDescriptionAcceptPackageAgreements);
        var acceptSourceAgreementsOption = new Option<bool>("--accept-source-agreements", Messages.OptionDescriptionAcceptSourceAgreements);
        var ignoreSecurityHashOption = new Option<bool>("--ignore-security-hash", Messages.OptionDescriptionIgnoreSecurityHash);
        var allOption = new Option<bool>("--all", Messages.OptionDescriptionUpgradeAll);

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
        command.Add(allOption);
        command.TreatUnmatchedTokensAsErrors = false;

        command.SetHandler(async context =>
        {
            var parseResult = context.ParseResult;
            var id = parseResult.GetValueForOption(idOption) ?? parseResult.GetValueForArgument(idArgument);
            var all = parseResult.GetValueForOption(allOption);

            if (!string.IsNullOrWhiteSpace(id) && !all)
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
                var argsToPass = parseResult.UnmatchedTokens.ToList();
                if (all)
                {
                    argsToPass.Insert(0, "--all");

                    // Add recognized options when --all is specified
                    var version = parseResult.GetValueForOption(versionOption);
                    if (!string.IsNullOrEmpty(version)) { argsToPass.Add("--version"); argsToPass.Add(version); }

                    var scope = parseResult.GetValueForOption(scopeOption);
                    if (!string.IsNullOrEmpty(scope)) { argsToPass.Add("--scope"); argsToPass.Add(scope); }

                    var arch = parseResult.GetValueForOption(archOption);
                    if (!string.IsNullOrEmpty(arch)) { argsToPass.Add("--architecture"); argsToPass.Add(arch); }

                    var location = parseResult.GetValueForOption(locationOption);
                    if (!string.IsNullOrEmpty(location)) { argsToPass.Add("--location"); argsToPass.Add(location); }

                    if (parseResult.GetValueForOption(interactiveOption)) { argsToPass.Add("--interactive"); }
                    if (parseResult.GetValueForOption(silentOption)) { argsToPass.Add("--silent"); }

                    var log = parseResult.GetValueForOption(logOption);
                    if (!string.IsNullOrEmpty(log)) { argsToPass.Add("--log"); argsToPass.Add(log); }

                    var overrideArg = parseResult.GetValueForOption(overrideOption);
                    if (!string.IsNullOrEmpty(overrideArg)) { argsToPass.Add("--override"); argsToPass.Add(overrideArg); }

                    if (parseResult.GetValueForOption(forceOption)) { argsToPass.Add("--force"); }
                    if (parseResult.GetValueForOption(skipDependenciesOption)) { argsToPass.Add("--skip-dependencies"); }

                    var header = parseResult.GetValueForOption(headerOption);
                    if (!string.IsNullOrEmpty(header)) { argsToPass.Add("--header"); argsToPass.Add(header); }

                    var installerType = parseResult.GetValueForOption(installerTypeOption);
                    if (!string.IsNullOrEmpty(installerType)) { argsToPass.Add("--installer-type"); argsToPass.Add(installerType); }

                    var custom = parseResult.GetValueForOption(customOption);
                    if (!string.IsNullOrEmpty(custom)) { argsToPass.Add("--custom"); argsToPass.Add(custom); }

                    var locale = parseResult.GetValueForOption(localeOption);
                    if (!string.IsNullOrEmpty(locale)) { argsToPass.Add("--locale"); argsToPass.Add(locale); }

                    if (parseResult.GetValueForOption(acceptPackageAgreementsOption)) { argsToPass.Add("--accept-package-agreements"); }
                    if (parseResult.GetValueForOption(acceptSourceAgreementsOption)) { argsToPass.Add("--accept-source-agreements"); }
                    if (parseResult.GetValueForOption(ignoreSecurityHashOption)) { argsToPass.Add("--ignore-security-hash"); }
                }
                context.ExitCode = await gistGetService.RunPassthroughAsync("upgrade", argsToPass.ToArray());
            }
        });

        return command;
    }


    private Command BuildPinCommand()
    {
        var command = new Command("pin", Messages.PinCommandDescription);

        var add = new Command("add", Messages.PinAddCommandDescription);
        var addId = new Argument<string>("package", Messages.PinAddPackageArgumentDescription);
        var addVersion = new Option<string>("--version", Messages.PinAddVersionOptionDescription) { IsRequired = true };
        var addBlocking = new Option<bool>("--blocking", Messages.PinAddBlockingOptionDescription);
        var addGating = new Option<bool>("--gating", Messages.PinAddGatingOptionDescription);
        var addForce = new Option<bool>("--force", Messages.PinAddForceOptionDescription);
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

        var remove = new Command("remove", Messages.PinRemoveCommandDescription);
        var removeId = new Argument<string>("package", Messages.PinRemovePackageArgumentDescription);
        remove.Add(removeId);
        remove.SetHandler(async id =>
        {
            await gistGetService.PinRemoveAndSaveAsync(id);
        }, removeId);
        command.Add(remove);

        var list = new Command("list", Messages.PinListCommandDescription);
        var listArgs = new Argument<string[]>("args") { Arity = ArgumentArity.ZeroOrMore };
        list.Add(listArgs);
        list.SetHandler(async args =>
        {
            var allArgs = new List<string> { "list" };
            allArgs.AddRange(args);
            await gistGetService.RunPassthroughAsync("pin", allArgs.ToArray());
        }, listArgs);
        command.Add(list);

        var reset = new Command("reset", Messages.PinResetCommandDescription);
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
            ["list"] = Messages.WingetListCommandDescription,
            ["search"] = Messages.WingetSearchCommandDescription,
            ["show"] = Messages.WingetShowCommandDescription,
            ["source"] = Messages.WingetSourceCommandDescription,
            ["settings"] = Messages.WingetSettingsCommandDescription,
            ["features"] = Messages.WingetFeaturesCommandDescription,
            ["hash"] = Messages.WingetHashCommandDescription,
            ["validate"] = Messages.WingetValidateCommandDescription,
            ["configure"] = Messages.WingetConfigureCommandDescription,
            ["download"] = Messages.WingetDownloadCommandDescription,
            ["repair"] = Messages.WingetRepairCommandDescription,
            ["dscv3"] = Messages.WingetDscv3CommandDescription,
            ["mcp"] = Messages.WingetMcpCommandDescription
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

    private static class CommandLineLocalizationResources
    {
        private static bool s_initialized;

        public static void EnsureRegistered()
        {
            if (s_initialized)
            {
                return;
            }

            var instanceField = typeof(LocalizationResources).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            instanceField?.SetValue(null, new ResourceLocalizationResources());
            s_initialized = true;
        }

        private sealed class ResourceLocalizationResources : LocalizationResources
        {
            public override string HelpDescriptionTitle() => Messages.HelpDescriptionTitle;
            public override string HelpUsageTitle() => Messages.HelpUsageTitle;
            public override string HelpOptionsTitle() => Messages.HelpOptionsTitle;
            public override string HelpCommandsTitle() => Messages.HelpCommandsTitle;
            public override string HelpArgumentsTitle() => Messages.HelpArgumentsTitle;
            public override string HelpAdditionalArgumentsTitle() => Messages.HelpAdditionalArgumentsTitle;
            public override string HelpAdditionalArgumentsDescription() => Messages.HelpAdditionalArgumentsDescription;
            public override string HelpUsageOptions() => Messages.HelpUsageOptions;
            public override string HelpUsageCommand() => Messages.HelpUsageCommand;
            public override string HelpUsageAdditionalArguments() => Messages.HelpUsageAdditionalArguments;
            public override string HelpOptionsRequiredLabel() => Messages.HelpOptionsRequiredLabel;
            public override string HelpArgumentDefaultValueLabel() => Messages.HelpArgumentDefaultValueLabel;
            public override string HelpOptionDescription() => Messages.HelpOptionDescription;
            public override string VersionOptionDescription() => Messages.VersionOptionDescription;
        }
    }
}
