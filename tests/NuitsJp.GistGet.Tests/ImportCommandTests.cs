using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Tests.Mocks;
using Shouldly;
using Xunit;

namespace NuitsJp.GistGet.Tests;

/// <summary>
/// t-wada式TDD用ImportCommandテスト
/// Red-Green-Refactorサイクルで進める
/// </summary>
public class ImportCommandTests
{
    private readonly ILogger<MockGistSyncService> _logger;
    private readonly MockGistSyncService _mockGistSyncService;

    public ImportCommandTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<MockGistSyncService>();
        _mockGistSyncService = new MockGistSyncService();
    }

    // Phase 1: RED - 失敗するテストから開始
    [Fact]
    public async Task ImportAsync_ShouldReturnSuccess_WhenExecuted()
    {
        // Arrange
        var mockGistSync = new MockGistSyncService();

        // Act
        var result = await mockGistSync.ImportAsync();

        // Assert
        result.ShouldBe(0);
        mockGistSync.LastCommand.ShouldBe("import");
    }

    [Fact]
    public async Task ImportAsync_ShouldProcessImportFile_WhenExecuted()
    {
        // このテストは失敗することを期待（実装されていない機能）
        // Arrange
        var mockGistSync = new MockGistSyncService();

        // Act
        var result = await mockGistSync.ImportAsync();

        // Assert
        result.ShouldBe(0);

        // 新機能：インポートファイル処理をテスト（まだ実装されていない）
        mockGistSync.ImportFileProcessed.ShouldBeTrue(); // これは失敗するはず
    }

    [Fact]
    public async Task ImportAsync_ShouldHandleImportPath_WhenSpecified()
    {
        // Arrange
        var mockGistSync = new MockGistSyncService();

        // Act  
        var result = await mockGistSync.ImportAsync();

        // Assert
        result.ShouldBe(0);
        // 将来の実装でインポートパスの処理をテスト
    }

    // 今後のフェーズ用テスト（まだ実装されていない機能）
    [Fact(Skip = "将来の機能: 実際のファイルシステム操作が必要")]
    public async Task ImportAsync_ShouldReadYamlFile_WhenImporting()
    {
        // 将来のファイル読み込みテスト
        await Task.CompletedTask;
    }

    [Fact(Skip = "将来の機能: COM APIとの統合が必要")]
    public async Task ImportAsync_ShouldInstallPackagesFromFile_WhenExecuted()
    {
        // 将来のCOM API統合テスト
        await Task.CompletedTask;
    }
}