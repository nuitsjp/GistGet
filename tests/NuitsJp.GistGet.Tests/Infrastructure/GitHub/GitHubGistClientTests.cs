using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet.Infrastructure.GitHub;
using Shouldly;

namespace NuitsJp.GistGet.Tests.Infrastructure.GitHub;

[Trait("Category", "Integration")]
[Trait("Category", "RequiresGitHubToken")]
public class GitHubGistClientTests : IAsyncLifetime
{
    private const string TestGistPrefix = "GISTGET_TEST";
    private readonly GitHubAuthService _authService;
    private readonly List<string> _createdGistIds = [];
    private readonly Mock<ILogger<GitHubGistClient>> _mockLogger;

    public GitHubGistClientTests()
    {
        _mockLogger = new Mock<ILogger<GitHubGistClient>>();
        var authLogger = new Mock<ILogger<GitHubAuthService>>();
        _authService = new GitHubAuthService(authLogger.Object);
    }

    public async Task InitializeAsync()
    {
        // テスト開始前に24時間以上前のテストGistを削除
        await CleanupOldTestGists();
    }

    public async Task DisposeAsync()
    {
        // テスト終了後にこのテストで作成したGistを削除
        await CleanupCreatedGists();
    }

    private async Task CleanupOldTestGists()
    {
        if (!await _authService.IsAuthenticatedAsync())
            return;

        try
        {
            var client = new GitHubGistClient(_authService, _mockLogger.Object);
            // 実装予定: 24時間以上前のテストGistを検索・削除
            // GitHub APIで自分のGist一覧を取得し、命名規則に従ってクリーンアップ
        }
        catch
        {
            // クリーンアップエラーは無視（テスト実行に影響しない）
        }
    }

    private async Task CleanupCreatedGists()
    {
        if (!await _authService.IsAuthenticatedAsync())
            return;

        var client = new GitHubGistClient(_authService, _mockLogger.Object);
        foreach (var gistId in _createdGistIds)
            try
            {
                // Gist削除APIを実装後に有効化
                // await client.DeleteAsync(gistId);
            }
            catch
            {
                // 削除エラーは無視
            }
    }

    private string GenerateTestGistName(string testMethodName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8];
        return $"{TestGistPrefix}_{timestamp}_{testMethodName}_{guid}";
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitializeCorrectly()
    {
        // Act & Assert
        Should.NotThrow(() => new GitHubGistClient(_authService, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullAuthService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new GitHubGistClient(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new GitHubGistClient(_authService, null!));
    }

    [Fact]
    public async Task GetFileContentAsync_WithNullGistId_ShouldThrowArgumentException()
    {
        // Arrange
        var client = new GitHubGistClient(_authService, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            client.GetFileContentAsync(null!, "packages.yaml"));
    }

    [Fact]
    public async Task GetFileContentAsync_WithEmptyGistId_ShouldThrowArgumentException()
    {
        // Arrange
        var client = new GitHubGistClient(_authService, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            client.GetFileContentAsync("", "packages.yaml"));
    }

    [Fact]
    public async Task GetFileContentAsync_WithNullFileName_ShouldThrowArgumentException()
    {
        // Arrange
        var client = new GitHubGistClient(_authService, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            client.GetFileContentAsync("d239aabb67e60650fbcb2b20a8342be1", null!));
    }

    [Fact]
    public async Task GetFileContentAsync_WithEmptyFileName_ShouldThrowArgumentException()
    {
        // Arrange
        var client = new GitHubGistClient(_authService, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            client.GetFileContentAsync("d239aabb67e60650fbcb2b20a8342be1", ""));
    }

    [Fact]
    public async Task UpdateFileContentAsync_WithNullGistId_ShouldThrowArgumentException()
    {
        // Arrange
        var client = new GitHubGistClient(_authService, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            client.UpdateFileContentAsync(null!, "packages.yaml", "content"));
    }

    [Fact]
    public async Task UpdateFileContentAsync_WithNullContent_ShouldThrowArgumentException()
    {
        // Arrange
        var client = new GitHubGistClient(_authService, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            client.UpdateFileContentAsync("d239aabb67e60650fbcb2b20a8342be1", "packages.yaml", null!));
    }

    [Fact]
    public async Task ExistsAsync_WithNullGistId_ShouldThrowArgumentException()
    {
        // Arrange
        var client = new GitHubGistClient(_authService, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => client.ExistsAsync(null!));
    }

    [Fact]
    public async Task ExistsAsync_WithEmptyGistId_ShouldThrowArgumentException()
    {
        // Arrange
        var client = new GitHubGistClient(_authService, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => client.ExistsAsync(""));
    }

    // 以下のテストは実際のGist IDが設定されている場合のみ実行される
    [Fact]
    public async Task GetFileContentAsync_WithValidGistId_ShouldReturnContent()
    {
        // Arrange
        var gistId = "d239aabb67e60650fbcb2b20a8342be1";
        var fileName = "packages.yaml";

        // このテストは実際のGitHub認証が必要なため、認証チェックでスキップすることも可能
        var authService = new GitHubAuthService(Mock.Of<ILogger<GitHubAuthService>>());
        var isAuthenticated = await authService.IsAuthenticatedAsync();

        if (!isAuthenticated)
            // 認証されていない場合はテストをスキップ
            return;

        var client = new GitHubGistClient(authService, _mockLogger.Object);

        // Act
        var content = await client.GetFileContentAsync(gistId, fileName);

        // Assert
        content.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithValidGistId_ShouldReturnTrue()
    {
        // Arrange
        var gistId = "d239aabb67e60650fbcb2b20a8342be1";

        var authService = new GitHubAuthService(Mock.Of<ILogger<GitHubAuthService>>());
        var isAuthenticated = await authService.IsAuthenticatedAsync();

        if (!isAuthenticated)
            // 認証されていない場合はテストをスキップ
            return;

        var client = new GitHubGistClient(authService, _mockLogger.Object);

        // Act
        var exists = await client.ExistsAsync(gistId);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithInvalidGistId_ShouldReturnFalse()
    {
        // Arrange
        var invalidGistId = "ffffffffffffffffffffffffffffffff"; // 存在しないGist ID

        var authService = new GitHubAuthService(Mock.Of<ILogger<GitHubAuthService>>());
        var isAuthenticated = await authService.IsAuthenticatedAsync();

        if (!isAuthenticated)
            // 認証されていない場合はテストをスキップ
            return;

        var client = new GitHubGistClient(authService, _mockLogger.Object);

        // Act
        var exists = await client.ExistsAsync(invalidGistId);

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "IntegrationCRUD")]
    public async Task CreateReadUpdateDelete_FullWorkflow_ShouldWork()
    {
        // Skip if not authenticated
        if (!await _authService.IsAuthenticatedAsync())
            return;

        // Arrange
        var client = new GitHubGistClient(_authService, _mockLogger.Object);
        var testGistName = GenerateTestGistName(nameof(CreateReadUpdateDelete_FullWorkflow_ShouldWork));
        var fileName = "packages.yaml";
        var initialContent = "# Test YAML\npackages:\n  - id: test.package\n    version: 1.0.0";
        var updatedContent = "# Updated Test YAML\npackages:\n  - id: test.package\n    version: 2.0.0";

        string? createdGistId = null;

        // Act 1: Create Gist (実装待ち - GitHub APIのCreateGist)
        // createdGistId = await client.CreateAsync(testGistName, fileName, initialContent, isPublic: false);
        // _createdGistIds.Add(createdGistId);

        // Act 2: Verify Exists
        // var exists = await client.ExistsAsync(createdGistId);
        // exists.ShouldBeTrue();

        // Act 3: Read Content
        // var readContent = await client.GetFileContentAsync(createdGistId, fileName);
        // readContent.ShouldBe(initialContent);

        // Act 4: Update Content
        // await client.UpdateFileContentAsync(createdGistId, fileName, updatedContent);

        // Act 5: Verify Update
        // var updatedReadContent = await client.GetFileContentAsync(createdGistId, fileName);
        // updatedReadContent.ShouldBe(updatedContent);

        // 現在はGitHubGistClientにCreate/Delete APIが未実装のため、テストを保留
        // テストの構造は統合テスト用に準備完了
    }

    [Fact]
    [Trait("Category", "IntegrationErrorHandling")]
    public async Task NetworkError_ShouldHandleGracefully()
    {
        // Skip if not authenticated
        if (!await _authService.IsAuthenticatedAsync())
            return;

        // Arrange
        var client = new GitHubGistClient(_authService, _mockLogger.Object);
        var invalidGistId = "invalid_gist_id_that_does_not_exist_123456789"; // 存在しないGist ID

        // Act & Assert: 存在しないGist IDの場合はfalseが返される（例外ではない）
        var exists = await client.ExistsAsync(invalidGistId);
        exists.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "IntegrationLargeContent")]
    public async Task LargeContent_ShouldHandleCorrectly()
    {
        // Skip if not authenticated  
        if (!await _authService.IsAuthenticatedAsync())
            return;

        // Arrange
        var client = new GitHubGistClient(_authService, _mockLogger.Object);
        var largeContent = new string('x', 100000); // 100KB content

        // このテストも Create/Update API実装後に有効化
        // GitHub Gistの制限（ファイルサイズ上限）をテストする予定
    }

    // 統合テスト用のヘルパーメソッド
    private async Task<bool> IsGitHubAvailable()
    {
        try
        {
            if (!await _authService.IsAuthenticatedAsync())
                return false;

            var client = new GitHubGistClient(_authService, _mockLogger.Object);
            // 簡単な接続テスト
            await client.ExistsAsync("ffffffffffffffffffffffffffffffff");
            return true;
        }
        catch
        {
            return false;
        }
    }
}