using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Presentation;
using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Presentation.Auth;
using NuitsJp.GistGet.Presentation.GistConfig;
using NuitsJp.GistGet.Presentation.Sync;
using NuitsJp.GistGet.Presentation.WinGet;

namespace NuitsJp.GistGet.Presentation;

/// <summary>
/// コマンド実行サービス（アーキテクチャ改善版）
/// </summary>
public class CommandRouter : ICommandRouter
{
    private readonly AuthCommand _authCommand;
    private readonly GistSetCommand _gistSetCommand;
    private readonly GistStatusCommand _gistStatusCommand;
    private readonly GistShowCommand _gistShowCommand;
    private readonly SyncCommand _syncCommand;
    private readonly WinGetCommand _winGetCommand;
    private readonly ILogger<CommandRouter> _logger;
    private readonly IErrorMessageService _errorMessageService;

    public CommandRouter(
        AuthCommand authCommand,
        GistSetCommand gistSetCommand,
        GistStatusCommand gistStatusCommand,
        GistShowCommand gistShowCommand,
        SyncCommand syncCommand,
        WinGetCommand winGetCommand,
        ILogger<CommandRouter> logger,
        IErrorMessageService errorMessageService)
    {
        _authCommand = authCommand;
        _gistSetCommand = gistSetCommand;
        _gistStatusCommand = gistStatusCommand;
        _gistShowCommand = gistShowCommand;
        _syncCommand = syncCommand;
        _winGetCommand = winGetCommand;
        _logger = logger;
        _errorMessageService = errorMessageService;
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            _logger.LogInformation("Executing command with args: {Args}", string.Join(" ", args));

            if (args.Length == 0)
            {
                // 引数がない場合はwingetのヘルプを表示
                return await _winGetCommand.ExecutePassthroughAsync(new[] { "--help" });
            }

            var command = args[0].ToLowerInvariant();
            return await RouteCommandAsync(command, args);
        }
        catch (System.Runtime.InteropServices.COMException comEx)
        {
            _errorMessageService.HandleComException(comEx);
            return 1;
        }
        catch (HttpRequestException httpEx)
        {
            _errorMessageService.HandleNetworkException(httpEx);
            return 1;
        }
        catch (InvalidOperationException invEx) when (invEx.Message.Contains("not found"))
        {
            _errorMessageService.HandlePackageNotFoundException(invEx);
            return 1;
        }
        catch (Exception ex)
        {
            _errorMessageService.HandleUnexpectedException(ex);
            return 1;
        }
    }

    private async Task<int> RouteCommandAsync(string command, string[] args)
    {
        var usesCom = command is "install" or "uninstall" or "upgrade";
        var usesGist = command is "sync";
        var usesPassthrough = command is "export" or "import" or "list" or "search" or "show";
        var isAuthCommand = command is "auth";
        var isGistSubCommand = command is "gist";


        if (isAuthCommand)
        {
            return await _authCommand.ExecuteAsync(args);
        }

        if (isGistSubCommand)
        {
            return await HandleGistSubCommandAsync(args);
        }



        if (usesGist)
        {
            return await HandleGistCommandAsync(command, args);
        }

        if (usesCom)
        {
            return await HandleComCommandAsync(command, args);
        }

        if (usesPassthrough)
        {
            _logger.LogDebug("Routing to passthrough client for explicit command: {Command}", command);
            return await _winGetCommand.ExecutePassthroughAsync(args);
        }

        _logger.LogDebug("Routing to passthrough client for unknown command: {Command}", command);
        return await _winGetCommand.ExecutePassthroughAsync(args);
    }

    private async Task<int> HandleGistSubCommandAsync(string[] args)
    {
        if (args.Length < 2)
        {
            System.Console.WriteLine("Usage: gistget gist <command> [options]");
            System.Console.WriteLine();
            System.Console.WriteLine("Commands:");
            System.Console.WriteLine("  set [--gist-id <id>] [--file <filename>]  - Gist設定を行います");
            System.Console.WriteLine("  status                                     - 現在のGist設定状態を表示します");
            System.Console.WriteLine("  show [--raw]                              - Gistの内容を表示します");
            System.Console.WriteLine();
            System.Console.WriteLine("Examples:");
            System.Console.WriteLine("  gistget gist set --gist-id abc123 --file packages.yaml");
            System.Console.WriteLine("  gistget gist set                  # 対話的に設定");
            System.Console.WriteLine("  gistget gist status");
            System.Console.WriteLine("  gistget gist show");
            System.Console.WriteLine("  gistget gist show --raw          # Raw YAML形式で表示");
            return 1;
        }

        var subCommand = args[1].ToLowerInvariant();
        _logger.LogDebug("Routing to Gist sub-command: {SubCommand}", subCommand);

        return subCommand switch
        {
            "set" => await HandleGistSetAsync(args),
            "status" => await _gistStatusCommand.ExecuteAsync(),
            "show" => await HandleGistShowAsync(args),
            _ => await ShowGistSubCommandHelp(subCommand)
        };
    }

    private async Task<int> HandleGistSetAsync(string[] args)
    {
        string? gistId = null;
        string? fileName = null;

        // 引数解析
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--gist-id" && i + 1 < args.Length)
            {
                gistId = args[i + 1];
                i++; // Skip next argument
            }
            else if (args[i] == "--file" && i + 1 < args.Length)
            {
                fileName = args[i + 1];
                i++; // Skip next argument
            }
        }

        return await _gistSetCommand.ExecuteAsync(gistId, fileName);
    }

    private async Task<int> HandleGistShowAsync(string[] args)
    {
        var raw = args.Contains("--raw");
        return await _gistShowCommand.ExecuteAsync(raw);
    }

    private Task<int> ShowGistSubCommandHelp(string invalidSubCommand)
    {
        System.Console.WriteLine($"Error: Unknown gist command '{invalidSubCommand}'");
        System.Console.WriteLine();
        System.Console.WriteLine("Available commands:");
        System.Console.WriteLine("  set     - Gist設定を行います");
        System.Console.WriteLine("  status  - 現在のGist設定状態を表示します");
        System.Console.WriteLine("  show    - Gistの内容を表示します");
        System.Console.WriteLine();
        System.Console.WriteLine("Use 'gistget gist' for more information.");
        return Task.FromResult(1);
    }



    private Task<int> HandleGistCommandAsync(string command, string[] args)
    {
        _logger.LogDebug("Routing to Gist service for command: {Command}", command);
        return command switch
        {
            "sync" => _syncCommand.ExecuteAsync(args),
            _ => throw new ArgumentException($"Unsupported gist command: {command}")
        };
    }

    private async Task<int> HandleComCommandAsync(string command, string[] args)
    {
        _logger.LogDebug("Routing to WinGet COM command: {Command}", command);
        return command switch
        {
            "install" => await _winGetCommand.ExecuteInstallAsync(args),
            "uninstall" => await _winGetCommand.ExecuteUninstallAsync(args),
            "upgrade" => await _winGetCommand.ExecuteUpgradeAsync(args),
            _ => throw new ArgumentException($"Unsupported COM command: {command}")
        };
    }


}