using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Infrastructure.Storage;
using NuitsJp.GistGet.Models;
using Shouldly;

namespace NuitsJp.GistGet.Tests.Business;

/// <summary>
/// GistManagerのBusiness層テスト（t-wada式TDD対応）
/// ワークフロー・ビジネスルールの検証に特化
/// Infrastructure層は完全にモック化
/// </summary>
public class GistManagerTests
{
    private readonly GistManager _gistManager;
    private readonly Mock<IGitHubGistClient> _mockGistClient;
    private readonly Mock<ILogger<GistManager>> _mockLogger;
    private readonly Mock<IGistConfigurationStorage> _mockStorage;
    private readonly Mock<IPackageYamlConverter> _mockYamlConverter;

    public GistManagerTests()
    {
        // Business層テスト専用: Infrastructure層を完全モック化し、ワークフローのみをテスト
        _mockGistClient = new Mock<IGitHubGistClient>();
        _mockStorage = new Mock<IGistConfigurationStorage>();
        _mockYamlConverter = new Mock<IPackageYamlConverter>();
        _mockLogger = new Mock<ILogger<GistManager>>();
        _gistManager = new GistManager(
            _mockGistClient.Object,
            _mockStorage.Object,
            _mockYamlConverter.Object,
            _mockLogger.Object);
    }

    #region Infrastructure Layer Isolation Tests

    [Fact]
    public async Task GetGistPackagesAsync_ShouldOnlyCallExpectedServices_NotOtherServices()
    {
        // Arrange - Business層: Infrastructure層の完全分離検証
        var testConfig = new GistConfiguration { GistId = "test-gist", FileName = "test.yaml" };
        var testPackages = new PackageCollection();
        var yamlContent = "test content";

        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync(testConfig);
        _mockGistClient.Setup(c => c.GetFileContentAsync("test-gist", "test.yaml")).ReturnsAsync(yamlContent);
        _mockYamlConverter.Setup(y => y.FromYaml(yamlContent)).Returns(testPackages);

        // Act
        await _gistManager.GetGistPackagesAsync();

        // Assert - Business層テスト: 期待されるサービスのみが呼ばれることを確認
        _mockStorage.Verify(s => s.LoadGistConfigurationAsync(), Times.Once);
        _mockGistClient.Verify(c => c.GetFileContentAsync("test-gist", "test.yaml"), Times.Once);
        _mockYamlConverter.Verify(y => y.FromYaml(yamlContent), Times.Once);
        _mockStorage.Verify(s => s.SaveGistConfigurationAsync(It.IsAny<GistConfiguration>()), Times.Once);

        // 他の予期しない呼び出しがないことを確認
        _mockStorage.VerifyNoOtherCalls();
        _mockGistClient.VerifyNoOtherCalls();
        _mockYamlConverter.VerifyNoOtherCalls();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitializeCorrectly()
    {
        // Act & Assert - Business層: 依存注入の正常初期化テスト
        Should.NotThrow(() => new GistManager(
            _mockGistClient.Object,
            _mockStorage.Object,
            _mockYamlConverter.Object,
            _mockLogger.Object));
    }

    [Theory]
    [InlineData(false, true, true, true)]   // gistClient = null
    [InlineData(true, false, true, true)]   // storage = null
    [InlineData(true, true, false, true)]   // yamlConverter = null
    [InlineData(true, true, true, false)]   // logger = null
    public void Constructor_WithNullDependencies_ShouldThrowArgumentNullException(
        bool hasGistClient, bool hasStorage, bool hasYamlConverter, bool hasLogger)
    {
        // Arrange
        var gistClient = hasGistClient ? _mockGistClient.Object : null;
        var storage = hasStorage ? _mockStorage.Object : null;
        var yamlConverter = hasYamlConverter ? _mockYamlConverter.Object : null;
        var logger = hasLogger ? _mockLogger.Object : null;

        // Act & Assert - Business層: null依存性注入の例外処理テスト
        Should.Throw<ArgumentNullException>(() => new GistManager(gistClient!, storage!, yamlConverter!, logger!));
    }

    #endregion

    #region Business Workflow Tests - Configuration Check

    [Fact]
    public async Task IsConfiguredAsync_ShouldReturnTrue_WhenStorageReturnsTrue()
    {
        // Arrange - Business層: 設定確認ワークフローテスト
        _mockStorage.Setup(s => s.IsConfiguredAsync()).ReturnsAsync(true);

        // Act
        var isConfigured = await _gistManager.IsConfiguredAsync();

        // Assert - Business層: ストレージ層との正しい連携検証
        isConfigured.ShouldBeTrue();
        _mockStorage.Verify(s => s.IsConfiguredAsync(), Times.Once);
        _mockStorage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IsConfiguredAsync_ShouldReturnFalse_WhenStorageReturnsFalse()
    {
        // Arrange - Business層: 未設定状態のワークフローテスト
        _mockStorage.Setup(s => s.IsConfiguredAsync()).ReturnsAsync(false);

        // Act
        var isConfigured = await _gistManager.IsConfiguredAsync();

        // Assert - Business層: 未設定時の適切な値返却検証
        isConfigured.ShouldBeFalse();
        _mockStorage.Verify(s => s.IsConfiguredAsync(), Times.Once);
    }

    [Fact]
    public async Task IsConfiguredAsync_ShouldReturnFalse_WhenStorageThrowsException()
    {
        // Arrange - Business層: Infrastructure層例外時のエラーハンドリングワークフロー
        _mockStorage.Setup(s => s.IsConfiguredAsync()).ThrowsAsync(new IOException("Storage error"));

        // Act
        var isConfigured = await _gistManager.IsConfiguredAsync();

        // Assert - Business層: 例外発生時の適切なフォールバック処理検証
        isConfigured.ShouldBeFalse();
        _mockStorage.Verify(s => s.IsConfiguredAsync(), Times.Once);
    }

    #endregion

    #region Business Workflow Tests - Get Packages

    [Fact]
    public async Task GetGistPackagesAsync_ShouldReturnPackages_WhenValidConfiguration()
    {
        // Arrange - Business層: Gistパッケージ取得の正常ワークフローテスト
        var testConfig = new GistConfiguration { GistId = "test-gist", FileName = "test.yaml" };
        var testPackages = new PackageCollection();
        var yamlContent = "packages:\\n- id: test.package";

        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync(testConfig);
        _mockGistClient.Setup(c => c.GetFileContentAsync("test-gist", "test.yaml")).ReturnsAsync(yamlContent);
        _mockYamlConverter.Setup(y => y.FromYaml(yamlContent)).Returns(testPackages);

        // Act
        var result = await _gistManager.GetGistPackagesAsync();

        // Assert - Business層: 完全なワークフローの実行と結果検証
        result.ShouldBe(testPackages);
        _mockStorage.Verify(s => s.LoadGistConfigurationAsync(), Times.Once);
        _mockGistClient.Verify(c => c.GetFileContentAsync("test-gist", "test.yaml"), Times.Once);
        _mockYamlConverter.Verify(y => y.FromYaml(yamlContent), Times.Once);
        _mockStorage.Verify(s => s.SaveGistConfigurationAsync(It.IsAny<GistConfiguration>()), Times.Once);
    }

    [Fact]
    public async Task GetGistPackagesAsync_ShouldThrowInvalidOperationException_WhenNotConfigured()
    {
        // Arrange - Business層: 未設定時の例外処理ワークフロー
        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync((GistConfiguration?)null);

        // Act & Assert - Business層: 設定前提条件チェックのビジネスルール検証
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => _gistManager.GetGistPackagesAsync());
        exception.Message.ShouldContain("Gist configuration not found");

        // 設定が存在しない場合は後続処理は実行されない
        _mockGistClient.Verify(c => c.GetFileContentAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockYamlConverter.Verify(y => y.FromYaml(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetGistPackagesAsync_ShouldPropagateException_WhenGistClientFails()
    {
        // Arrange - Business層: Infrastructure層エラーの適切な伝播テスト
        var testConfig = new GistConfiguration { GistId = "test-gist", FileName = "test.yaml" };
        var gistException = new InvalidOperationException("Gist access failed");

        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync(testConfig);
        _mockGistClient.Setup(c => c.GetFileContentAsync("test-gist", "test.yaml")).ThrowsAsync(gistException);

        // Act & Assert - Business層: Infrastructure層例外の適切な処理検証
        var thrownException =
            await Should.ThrowAsync<InvalidOperationException>(() => _gistManager.GetGistPackagesAsync());
        thrownException.ShouldBe(gistException);

        // エラー発生時は後続処理は実行されない
        _mockYamlConverter.Verify(y => y.FromYaml(It.IsAny<string>()), Times.Never);
        _mockStorage.Verify(s => s.SaveGistConfigurationAsync(It.IsAny<GistConfiguration>()), Times.Never);
    }

    #endregion

    #region Business Workflow Tests - Update Packages

    [Fact]
    public async Task UpdateGistPackagesAsync_ShouldUpdateGist_WhenValidConfiguration()
    {
        // Arrange - Business層: Gistパッケージ更新の正常ワークフローテスト
        var testConfig = new GistConfiguration { GistId = "test-gist", FileName = "test.yaml" };
        var testPackages = new PackageCollection();
        var yamlContent = "packages:\\n- id: updated.package";

        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync(testConfig);
        _mockYamlConverter.Setup(y => y.ToYaml(testPackages)).Returns(yamlContent);

        // Act
        await _gistManager.UpdateGistPackagesAsync(testPackages);

        // Assert - Business層: 完全な更新ワークフローの実行検証
        _mockStorage.Verify(s => s.LoadGistConfigurationAsync(), Times.Once);
        _mockYamlConverter.Verify(y => y.ToYaml(testPackages), Times.Once);
        _mockGistClient.Verify(c => c.UpdateFileContentAsync("test-gist", "test.yaml", yamlContent), Times.Once);
        _mockStorage.Verify(s => s.SaveGistConfigurationAsync(It.IsAny<GistConfiguration>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGistPackagesAsync_ShouldThrowArgumentNullException_WhenPackagesIsNull()
    {
        // Arrange & Act & Assert - Business層: null入力に対するバリデーションルール
        await Should.ThrowAsync<ArgumentNullException>(() => _gistManager.UpdateGistPackagesAsync(null!));

        // null入力時は一切の処理が実行されない
        _mockStorage.Verify(s => s.LoadGistConfigurationAsync(), Times.Never);
        _mockYamlConverter.Verify(y => y.ToYaml(It.IsAny<PackageCollection>()), Times.Never);
        _mockGistClient.Verify(
            c => c.UpdateFileContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateGistPackagesAsync_ShouldThrowInvalidOperationException_WhenNotConfigured()
    {
        // Arrange - Business層: 未設定時の例外処理ワークフロー
        var testPackages = new PackageCollection();
        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync((GistConfiguration?)null);

        // Act & Assert - Business層: 設定前提条件チェック
        var exception =
            await Should.ThrowAsync<InvalidOperationException>(() =>
                _gistManager.UpdateGistPackagesAsync(testPackages));
        exception.Message.ShouldContain("Gist configuration not found");

        // 設定が存在しない場合は後続処理は実行されない
        _mockYamlConverter.Verify(y => y.ToYaml(It.IsAny<PackageCollection>()), Times.Never);
        _mockGistClient.Verify(
            c => c.UpdateFileContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Business Workflow Tests - Configuration Management

    [Fact]
    public async Task GetConfigurationAsync_ShouldReturnConfiguration_WhenExists()
    {
        // Arrange - Business層: 設定取得の正常ワークフローテスト
        var testConfig = new GistConfiguration { GistId = "test-gist", FileName = "test.yaml" };
        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync(testConfig);

        // Act
        var result = await _gistManager.GetConfigurationAsync();

        // Assert - Business層: 設定値の正確な返却検証
        result.ShouldBe(testConfig);
        _mockStorage.Verify(s => s.LoadGistConfigurationAsync(), Times.Once);
        _mockStorage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldThrowInvalidOperationException_WhenNotConfigured()
    {
        // Arrange - Business層: 設定未存在時の例外処理
        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync((GistConfiguration?)null);

        // Act & Assert - Business層: 設定なし時の適切な例外処理検証
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => _gistManager.GetConfigurationAsync());
        exception.Message.ShouldContain("Gist configuration not found");
    }

    #endregion

    #region Business Workflow Tests - Gist Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateGistAccessAsync_ShouldThrowArgumentException_WhenGistIdInvalid(string? invalidGistId)
    {
        // Act & Assert - Business層: 入力値検証のビジネスルール
        var exception =
            await Should.ThrowAsync<ArgumentException>(() => _gistManager.ValidateGistAccessAsync(invalidGistId!));
        exception.Message.ShouldContain("Gist ID cannot be null or empty");

        // 無効な入力の場合はGistクライアントは呼ばれない
        _mockGistClient.Verify(c => c.ExistsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ValidateGistAccessAsync_ShouldComplete_WhenGistExists()
    {
        // Arrange - Business層: Gist存在確認の正常ワークフロー
        var gistId = "valid-gist-id";
        _mockGistClient.Setup(c => c.ExistsAsync(gistId)).ReturnsAsync(true);

        // Act & Assert - Business層: 正常な検証完了
        await Should.NotThrowAsync(() => _gistManager.ValidateGistAccessAsync(gistId));
        _mockGistClient.Verify(c => c.ExistsAsync(gistId), Times.Once);
    }

    [Fact]
    public async Task ValidateGistAccessAsync_ShouldThrowInvalidOperationException_WhenGistNotExists()
    {
        // Arrange - Business層: Gist未存在時の例外処理ワークフロー
        var gistId = "non-existent-gist";
        _mockGistClient.Setup(c => c.ExistsAsync(gistId)).ReturnsAsync(false);

        // Act & Assert - Business層: 存在しないGistに対するビジネスルール検証
        var exception =
            await Should.ThrowAsync<InvalidOperationException>(() => _gistManager.ValidateGistAccessAsync(gistId));
        exception.Message.ShouldContain($"Gist {gistId} does not exist or is not accessible");
    }

    [Fact]
    public async Task ValidateGistAccessAsync_ShouldPropagateException_WhenGistClientFails()
    {
        // Arrange - Business層: Infrastructure層例外の適切な伝播
        var gistId = "test-gist";
        var clientException = new HttpRequestException("Network error");
        _mockGistClient.Setup(c => c.ExistsAsync(gistId)).ThrowsAsync(clientException);

        // Act & Assert - Business層: Infrastructure層例外の適切な処理
        var thrownException =
            await Should.ThrowAsync<HttpRequestException>(() => _gistManager.ValidateGistAccessAsync(gistId));
        thrownException.ShouldBe(clientException);
    }

    #endregion
}