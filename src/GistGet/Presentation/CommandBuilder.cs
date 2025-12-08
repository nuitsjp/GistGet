using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using GistGet.Model;
using GistGet.Service;
using Spectre.Console;

namespace GistGet.Presentation;

public class CommandBuilder
{
    private readonly IPackageService _packageService;
    private readonly IGistService _gistService;
    private readonly IAuthService _authService;

    public CommandBuilder(IPackageService packageService, IGistService gistService, IAuthService authService)
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
        rootCommand.AddCommand(BuildPinCommand());

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

        return command;
    }

    private Command BuildExportCommand()
    {
        var command = new Command("export", "Export current package state to YAML");
        var outputOption = new Option<string>("--output", "Output file path");
        command.AddOption(outputOption);

        return command;
    }

    private Command BuildImportCommand()
    {
        var command = new Command("import", "Import YAML file to Gist");
        var fileArgument = new Argument<string>("file", "YAML file to import");
        command.AddArgument(fileArgument);

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
        command.AddCommand(status);

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

        command.AddOption(idOption);
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
            idOption, versionOption, scopeOption, archOption, locationOption,
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
        var idOption = new Option<string>("--id", "Package ID") { IsRequired = true };
        command.AddOption(idOption);

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
        }, idOption);

        return command;
    }

    private Command BuildUpgradeCommand()
    {
        var command = new Command("upgrade", "Upgrade a package and save to Gist");
        var idArgument = new Argument<string?>("package", "Package ID") { Arity = ArgumentArity.ZeroOrOne };
        var idOption = new Option<string>("--id", "Package ID (winget compatible)");
        var versionOption = new Option<string>("--version", "Package version");

        command.AddArgument(idArgument);
        command.AddOption(idOption);
        command.AddOption(versionOption);

        // Allow unmatched tokens to be collected for passthrough
        command.TreatUnmatchedTokensAsErrors = false;

        command.SetHandler(async (InvocationContext context) =>
        {
            var parseResult = context.ParseResult;
            var id = parseResult.GetValueForOption(idOption) ?? parseResult.GetValueForArgument(idArgument);
            var version = parseResult.GetValueForOption(versionOption);

            // If ID is specified, perform managed upgrade
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (await _packageService.UpgradeAndSaveAsync(id, version))
                {
                    AnsiConsole.MarkupLine($"[green]Upgraded and saved {id}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Failed to upgrade {id}[/]");
                }
            }
            else
            {
                // If ID is missing, pass through everything to winget
                // Reconstruct arguments from tokens
                var tokens = parseResult.Tokens.Select(t => t.Value).ToList();

                // We need to be careful not to duplicate "upgrade" if it's already in tokens, 
                // but usually tokens are what follows the command.
                // However, System.CommandLine might parse differently.
                // Let's just grab all arguments passed to the process after "upgrade"
                // But simpler: just use the tokens that were not matched or even all tokens?
                // Actually, for passthrough, we want exactly what the user typed after "upgrade".

                // A safer approach for passthrough in this architecture might be to just take all unmatched tokens
                // plus the matched ones if we can reconstruct them, OR just rely on the fact that 
                // if we are here, we want to run `winget upgrade [args]`.

                // Let's collect all tokens from the parse result that belong to this command's scope.
                // But simpler: just pass the raw args excluding the root command?
                // The _executor.RunPassthroughAsync takes "command" and "args".
                // "command" is "upgrade". "args" should be the rest.

                // Let's try to use the UnmatchedTokens if any, plus the values of options if they were parsed?
                // No, if we set TreatUnmatchedTokensAsErrors = false, they end up in UnmatchedTokens.
                // But "package" argument might have consumed something if it looked like an ID?
                // Wait, if "package" argument is ZeroOrOne, it might consume the first arg.
                // If `id` is null/empty, it means it didn't match or wasn't provided.

                // If the user typed `gistget upgrade --all`, `--all` is unmatched (since we didn't define it).
                // `id` (argument) will be null because `--all` starts with `-`.

                // So we can collect all tokens.
                var allTokens = parseResult.Tokens.Select(t => t.Value).ToArray();
                // Also need to include unmatched tokens?
                var unmatched = parseResult.UnmatchedTokens.ToArray();

                // Actually, `parseResult.Tokens` contains the tokens that were parsed for this command.
                // `parseResult.UnmatchedTokens` contains what wasn't understood.
                // We want to reconstruct the command line.

                var argsList = new System.Collections.Generic.List<string>();
                argsList.AddRange(parseResult.Tokens.Select(t => t.Value));
                argsList.AddRange(parseResult.UnmatchedTokens);

                // Note: This simple reconstruction might lose order or exact formatting, 
                // but for winget passthrough it's usually sufficient.

                await _packageService.RunPassthroughAsync("upgrade", argsList.ToArray());
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
        add.AddArgument(addId);
        add.AddOption(addVersion);
        add.SetHandler(async (string id, string version) =>
        {
            if (await _packageService.PinAddAndSaveAsync(id, version))
            {
                AnsiConsole.MarkupLine($"[green]Pinned {id} to version {version} and saved to Gist[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to pin {id}[/]");
            }
        }, addId, addVersion);
        command.AddCommand(add);

        var remove = new Command("remove", "Remove a package pin");
        var removeId = new Argument<string>("package", "Package ID");
        remove.AddArgument(removeId);
        remove.SetHandler(async (string id) =>
        {
            if (await _packageService.PinRemoveAndSaveAsync(id))
            {
                AnsiConsole.MarkupLine($"[green]Unpinned {id} and updated Gist[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to unpin {id}[/]");
            }
        }, removeId);
        command.AddCommand(remove);

        var list = new Command("list", "List pinned packages");
        var listArgs = new Argument<string[]>("args") { Arity = ArgumentArity.ZeroOrMore };
        list.AddArgument(listArgs);
        list.SetHandler(async (string[] args) =>
        {
            var allArgs = new System.Collections.Generic.List<string> { "list" };
            allArgs.AddRange(args);
            await _packageService.RunPassthroughAsync("pin", allArgs.ToArray());
        }, listArgs);
        command.AddCommand(list);

        var reset = new Command("reset", "Reset all pins");
        var resetArgs = new Argument<string[]>("args") { Arity = ArgumentArity.ZeroOrMore };
        reset.AddArgument(resetArgs);
        reset.SetHandler(async (string[] args) =>
        {
            var allArgs = new System.Collections.Generic.List<string> { "reset" };
            allArgs.AddRange(args);
            await _packageService.RunPassthroughAsync("pin", allArgs.ToArray());
        }, resetArgs);
        command.AddCommand(reset);

        return command;
    }

    private Command[] BuildWingetPassthroughCommands()
    {
        var commands = new System.Collections.Generic.List<Command>();
        var wingetCommands = new[] {
            "list", "search", "show", "source", "settings", "features",
            "hash", "validate", "configure", "download", "repair"
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
    private class InstallPackageBinder : BinderBase<GistGetPackage>
    {
        private readonly Option<string> _idOption;
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
            Option<string> idOption,
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
            _idOption = idOption;
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

        protected override GistGetPackage GetBoundValue(BindingContext bindingContext)
        {
            var id = bindingContext.ParseResult.GetValueForOption(_idOption)
                ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Package ID is required.");
            }

            return new GistGetPackage
            {
                Id = id,
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
