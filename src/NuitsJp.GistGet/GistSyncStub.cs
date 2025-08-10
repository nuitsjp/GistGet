using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Abstractions;

namespace NuitsJp.GistGet;

/// <summary>
/// Gist同期サービスのスタブ実装（アーキテクチャ改善版）
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

    public async Task<int> ExportAsync()
    {
        _logger.LogInformation("Starting Gist export operation");
        await Task.Delay(500);
        _logger.LogInformation("Gist export completed successfully");
        return 0;
    }

    public async Task<int> ImportAsync()
    {
        _logger.LogInformation("Starting Gist import operation");
        await Task.Delay(500);
        _logger.LogInformation("Gist import completed successfully");
        return 0;
    }
}