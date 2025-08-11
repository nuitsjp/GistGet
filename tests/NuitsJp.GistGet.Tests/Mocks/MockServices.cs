using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Business;

namespace NuitsJp.GistGet.Tests.Mocks;

// テスト用のモック実装
public class MockWinGetClient : IWinGetClient
{
    public bool InitializeCalled { get; private set; }
    public string? LastCommand { get; private set; }
    public string[]? LastArgs { get; private set; }

    public Task InitializeAsync()
    {
        InitializeCalled = true;
        return Task.CompletedTask;
    }

    public Task<int> InstallPackageAsync(string[] args)
    {
        LastCommand = "install";
        LastArgs = args;
        return Task.FromResult(0);
    }

    public Task<int> UninstallPackageAsync(string[] args)
    {
        LastCommand = "uninstall";
        LastArgs = args;
        return Task.FromResult(0);
    }

    public Task<int> UpgradePackageAsync(string[] args)
    {
        LastCommand = "upgrade";
        LastArgs = args;
        return Task.FromResult(0);
    }

    public Task<List<(string Id, string Name, string Version)>> GetInstalledPackagesAsync()
    {
        // テスト用のモックデータを返す
        var packages = new List<(string Id, string Name, string Version)>
        {
            ("TestApp.TestApp", "Test Application", "1.0.0"),
            ("AnotherApp.AnotherApp", "Another Application", "2.0.0")
        };
        return Task.FromResult(packages);
    }
}

public class MockWinGetPassthroughClient : IWinGetPassthroughClient
{
    public string[]? LastArgs { get; private set; }

    public Task<int> ExecuteAsync(string[] args)
    {
        LastArgs = args;
        return Task.FromResult(0);
    }
}

public class MockGistSyncService : IGistSyncService
{
    public string? LastCommand { get; private set; }
    public bool SyncStatePersisted { get; private set; } = false;

    public void AfterInstall(string packageId)
    {
        // テスト用: 何もしない
    }

    public void AfterUninstall(string packageId)
    {
        // テスト用: 何もしない
    }

    public Task<int> SyncAsync()
    {
        LastCommand = "sync";
        // REFACTOR段階：より意味的な実装に改善
        SyncStatePersisted = true;
        return Task.FromResult(0);
    }
}