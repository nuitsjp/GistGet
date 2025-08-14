using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Tests.Mocks;
using Shouldly;
using Xunit;

namespace NuitsJp.GistGet.Tests.Presentation.Commands;

/// <summary>
/// t-wada式TDD用SyncCommandテスト
/// Red-Green-Refactorサイクルで進める
/// </summary>
public class SyncCommandTests
{
    private readonly ILogger<MockGistSyncService> _logger;
    private readonly MockGistSyncService _mockGistSyncService;

    public SyncCommandTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<MockGistSyncService>();
        _mockGistSyncService = new MockGistSyncService();
    }

    // Phase 1: RED - 失敗するテストから開始
    [Fact]
    public async Task SyncAsync_ShouldReturnSuccess_WhenExecuted()
    {
        // Arrange
        var mockGistSync = new MockGistSyncService();

        // Act
        var result = await mockGistSync.SyncAsync();

        // Assert
        result.ShouldBe(0);
        mockGistSync.LastCommand.ShouldBe("sync");
    }

    [Fact]
    public async Task SyncAsync_ShouldPersistSyncState_WhenExecuted()
    {
        // このテストは失敗することを期待（実装されていない機能）
        // Arrange
        var mockGistSync = new MockGistSyncService();

        // Act  
        var result = await mockGistSync.SyncAsync();

        // Assert
        result.ShouldBe(0);

        // 新機能：同期状態の永続化をテスト（まだ実装されていない）
        mockGistSync.SyncStatePersisted.ShouldBeTrue(); // これは失敗するはず
    }

    // 今後のフェーズ用テスト（まだ実装されていない機能）
    [Fact(Skip = "将来の機能: 実際のGist API統合が必要")]
    public async Task SyncAsync_ShouldCallGistAPI_WhenSyncing()
    {
        // 将来のGist API統合テスト
        await Task.CompletedTask;
    }

    [Fact(Skip = "将来の機能: COM APIとの統合が必要")]
    public async Task SyncAsync_ShouldSyncWithInstalledPackages_WhenExecuted()
    {
        // 将来のCOM API統合テスト
        await Task.CompletedTask;
    }
}