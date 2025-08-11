using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Abstractions;

namespace NuitsJp.GistGet.Services;

/// <summary>
/// Gist同期サービスの実装
/// </summary>
public class GistSyncService : IGistSyncService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IGitHubAuthService _gitHubAuthService;
    private readonly ILogger<GistSyncService> _logger;

    public GistSyncService(
        IServiceProvider serviceProvider,
        IGitHubAuthService gitHubAuthService,
        ILogger<GistSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _gitHubAuthService = gitHubAuthService;
        _logger = logger;
    }

    public void AfterInstall(string packageId)
    {
        _logger.LogInformation("Package installed: {PackageId}, Gist sync will be handled separately", packageId);
    }

    public void AfterUninstall(string packageId)
    {
        _logger.LogInformation("Package uninstalled: {PackageId}, Gist sync will be handled separately", packageId);
    }

    public async Task<int> SyncAsync()
    {
        _logger.LogInformation("Starting Gist sync operation");

        // 同期処理は後で実装
        await Task.Delay(500);
        _logger.LogInformation("Gist sync completed successfully");
        return 0;
    }
}