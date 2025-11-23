using GistGet.Application.Services;
using GistGet.Utils;
using Spectre.Console;
using System;
using System.CommandLine;
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
                AnsiConsole.MarkupLine("[green]Authenticated[/]");
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

        command.AddArgument(idArgument);
        command.AddOption(versionOption);

        command.SetHandler(async (string id, string? version) =>
        {
            var package = new GistGet.Models.GistGetPackage { Id = id, Version = version };
            if (await _packageService.InstallAndSaveAsync(package))
            {
                AnsiConsole.MarkupLine($"[green]Installed and saved {id}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to install {id}[/]");
            }
        }, idArgument, versionOption);

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
}
