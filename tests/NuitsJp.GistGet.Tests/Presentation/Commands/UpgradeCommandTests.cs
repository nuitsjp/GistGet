using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Tests.Mocks;
using Shouldly;
using Xunit;

namespace NuitsJp.GistGet.Tests.Presentation.Commands;

/// <summary>
/// t-wada式TDD用UpgradeCommandテスト
/// Red-Green-Refactorサイクルで進める
/// </summary>
public class UpgradeCommandTests
{
    private readonly ILogger<WinGetComClient> _logger;
    private readonly MockGistSyncService _mockGistSyncService;
    private readonly Mock<IProcessWrapper> _mockProcessWrapper;

    public UpgradeCommandTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<WinGetComClient>();
        _mockGistSyncService = new MockGistSyncService();
        _mockProcessWrapper = new Mock<IProcessWrapper>();
    }

    // Phase 1: RED - 失敗するテストから開始
    [Fact]
    public async Task UpgradePackageAsync_ShouldReturnSuccess_WhenValidPackageIdProvided()
    {
        // Arrange
        var client = new WinGetComClient(_mockGistSyncService, _logger, _mockProcessWrapper.Object);
        var args = new[] { "upgrade", "--id", "TestPackage.Test" };

        // Act
        var result = await client.UpgradePackageAsync(args);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public async Task UpgradePackageAsync_ShouldReturnSuccess_WhenAllOption()
    {
        // Arrange
        var client = new WinGetComClient(_mockGistSyncService, _logger, _mockProcessWrapper.Object);
        var args = new[] { "upgrade", "--all" };

        // Act
        var result = await client.UpgradePackageAsync(args);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public async Task UpgradePackageAsync_ShouldWorkWithoutInitialization_WhenValidArgs()
    {
        // Arrange
        var client = new WinGetComClient(_mockGistSyncService, _logger, _mockProcessWrapper.Object);
        var args = new[] { "upgrade", "--id", "TestPackage.Test" };

        // Act
        var result = await client.UpgradePackageAsync(args);

        // Assert  
        result.ShouldBe(0); // 初期化なしでも有効な引数なら成功
    }

    [Fact]
    public async Task UpgradePackageAsync_ShouldReturnError_WhenMissingPackageId()
    {
        // Arrange
        var client = new WinGetComClient(_mockGistSyncService, _logger, _mockProcessWrapper.Object);
        var args = new[] { "upgrade" }; // --id が指定されていない

        // Act
        var result = await client.UpgradePackageAsync(args);

        // Assert
        result.ShouldBe(1); // パッケージIDが指定されていない場合はエラー
    }

    // 今後のフェーズ用テスト（まだ実装されていない機能）
    [Fact(Skip = "将来の機能: 実際のCOM API統合が必要")]
    public async Task UpgradePackageAsync_ShouldCallCOMAPI_WhenInitialized()
    {
        // 将来のCOM API統合テスト
        await Task.CompletedTask;
    }

    [Fact(Skip = "将来の機能: Gistとの統合が必要")]
    public async Task UpgradePackageAsync_ShouldUpdateGist_WhenSuccessful()
    {
        // 将来のGist同期テスト
        await Task.CompletedTask;
    }
}