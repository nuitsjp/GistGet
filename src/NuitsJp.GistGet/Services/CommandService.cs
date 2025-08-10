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

    public CommandService(
        IWinGetClient winGetClient,
        IWinGetPassthroughClient passthroughClient,
        IGistSyncService gistSyncService,
        ILogger<CommandService> logger)
    {
        _winGetClient = winGetClient;
        _passthroughClient = passthroughClient;
        _gistSyncService = gistSyncService;
        _logger = logger;
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
            var usesCom = command is "install" or "uninstall" or "upgrade";
            var usesGist = command is "sync" or "export" or "import";

            // テスト用コマンドの追加
            if (command == "test-list")
            {
                _logger.LogDebug("Executing test-list command");
                await _winGetClient.InitializeAsync();
                var packages = await ((WinGetComClient)_winGetClient).GetInstalledPackagesAsync();
                
                Console.WriteLine($"Found {packages.Count} installed packages:");
                foreach (var (id, name, version) in packages.Take(10)) // 最初の10個を表示
                {
                    Console.WriteLine($"  {id} | {name} | {version}");
                }
                if (packages.Count > 10)
                {
                    Console.WriteLine($"  ... and {packages.Count - 10} more packages");
                }
                return 0;
            }

            if (command == "test-search")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: gistget test-search <query>");
                    return 1;
                }
                
                _logger.LogDebug("Executing test-search command");
                await _winGetClient.InitializeAsync();
                var packages = await ((WinGetComClient)_winGetClient).SearchPackagesAsync(args[1]);
                
                Console.WriteLine($"Found {packages.Count} packages matching '{args[1]}':");
                foreach (var (id, name, version) in packages.Take(10)) // 最初の10個を表示
                {
                    Console.WriteLine($"  {id} | {name} | {version}");
                }
                if (packages.Count > 10)
                {
                    Console.WriteLine($"  ... and {packages.Count - 10} more packages");
                }
                return 0;
            }

            if (usesGist)
            {
                _logger.LogDebug("Routing to Gist service for command: {Command}", command);
                return command switch
                {
                    "sync" => await _gistSyncService.SyncAsync(),
                    "export" => await _gistSyncService.ExportAsync(),
                    "import" => await _gistSyncService.ImportAsync(),
                    _ => await _passthroughClient.ExecuteAsync(args)
                };
            }

            if (usesCom)
            {
                _logger.LogDebug("Routing to COM client for command: {Command}", command);
                await _winGetClient.InitializeAsync();
                
                return command switch
                {
                    "install" => await _winGetClient.InstallPackageAsync(args),
                    "uninstall" => await _winGetClient.UninstallPackageAsync(args),
                    "upgrade" => await _winGetClient.UpgradePackageAsync(args),
                    _ => await _passthroughClient.ExecuteAsync(args)
                };
            }

            _logger.LogDebug("Routing to passthrough client for command: {Command}", command);
            return await _passthroughClient.ExecuteAsync(args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command");
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}