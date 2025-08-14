using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Infrastructure.GitHub;
using Xunit;

namespace NuitsJp.GistGet.Tests.Infrastructure.GitHub;

/// <summary>
/// GitHubAuthServiceのテスト（t-wada式TDD対応）
/// DPAPIによるトークン暗号化機能のテスト
/// </summary>
public class GitHubAuthServiceTests : IDisposable
{
    private readonly string _testTokenDirectory;
    private readonly ILogger<GitHubAuthService> _logger;

    public GitHubAuthServiceTests()
    {
        // テスト用の一時ディレクトリを作成
        _testTokenDirectory = Path.Combine(Path.GetTempPath(), "GistGetTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testTokenDirectory);

        // テスト用のロガーを作成
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<GitHubAuthService>();
    }

    public void Dispose()
    {
        // テスト後のクリーンアップ
        if (Directory.Exists(_testTokenDirectory))
        {
            Directory.Delete(_testTokenDirectory, true);
        }
    }

    [Fact]
    public async Task SaveTokenAsync_ShouldEncryptTokenWithDPAPI_WhenCalledWithValidToken()
    {
        // Arrange
        const string testToken = "test_github_token_12345";
        var service = CreateTestService();

        // Act
        await service.SaveTokenAsync(testToken);

        // Assert
        var tokenFilePath = Path.Combine(_testTokenDirectory, "token.json");
        Assert.True(File.Exists(tokenFilePath), "トークンファイルが作成されていません");

        var fileContent = await File.ReadAllTextAsync(tokenFilePath);

        // DPAPIで暗号化されているため、平文のトークンが含まれていないことを確認
        Assert.DoesNotContain(testToken, fileContent);

        // Base64エンコードされた暗号化データが含まれていることを確認
        Assert.Contains("EncryptedToken", fileContent);
    }

    [Fact]
    public async Task LoadTokenAsync_ShouldDecryptTokenWithDPAPI_WhenCalledWithEncryptedToken()
    {
        // Arrange
        const string originalToken = "test_github_token_67890";
        var service = CreateTestService();

        // まず暗号化して保存
        await service.SaveTokenAsync(originalToken);

        // Act
        var decryptedToken = await service.LoadTokenAsync();

        // Assert
        Assert.Equal(originalToken, decryptedToken);
    }

    [Fact]
    public async Task LoadTokenAsync_ShouldReturnNull_WhenTokenFileDoesNotExist()
    {
        // Arrange
        var service = CreateTestService();

        // Act
        var token = await service.LoadTokenAsync();

        // Assert
        Assert.Null(token);
    }


    [Fact]
    public async Task LoadTokenAsync_ShouldReturnNull_WhenDecryptionFails()
    {
        // Arrange
        var service = CreateTestService();
        var tokenFilePath = Path.Combine(_testTokenDirectory, "token.json");

        // 不正な暗号化データを作成（復号化に失敗させる）
        var invalidData = new { EncryptedToken = "invalid_base64_data", CreatedAt = DateTime.UtcNow };
        var json = System.Text.Json.JsonSerializer.Serialize(invalidData);
        await File.WriteAllTextAsync(tokenFilePath, json);

        // Act
        var token = await service.LoadTokenAsync();

        // Assert
        Assert.Null(token);
    }

    private GitHubAuthService CreateTestService()
    {
        // テスト用のGitHubAuthServiceを作成
        var service = new GitHubAuthService(_logger);

        // リフレクションを使用してテスト用ディレクトリを設定
        var tokenFilePathField = typeof(GitHubAuthService).GetField("_tokenFilePath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var testTokenFilePath = Path.Combine(_testTokenDirectory, "token.json");
        tokenFilePathField?.SetValue(service, testTokenFilePath);

        return service;
    }
}