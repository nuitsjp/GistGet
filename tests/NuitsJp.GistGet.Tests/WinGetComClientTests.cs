using Microsoft.Extensions.Logging;
using NuitsJp.GistGet;
using NuitsJp.GistGet.Tests.Mocks;
using Shouldly;
using Xunit;

namespace NuitsJp.GistGet.Tests;

public class WinGetComClientTests
{
    private readonly ILogger<WinGetComClient> _logger;
    private readonly MockGistSyncService _mockGistSyncService;

    public WinGetComClientTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<WinGetComClient>();
        _mockGistSyncService = new MockGistSyncService();
    }

    [Fact(Skip = "COM API requires proper environment setup - covered by integration tests")]
    public async Task InitializeAsync_ShouldSucceed_WhenCOMAPIIsAvailable()
    {
        // このテストはCOM APIが適切に設定された環境でのみ実行可能
        // 実際のテストはgistget test-listなどの統合テストで確認
        await Task.CompletedTask;
    }

    [Fact(Skip = "COM API requires proper environment setup - covered by integration tests")]
    public async Task GetInstalledPackagesAsync_ShouldReturnPackages_WhenInitialized()
    {
        // このテストはCOM APIが適切に設定された環境でのみ実行可能
        // 実際のテストはgistget test-listなどの統合テストで確認
        await Task.CompletedTask;
    }

    [Fact(Skip = "COM API requires proper environment setup - covered by integration tests")]
    public async Task SearchPackagesAsync_ShouldReturnResults_WhenQueryIsValid()
    {
        // このテストはCOM APIが適切に設定された環境でのみ実行可能
        // 実際のテストはgistget test-searchなどの統合テストで確認
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetInstalledPackagesAsync_ShouldThrow_WhenNotInitialized()
    {
        // Arrange
        var client = new WinGetComClient(_mockGistSyncService, _logger);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await client.GetInstalledPackagesAsync());
    }

    [Fact]
    public async Task SearchPackagesAsync_ShouldThrow_WhenNotInitialized()
    {
        // Arrange
        var client = new WinGetComClient(_mockGistSyncService, _logger);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await client.SearchPackagesAsync("test"));
    }
}

