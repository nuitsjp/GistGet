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
    public string? LastOutput { get; private set; }

    // テスト用のサンプル出力データ
    private readonly Dictionary<string, string> _mockOutputs = new()
    {
        { "list", "Name        Id                  Version  Source\nGit for Windows  Git.Git             2.43.0   winget\nNotePad++     Notepad++.Notepad++  8.6.8    winget" },
        { "search notepad", "Name        Id                  Version  Source\nNotePad++     Notepad++.Notepad++  8.6.8    winget" },
        { "show Git.Git", "Found Git for Windows [Git.Git]\nVersion: 2.43.0\nPublisher: The Git Development Community" },
        { "export", "{\"Sources\":[{\"Packages\":[{\"PackageIdentifier\":\"Git.Git\",\"Version\":\"2.43.0\"}],\"SourceDetails\":{\"Argument\":\"https://cdn.winget.microsoft.com/cache\",\"Identifier\":\"winget\",\"Name\":\"winget\",\"Type\":\"Microsoft.PreIndexed.Package\"}}],\"WinGetVersion\":\"1.7.10661\"}" },
        { "import", "Successfully imported packages." }
    };

    public Task<int> ExecuteAsync(string[] args)
    {
        LastArgs = args;

        // 引数に基づいて適切なモック出力を生成
        var command = args.Length > 0 ? args[0] : "";
        var fullCommand = string.Join(" ", args);

        LastOutput = command switch
        {
            "export" => _mockOutputs["export"],
            "import" => _mockOutputs["import"],
            "list" => _mockOutputs["list"],
            "show" when args.Contains("Git.Git") => _mockOutputs["show Git.Git"],
            "search" when fullCommand.Contains("notepad") => _mockOutputs["search notepad"],
            _ => $"Mock output for: {fullCommand}"
        };

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