using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Abstractions;

namespace NuitsJp.GistGet;

/// <summary>
/// Gist同期サービスのスタブ実装
/// </summary>
public class GistSyncStub : IGistSyncService
{
    private readonly ILogger<GistSyncStub> _logger;

    public GistSyncStub(ILogger<GistSyncStub> logger)
    {
        _logger = logger;
    }

    public void AfterInstall(string packageId)
    {
        _logger.LogInformation("Package installed: {PackageId}, updating Gist", packageId);
    }

    public void AfterUninstall(string packageId)
    {
        _logger.LogInformation("Package uninstalled: {PackageId}, updating Gist", packageId);
    }

    public async Task<int> SyncAsync()
    {
        _logger.LogInformation("Starting Gist sync operation");
        await Task.Delay(500);
        _logger.LogInformation("Gist sync completed successfully");
        return 0;
    }
}