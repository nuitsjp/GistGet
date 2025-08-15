using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Business.Models;
using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Tests.Mocks;

// テスト用のモック実装
public class MockWinGetClient : IWinGetClient
{
    public bool InitializeCalled { get; private set; }
    public string? LastCommand { get; private set; }
    public string[]? LastArgs { get; private set; }

    // テスト制御用プロパティ
    public Exception? ShouldThrowOnInstall { get; set; }
    public Exception? ShouldThrowOnUninstall { get; set; }
    public Exception? ShouldThrowOnUpgrade { get; set; }
    public int ShouldReturnErrorCode { get; set; } = 0;

    public Task InitializeAsync()
    {
        InitializeCalled = true;
        return Task.CompletedTask;
    }

    public Task<int> InstallPackageAsync(string[] args)
    {
        if (ShouldThrowOnInstall != null)
            throw ShouldThrowOnInstall;

        LastCommand = "install";
        LastArgs = args;
        return Task.FromResult(ShouldReturnErrorCode);
    }

    public Task<int> UninstallPackageAsync(string[] args)
    {
        if (ShouldThrowOnUninstall != null)
            throw ShouldThrowOnUninstall;

        LastCommand = "uninstall";
        LastArgs = args;
        return Task.FromResult(ShouldReturnErrorCode);
    }

    public Task<int> UpgradePackageAsync(string[] args)
    {
        if (ShouldThrowOnUpgrade != null)
            throw ShouldThrowOnUpgrade;

        LastCommand = "upgrade";
        LastArgs = args;
        return Task.FromResult(ShouldReturnErrorCode);
    }

    public Task<List<PackageDefinition>> GetInstalledPackagesAsync()
    {
        // テスト用のモックデータを返す
        var packages = new List<PackageDefinition>
        {
            new("TestApp.TestApp") { Version = "1.0.0" },
            new("AnotherApp.AnotherApp") { Version = "2.0.0" }
        };
        return Task.FromResult(packages);
    }

    public Task<List<PackageDefinition>> SearchPackagesAsync(string query)
    {
        // テスト用のモック検索結果を返す
        var packages = new List<PackageDefinition>
        {
            new(query) { Version = "1.0.0" },
            new($"{query}.Extended") { Version = "2.0.0" }
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

    public Task<SyncResult> SyncAsync()
    {
        LastCommand = "sync";
        // REFACTOR段階：より意味的な実装に改善
        SyncStatePersisted = true;
        return Task.FromResult(new SyncResult { ExitCode = 0 });
    }

    public Task ExecuteRebootAsync()
    {
        // テスト用: 何もしない
        return Task.CompletedTask;
    }
}