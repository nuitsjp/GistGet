using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Tests.Mocks;
using Shouldly;
using Xunit;

namespace NuitsJp.GistGet.Tests;

/// <summary>
/// t-wada式TDD用ExportCommandテスト  
/// Red-Green-Refactorサイクルで進める
/// </summary>
public class ExportCommandTests
{
    private readonly ILogger<MockGistSyncService> _logger;
    private readonly MockGistSyncService _mockGistSyncService;

    public ExportCommandTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<MockGistSyncService>();
        _mockGistSyncService = new MockGistSyncService();
    }

    // Phase 1: RED - 失敗するテストから開始
    [Fact]
    public async Task ExportAsync_ShouldReturnSuccess_WhenExecuted()
    {
        // Arrange
        var mockGistSync = new MockGistSyncService();

        // Act
        var result = await mockGistSync.ExportAsync();

        // Assert
        result.ShouldBe(0);
        mockGistSync.LastCommand.ShouldBe("export");
    }

    [Fact]
    public async Task ExportAsync_ShouldGenerateExportFile_WhenExecuted()
    {
        // このテストは失敗することを期待（実装されていない機能）
        // Arrange
        var mockGistSync = new MockGistSyncService();

        // Act
        var result = await mockGistSync.ExportAsync();

        // Assert
        result.ShouldBe(0);
        
        // 新機能：エクスポートファイル生成をテスト（まだ実装されていない）
        mockGistSync.ExportFileGenerated.ShouldBeTrue(); // これは失敗するはず
    }

    [Fact]
    public async Task ExportAsync_ShouldHandleExportPath_WhenSpecified()
    {
        // Arrange
        var mockGistSync = new MockGistSyncService();

        // Act  
        var result = await mockGistSync.ExportAsync();

        // Assert
        result.ShouldBe(0);
        // 将来の実装でエクスポートパスの処理をテスト
    }

    // 今後のフェーズ用テスト（まだ実装されていない機能）
    [Fact(Skip = "将来の機能: 実際のファイルシステム操作が必要")]
    public async Task ExportAsync_ShouldCreateYamlFile_WhenExporting()
    {
        // 将来のファイル生成テスト
        await Task.CompletedTask;
    }

    [Fact(Skip = "将来の機能: COM APIとの統合が必要")]
    public async Task ExportAsync_ShouldExportInstalledPackages_WhenExecuted()
    {
        // 将来のCOM API統合テスト
        await Task.CompletedTask;
    }
}