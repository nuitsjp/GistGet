using GistGet.Application.Services;
using GistGet.Utils;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.IO;
using System.Threading.Tasks;

namespace GistGet.Presentation;

public class CliCommandBuilder
{
    private readonly IPackageService _packageService;
    private readonly IGistService _gistService;
    private readonly IAuthService _authService;

    public CliCommandBuilder(IPackageService packageService, IGistService gistService, IAuthService authService)
    {
        _packageService = packageService;
        _gistService = gistService;
        _authService = authService;
    }

    public RootCommand Build()
    {
        var rootCommand = new RootCommand("GistGet - Windows Package Manager Cloud Sync Tool");

        rootCommand.AddCommand(BuildSyncCommand());
        rootCommand.AddCommand(BuildExportCommand());
        rootCommand.AddCommand(BuildImportCommand());
        rootCommand.AddCommand(BuildAuthCommand());

        rootCommand.AddCommand(BuildInstallCommand());
        rootCommand.AddCommand(BuildUninstallCommand());
        rootCommand.AddCommand(BuildUpgradeCommand());

        foreach (var cmd in BuildWingetPassthroughCommands())
        {
            rootCommand.AddCommand(cmd);
        }

        return rootCommand;
    }

    private Command BuildSyncCommand()
    {
        var command = new Command("sync", "Synchronize packages with Gist");
        var urlOption = new Option<string>("--url", "Gist URL to sync from (optional)");
        command.AddOption(urlOption);

        command.SetHandler(async (string? url) =>
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]Starting synchronization...[/]");

                AnsiConsole.MarkupLine("Fetching packages from Gist...");
                var gistPackages = await _gistService.GetPackagesAsync(url);
                AnsiConsole.MarkupLine($"Found [green]{gistPackages.Count}[/] packages in Gist.");

                AnsiConsole.MarkupLine("Fetching installed packages...");
                var localPackages = await _packageService.GetInstalledPackagesAsync();
                AnsiConsole.MarkupLine($"Found [green]{localPackages.Count}[/] installed packages.");

                var result = await _packageService.SyncAsync(gistPackages, localPackages);

                if (result.Installed.Count == 0 && result.Uninstalled.Count == 0 && result.Failed.Count == 0)
                {
                    AnsiConsole.MarkupLine("[green]System is already in sync![/]");
                }
                else
                {
                    foreach (var pkg in result.Uninstalled) AnsiConsole.MarkupLine($"[green]Uninstalled {pkg.Id}[/]");
                    foreach (var pkg in result.Installed) AnsiConsole.MarkupLine($"[green]Installed {pkg.Id}[/]");
                    foreach (var err in result.Errors) AnsiConsole.MarkupLine($"[red]{err}[/]");

                    AnsiConsole.MarkupLine("[bold green]Synchronization completed![/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold red]Synchronization failed: {ex.Message}[/]");
            }
        }, urlOption);

        return command;
    }

    private Command BuildExportCommand()
    {
        var command = new Command("export", "Export current package state to YAML");
        var outputOption = new Option<string>("--output", "Output file path");
        command.AddOption(outputOption);

        command.SetHandler(async (string? output) =>
        {
            try
            {
                AnsiConsole.MarkupLine("Fetching installed packages...");
                var packages = await _packageService.GetInstalledPackagesAsync();
                var yaml = YamlHelper.Serialize(packages);

                if (!string.IsNullOrEmpty(output))
                {
                    await File.WriteAllTextAsync(output, yaml);
                    AnsiConsole.MarkupLine($"[green]Exported {packages.Count} packages to {output}[/]");
                }
                else
                {
                    AnsiConsole.Write(new Text(yaml));
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Export failed: {ex.Message}[/]");
            }
        }, outputOption);

        return command;
    }

    private Command BuildImportCommand()
    {
        var command = new Command("import", "Import YAML file to Gist");
        var fileArgument = new Argument<string>("file", "YAML file to import");
        command.AddArgument(fileArgument);

        command.SetHandler(async (string file) =>
        {
            if (!File.Exists(file))
            {
                AnsiConsole.MarkupLine($"[red]File not found: {file}[/]");
                return;
            }

            var yaml = await File.ReadAllTextAsync(file);
            var packages = YamlHelper.Deserialize(yaml);
            await _gistService.SavePackagesAsync(packages);
        }, fileArgument);

        return command;
    }

    private Command BuildAuthCommand()
    {
        var command = new Command("auth", "Manage authentication");

        var login = new Command("login", "Login to GitHub");
        login.SetHandler(async () => await _authService.LoginAsync());
        command.AddCommand(login);

        var logout = new Command("logout", "Logout from GitHub");
        logout.SetHandler(async () => await _authService.LogoutAsync());
        command.AddCommand(logout);

        var status = new Command("status", "Check authentication status");
        status.SetHandler(async () =>
        {
            if (await _authService.IsAuthenticatedAsync())
            {
                var userInfo = await _authService.GetUserInfoAsync();
                if (userInfo != null)
                {
                    AnsiConsole.MarkupLine("[green]Authenticated[/]");
                    AnsiConsole.MarkupLine($"User: [cyan]{userInfo.Login}[/]");
                    if (!string.IsNullOrEmpty(userInfo.Name))
                    {
                        AnsiConsole.MarkupLine($"Name: [cyan]{userInfo.Name}[/]");
                    }
                    if (!string.IsNullOrEmpty(userInfo.Email))
                    {
                        AnsiConsole.MarkupLine($"Email: [cyan]{userInfo.Email}[/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[green]Authenticated[/]");
                    AnsiConsole.MarkupLine("[yellow]Unable to fetch user information[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Not authenticated[/]");
            }
        });
        command.AddCommand(status);

        return command;
    }

    private Command BuildInstallCommand()
    {
        var command = new Command("install", "Install a package and save to Gist");
        var idArgument = new Argument<string>("package", "Package ID");
        
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

        command.AddArgument(idArgument);
        command.AddOption(versionOption);
        command.AddOption(scopeOption);
        command.AddOption(archOption);
        command.AddOption(locationOption);
        command.AddOption(interactiveOption);
        command.AddOption(silentOption);
        command.AddOption(logOption);
        command.AddOption(overrideOption);
        command.AddOption(forceOption);
        command.AddOption(skipDependenciesOption);
        command.AddOption(headerOption);
        command.AddOption(installerTypeOption);
        command.AddOption(customOption);

        var binder = new InstallPackageBinder(
            idArgument, versionOption, scopeOption, archOption, locationOption,
            interactiveOption, silentOption, logOption, overrideOption, forceOption,
            skipDependenciesOption, headerOption, installerTypeOption, customOption);

        command.SetHandler(async (package) =>
        {
            if (await _packageService.InstallAndSaveAsync(package))
            {
                AnsiConsole.MarkupLine($"[green]Installed and saved {package.Id}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to install {package.Id}[/]");
            }
        }, binder);

        return command;
    }

    private Command BuildUninstallCommand()
    {
        var command = new Command("uninstall", "Uninstall a package and update Gist");
        var idArgument = new Argument<string>("package", "Package ID");
        command.AddArgument(idArgument);

        command.SetHandler(async (string id) =>
        {
            if (await _packageService.UninstallAndSaveAsync(id))
            {
                AnsiConsole.MarkupLine($"[green]Uninstalled and updated Gist for {id}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to uninstall {id}[/]");
            }
        }, idArgument);

        return command;
    }

    private Command BuildUpgradeCommand()
    {
        var command = new Command("upgrade", "Upgrade a package and save to Gist");
        var idArgument = new Argument<string>("package", "Package ID");
        var versionOption = new Option<string>("--version", "Package version");
        command.AddArgument(idArgument);
        command.AddOption(versionOption);

        command.SetHandler(async (string id, string? version) =>
        {
            if (await _packageService.UpgradeAndSaveAsync(id, version))
            {
                AnsiConsole.MarkupLine($"[green]Upgraded and saved {id}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to upgrade {id}[/]");
            }
        }, idArgument, versionOption);

        return command;
    }

    private Command[] BuildWingetPassthroughCommands()
    {
        var commands = new System.Collections.Generic.List<Command>();
        var wingetCommands = new[] {
            "list", "search", "show", "source", "settings", "features",
            "hash", "validate", "configure", "download", "repair", "pin"
        };

        foreach (var cmd in wingetCommands)
        {
            var command = new Command(cmd, $"Pass through to winget {cmd}");
            var argsArgument = new Argument<string[]>("arguments") { Arity = ArgumentArity.ZeroOrMore };
            command.AddArgument(argsArgument);

            command.SetHandler(async (string[] arguments) =>
            {
                await _packageService.RunPassthroughAsync(cmd, arguments);
            }, argsArgument);

            commands.Add(command);
        }

        return commands.ToArray();
    }
    private class InstallPackageBinder : BinderBase<GistGet.Models.GistGetPackage>
    {
        private readonly Argument<string> _id;
        private readonly Option<string> _version;
        private readonly Option<string> _scope;
        private readonly Option<string> _arch;
        private readonly Option<string> _location;
        private readonly Option<bool> _interactive;
        private readonly Option<bool> _silent;
        private readonly Option<string> _log;
        private readonly Option<string> _override;
        private readonly Option<bool> _force;
        private readonly Option<bool> _skipDependencies;
        private readonly Option<string> _header;
        private readonly Option<string> _installerType;
        private readonly Option<string> _custom;

        public InstallPackageBinder(
            Argument<string> id,
            Option<string> version,
            Option<string> scope,
            Option<string> arch,
            Option<string> location,
            Option<bool> interactive,
            Option<bool> silent,
            Option<string> log,
            Option<string> overrideArgs,
            Option<bool> force,
            Option<bool> skipDependencies,
            Option<string> header,
            Option<string> installerType,
            Option<string> custom)
        {
            _id = id;
            _version = version;
            _scope = scope;
            _arch = arch;
            _location = location;
            _interactive = interactive;
            _silent = silent;
            _log = log;
            _override = overrideArgs;
            _force = force;
            _skipDependencies = skipDependencies;
            _header = header;
            _installerType = installerType;
            _custom = custom;
        }

        protected override GistGet.Models.GistGetPackage GetBoundValue(BindingContext bindingContext)
        {
            return new GistGet.Models.GistGetPackage
            {
                Id = bindingContext.ParseResult.GetValueForArgument(_id),
                Version = bindingContext.ParseResult.GetValueForOption(_version),
                Scope = bindingContext.ParseResult.GetValueForOption(_scope),
                Architecture = bindingContext.ParseResult.GetValueForOption(_arch),
                Location = bindingContext.ParseResult.GetValueForOption(_location),
                Interactive = bindingContext.ParseResult.GetValueForOption(_interactive),
                Silent = bindingContext.ParseResult.GetValueForOption(_silent),
                Log = bindingContext.ParseResult.GetValueForOption(_log),
                Override = bindingContext.ParseResult.GetValueForOption(_override),
                Force = bindingContext.ParseResult.GetValueForOption(_force),
                SkipDependencies = bindingContext.ParseResult.GetValueForOption(_skipDependencies),
                Header = bindingContext.ParseResult.GetValueForOption(_header),
                InstallerType = bindingContext.ParseResult.GetValueForOption(_installerType),
                Custom = bindingContext.ParseResult.GetValueForOption(_custom)
            };
        }
    }
}
