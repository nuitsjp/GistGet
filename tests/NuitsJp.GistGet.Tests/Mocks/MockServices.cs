using NuitsJp.GistGet.Abstractions;

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
    public bool ExportFileGenerated { get; private set; } = false;
    public bool ImportFileProcessed { get; private set; } = false;

    public void AfterInstall(string packageId)
    {
        // テスト用: 何もしない
    }

    public void AfterUninstall(string packageId)
    {
        // テスト用: 何もしない
    }

    public Task<int> ExportAsync()
    {
        LastCommand = "export";
        // REFACTOR段階：より意味的な実装に改善
        ExportFileGenerated = true;
        return Task.FromResult(0);
    }

    public Task<int> ImportAsync()
    {
        LastCommand = "import";
        // REFACTOR段階：より意味的な実装に改善
        ImportFileProcessed = true;
        return Task.FromResult(0);
    }

    public Task<int> SyncAsync()
    {
        LastCommand = "sync";
        // REFACTOR段階：より意味的な実装に改善
        SyncStatePersisted = true;
        return Task.FromResult(0);
    }
}