using Shouldly;
using Xunit;
using NuitsJp.GistGet.Infrastructure.GitHub;
using Moq;
using Microsoft.Extensions.Logging;

namespace NuitsJp.GistGet.Tests.Infrastructure.GitHub;

[Trait("Category", "Local")]
public class GitHubGistClientTests
{
    private readonly Mock<ILogger<GitHubGistClient>> _mockLogger;
    private readonly GitHubAuthService _authService;

    public GitHubGistClientTests()
    {
        _mockLogger = new Mock<ILogger<GitHubGistClient>>();
        var authLogger = new Mock<ILogger<GitHubAuthService>>();
        _authService = new GitHubAuthService(authLogger.Object);
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
        {
            // 認証されていない場合はテストをスキップ
            return;
        }

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
        {
            // 認証されていない場合はテストをスキップ
            return;
        }

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
        {
            // 認証されていない場合はテストをスキップ
            return;
        }

        var client = new GitHubGistClient(authService, _mockLogger.Object);

        // Act
        var exists = await client.ExistsAsync(invalidGistId);

        // Assert
        exists.ShouldBeFalse();
    }
}