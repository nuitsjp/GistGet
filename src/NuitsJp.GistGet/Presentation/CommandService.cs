using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Presentation;
using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Presentation.Commands;

namespace NuitsJp.GistGet.Presentation;

/// <summary>
/// コマンド実行サービス（アーキテクチャ改善版）
/// </summary>
public class CommandService : ICommandRouter
{
    private readonly IWinGetClient _winGetClient;
    private readonly IWinGetPassthroughClient _passthroughClient;
    private readonly IGistSyncService _gistSyncService;
    private readonly AuthCommand _authCommand;
    private readonly TestGistCommand _testGistCommand;
    private readonly GistSetCommand _gistSetCommand;
    private readonly GistStatusCommand _gistStatusCommand;
    private readonly GistShowCommand _gistShowCommand;
    private readonly ILogger<CommandService> _logger;
    private readonly IErrorMessageService _errorMessageService;

    public CommandService(
        IWinGetClient winGetClient,
        IWinGetPassthroughClient passthroughClient,
        IGistSyncService gistSyncService,
        AuthCommand authCommand,
        TestGistCommand testGistCommand,
        GistSetCommand gistSetCommand,
        GistStatusCommand gistStatusCommand,
        GistShowCommand gistShowCommand,
        ILogger<CommandService> logger,
        IErrorMessageService errorMessageService)
    {
        _winGetClient = winGetClient;
        _passthroughClient = passthroughClient;
        _gistSyncService = gistSyncService;
        _authCommand = authCommand;
        _testGistCommand = testGistCommand;
        _gistSetCommand = gistSetCommand;
        _gistStatusCommand = gistStatusCommand;
        _gistShowCommand = gistShowCommand;
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
                return await _passthroughClient.ExecuteAsync(args);
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
        var usesGist = command is "sync";  // export/importを除外
        var isAuthCommand = command is "auth";
        var isTestGistCommand = command is "test-gist";
        var isGistSubCommand = command is "gist";

        if (isAuthCommand)
        {
            return await _authCommand.ExecuteAsync(args);
        }

        if (isTestGistCommand)
        {
            return await _testGistCommand.ExecuteAsync();
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

        _logger.LogDebug("Routing to passthrough client for command: {Command}", command);
        return await _passthroughClient.ExecuteAsync(args);
    }

    private async Task<int> HandleGistSubCommandAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: gistget gist <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  set [--gist-id <id>] [--file <filename>]  - Gist設定を行います");
            Console.WriteLine("  status                                     - 現在のGist設定状態を表示します");
            Console.WriteLine("  show [--raw]                              - Gistの内容を表示します");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  gistget gist set --gist-id abc123 --file packages.yaml");
            Console.WriteLine("  gistget gist set                  # 対話的に設定");
            Console.WriteLine("  gistget gist status");
            Console.WriteLine("  gistget gist show");
            Console.WriteLine("  gistget gist show --raw          # Raw YAML形式で表示");
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
        Console.WriteLine($"Error: Unknown gist command '{invalidSubCommand}'");
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        Console.WriteLine("  set     - Gist設定を行います");
        Console.WriteLine("  status  - 現在のGist設定状態を表示します");
        Console.WriteLine("  show    - Gistの内容を表示します");
        Console.WriteLine();
        Console.WriteLine("Use 'gistget gist' for more information.");
        return Task.FromResult(1);
    }

    private async Task<int> HandleGistCommandAsync(string command, string[] args)
    {
        _logger.LogDebug("Routing to Gist service for command: {Command}", command);
        return command switch
        {
            "sync" => await _gistSyncService.SyncAsync(),
            _ => throw new ArgumentException($"Unsupported gist command: {command}")
        };
    }

    private async Task<int> HandleComCommandAsync(string command, string[] args)
    {
        _logger.LogDebug("Routing to COM client for command: {Command}", command);
        await _winGetClient.InitializeAsync();

        return command switch
        {
            "install" => await _winGetClient.InstallPackageAsync(args),
            "uninstall" => await _winGetClient.UninstallPackageAsync(args),
            "upgrade" => await _winGetClient.UpgradePackageAsync(args),
            _ => throw new ArgumentException($"Unsupported COM command: {command}")
        };
    }


}