using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Abstractions;

namespace NuitsJp.GistGet.Services;

/// <summary>
/// コマンド実行サービス（アーキテクチャ改善版）
/// </summary>
public class CommandService : ICommandService
{
    private readonly IWinGetClient _winGetClient;
    private readonly IWinGetPassthroughClient _passthroughClient;
    private readonly IGistSyncService _gistSyncService;
    private readonly ILogger<CommandService> _logger;
    private readonly IErrorMessageService _errorMessageService;

    public CommandService(
        IWinGetClient winGetClient,
        IWinGetPassthroughClient passthroughClient,
        IGistSyncService gistSyncService,
        ILogger<CommandService> logger,
        IErrorMessageService errorMessageService)
    {
        _winGetClient = winGetClient;
        _passthroughClient = passthroughClient;
        _gistSyncService = gistSyncService;
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
        var usesGist = command is "sync" or "export" or "import";

        if (usesGist)
        {
            return await HandleGistCommandAsync(command);
        }

        if (usesCom)
        {
            return await HandleComCommandAsync(command, args);
        }

        _logger.LogDebug("Routing to passthrough client for command: {Command}", command);
        return await _passthroughClient.ExecuteAsync(args);
    }

    private async Task<int> HandleGistCommandAsync(string command)
    {
        _logger.LogDebug("Routing to Gist service for command: {Command}", command);
        return command switch
        {
            "sync" => await _gistSyncService.SyncAsync(),
            "export" => await _gistSyncService.ExportAsync(),
            "import" => await _gistSyncService.ImportAsync(),
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