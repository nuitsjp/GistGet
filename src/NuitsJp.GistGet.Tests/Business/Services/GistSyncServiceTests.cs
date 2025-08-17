using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Infrastructure.Os;
using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Models;
using Shouldly;

namespace NuitsJp.GistGet.Tests.Business.Services;

/// <summary>
/// GistSyncServiceのBusiness層テスト（t-wada式TDD対応）
/// 同期ワークフロー・ビジネスルールの検証に特化
/// Infrastructure層は完全にモック化
/// </summary>
public class GistSyncServiceTests
{
    private readonly GistSyncService _gistSyncService;
    private readonly Mock<IGistManager> _mockGistManager;
    private readonly Mock<ILogger<GistSyncService>> _mockLogger;
    private readonly Mock<IOsService> _mockOsService;
    private readonly Mock<IWinGetClient> _mockWinGetClient;

    public GistSyncServiceTests()
    {
        // Business層テスト専用: Infrastructure層を完全モック化し、同期ワークフローのみをテスト
        _mockGistManager = new Mock<IGistManager>();
        _mockWinGetClient = new Mock<IWinGetClient>();
        _mockOsService = new Mock<IOsService>();
        _mockLogger = new Mock<ILogger<GistSyncService>>();
        _gistSyncService = new GistSyncService(
            _mockGistManager.Object,
            _mockWinGetClient.Object,
            _mockOsService.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitializeCorrectly()
    {
        // Act & Assert - Business層: 依存注入の正常初期化テスト
        Should.NotThrow(() => new GistSyncService(
            _mockGistManager.Object,
            _mockWinGetClient.Object,
            _mockOsService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullGistManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert - Business層: null依存の適切な例外処理テスト
        Should.Throw<ArgumentNullException>(() => new GistSyncService(
            null!,
            _mockWinGetClient.Object,
            _mockOsService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullWinGetClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert - Business層: null依存の適切な例外処理テスト
        Should.Throw<ArgumentNullException>(() => new GistSyncService(
            _mockGistManager.Object,
            null!,
            _mockOsService.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullOsService_ShouldThrowArgumentNullException()
    {
        // Act & Assert - Business層: null依存の適切な例外処理テスト
        Should.Throw<ArgumentNullException>(() => new GistSyncService(
            _mockGistManager.Object,
            _mockWinGetClient.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert - Business層: null依存の適切な例外処理テスト
        Should.Throw<ArgumentNullException>(() => new GistSyncService(
            _mockGistManager.Object,
            _mockWinGetClient.Object,
            _mockOsService.Object,
            null!));
    }

    #endregion

    #region SyncAsync Tests

    [Fact]
    public async Task SyncAsync_WhenGistNotConfigured_ShouldReturnErrorResult()
    {
        // Arrange - Business層: Gist未設定時のワークフロー検証
        _mockGistManager.Setup(x => x.IsConfiguredAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _gistSyncService.SyncAsync();

        // Assert - Business層: エラー結果とビジネスルール検証
        result.ShouldNotBeNull();
        result.ExitCode.ShouldBe(1);
        result.IsSuccess.ShouldBeFalse();
        result.InstalledPackages.ShouldBeEmpty();
        result.UninstalledPackages.ShouldBeEmpty();
        result.FailedPackages.ShouldBeEmpty();
        result.RebootRequired.ShouldBeFalse();

        // Infrastructure層への正しい呼び出し検証
        _mockGistManager.Verify(x => x.IsConfiguredAsync(), Times.Once);
        _mockGistManager.Verify(x => x.GetGistPackagesAsync(), Times.Never);
        _mockWinGetClient.Verify(x => x.InitializeAsync(), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WhenNoChangesRequired_ShouldReturnSuccessWithoutActions()
    {
        // Arrange - Business層: 冪等性（同期不要時）のワークフロー検証
        var gistPackages = new PackageCollection
        {
            new PackageDefinition("Git.Git"),
            new PackageDefinition("Microsoft.VisualStudioCode")
        };
        var installedPackages = new List<PackageDefinition>
        {
            new("Git.Git"),
            new("Microsoft.VisualStudioCode")
        };

        _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);
        _mockGistManager.Setup(x => x.GetGistPackagesAsync()).ReturnsAsync(gistPackages);
        _mockWinGetClient.Setup(x => x.GetInstalledPackagesAsync()).ReturnsAsync(installedPackages);

        // Act
        var result = await _gistSyncService.SyncAsync();

        // Assert - Business層: 冪等性確保と成功結果検証
        result.ShouldNotBeNull();
        result.ExitCode.ShouldBe(0);
        result.IsSuccess.ShouldBeTrue();
        result.InstalledPackages.ShouldBeEmpty();
        result.UninstalledPackages.ShouldBeEmpty();
        result.FailedPackages.ShouldBeEmpty();
        result.RebootRequired.ShouldBeFalse();
        result.HasChanges.ShouldBeFalse();

        // Infrastructure層への正しい呼び出し検証
        _mockWinGetClient.Verify(x => x.InitializeAsync(), Times.Once);
        _mockWinGetClient.Verify(x => x.InstallPackageAsync(It.IsAny<string[]>()), Times.Never);
        _mockWinGetClient.Verify(x => x.UninstallPackageAsync(It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WithInstallRequired_ShouldExecuteInstallAndReturnSuccess()
    {
        // Arrange - Business層: インストール必要時のワークフロー検証
        var gistPackages = new PackageCollection
        {
            new PackageDefinition("Git.Git"),
            new PackageDefinition("Microsoft.VisualStudioCode")
        };
        var installedPackages = new List<PackageDefinition>
        {
            new("Git.Git")
            // VisualStudioCodeは未インストール
        };

        _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);
        _mockGistManager.Setup(x => x.GetGistPackagesAsync()).ReturnsAsync(gistPackages);
        _mockWinGetClient.Setup(x => x.GetInstalledPackagesAsync()).ReturnsAsync(installedPackages);
        _mockWinGetClient.Setup(x => x.InstallPackageAsync(It.IsAny<string[]>())).ReturnsAsync(0);

        // Act
        var result = await _gistSyncService.SyncAsync();

        // Assert - Business層: インストール実行とビジネスルール検証
        result.ShouldNotBeNull();
        result.ExitCode.ShouldBe(0);
        result.IsSuccess.ShouldBeTrue();
        result.InstalledPackages.ShouldContain("Microsoft.VisualStudioCode");
        result.InstalledPackages.Count.ShouldBe(1);
        result.UninstalledPackages.ShouldBeEmpty();
        result.FailedPackages.ShouldBeEmpty();
        result.HasChanges.ShouldBeTrue();

        // Infrastructure層への正しい呼び出し検証
        _mockWinGetClient.Verify(x => x.InstallPackageAsync(
            It.Is<string[]>(args => args.Contains("Microsoft.VisualStudioCode"))), Times.Once);
        _mockWinGetClient.Verify(x => x.UninstallPackageAsync(It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WithUninstallRequired_ShouldExecuteUninstallAndReturnSuccess()
    {
        // Arrange - Business層: アンインストール必要時のワークフロー検証
        var gistPackages = new PackageCollection
        {
            new PackageDefinition("Git.Git"),
            new PackageDefinition("Microsoft.VisualStudioCode", uninstall: true)
        };
        var installedPackages = new List<PackageDefinition>
        {
            new("Git.Git"),
            new("Microsoft.VisualStudioCode")
        };

        _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);
        _mockGistManager.Setup(x => x.GetGistPackagesAsync()).ReturnsAsync(gistPackages);
        _mockWinGetClient.Setup(x => x.GetInstalledPackagesAsync()).ReturnsAsync(installedPackages);
        _mockWinGetClient.Setup(x => x.UninstallPackageAsync(It.IsAny<string[]>())).ReturnsAsync(0);

        // Act
        var result = await _gistSyncService.SyncAsync();

        // Assert - Business層: アンインストール実行とビジネスルール検証
        result.ShouldNotBeNull();
        result.ExitCode.ShouldBe(0);
        result.IsSuccess.ShouldBeTrue();
        result.InstalledPackages.ShouldBeEmpty();
        result.UninstalledPackages.ShouldContain("Microsoft.VisualStudioCode");
        result.UninstalledPackages.Count.ShouldBe(1);
        result.FailedPackages.ShouldBeEmpty();
        result.HasChanges.ShouldBeTrue();

        // Infrastructure層への正しい呼び出し検証
        _mockWinGetClient.Verify(x => x.UninstallPackageAsync(
            It.Is<string[]>(args => args.Contains("Microsoft.VisualStudioCode"))), Times.Once);
        _mockWinGetClient.Verify(x => x.InstallPackageAsync(It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WithInstallFailure_ShouldReturnErrorWithFailedPackages()
    {
        // Arrange - Business層: インストール失敗時のエラーハンドリング検証
        var gistPackages = new PackageCollection
        {
            new PackageDefinition("Microsoft.VisualStudioCode")
        };
        var installedPackages = new List<PackageDefinition>();

        _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);
        _mockGistManager.Setup(x => x.GetGistPackagesAsync()).ReturnsAsync(gistPackages);
        _mockWinGetClient.Setup(x => x.GetInstalledPackagesAsync()).ReturnsAsync(installedPackages);
        _mockWinGetClient.Setup(x => x.InstallPackageAsync(It.IsAny<string[]>())).ReturnsAsync(1); // 失敗

        // Act
        var result = await _gistSyncService.SyncAsync();

        // Assert - Business層: 失敗時のエラー処理とビジネスルール検証
        result.ShouldNotBeNull();
        result.ExitCode.ShouldBe(1);
        result.IsSuccess.ShouldBeFalse();
        result.InstalledPackages.ShouldBeEmpty();
        result.UninstalledPackages.ShouldBeEmpty();
        result.FailedPackages.ShouldContain("Microsoft.VisualStudioCode");
        result.FailedPackages.Count.ShouldBe(1);
        result.HasChanges.ShouldBeFalse();
    }

    [Fact]
    public async Task SyncAsync_WithRebootRequiredPackage_ShouldSetRebootFlag()
    {
        // Arrange - Business層: 再起動要求パッケージのビジネスルール検証
        var gistPackages = new PackageCollection
        {
            new PackageDefinition("Microsoft.VisualStudio.2022.Community")
        };
        var installedPackages = new List<PackageDefinition>();

        _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);
        _mockGistManager.Setup(x => x.GetGistPackagesAsync()).ReturnsAsync(gistPackages);
        _mockWinGetClient.Setup(x => x.GetInstalledPackagesAsync()).ReturnsAsync(installedPackages);
        _mockWinGetClient.Setup(x => x.InstallPackageAsync(It.IsAny<string[]>())).ReturnsAsync(0);

        // Act
        var result = await _gistSyncService.SyncAsync();

        // Assert - Business層: 再起動フラグ設定のビジネスルール検証
        result.ShouldNotBeNull();
        result.ExitCode.ShouldBe(0);
        result.IsSuccess.ShouldBeTrue();
        result.InstalledPackages.ShouldContain("Microsoft.VisualStudio.2022.Community");
        result.RebootRequired.ShouldBeTrue();
        result.HasChanges.ShouldBeTrue();
    }

    [Fact]
    public async Task SyncAsync_WithException_ShouldReturnErrorResult()
    {
        // Arrange - Business層: 例外発生時のエラーハンドリング検証
        _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);
        _mockGistManager.Setup(x => x.GetGistPackagesAsync())
            .ThrowsAsync(new InvalidOperationException("Gist access failed"));

        // Act
        var result = await _gistSyncService.SyncAsync();

        // Assert - Business層: 例外時のエラー処理とビジネスルール検証
        result.ShouldNotBeNull();
        result.ExitCode.ShouldBe(1);
        result.IsSuccess.ShouldBeFalse();
        result.InstalledPackages.ShouldBeEmpty();
        result.UninstalledPackages.ShouldBeEmpty();
        result.FailedPackages.ShouldBeEmpty();
        result.RebootRequired.ShouldBeFalse();
        result.HasChanges.ShouldBeFalse();
    }

    #endregion

    #region AfterInstall/AfterUninstall Tests

    [Fact]
    public async Task AfterInstallAsync_WithPackageId_ShouldUpdateGist()
    {
        // Arrange
        var packageId = "Git.Git";
        var existingPackages = new PackageCollection();
        _mockGistManager.Setup(x => x.GetGistPackagesAsync()).ReturnsAsync(existingPackages);

        // Act
        await _gistSyncService.AfterInstallAsync(packageId);

        // Assert
        _mockGistManager.Verify(x => x.GetGistPackagesAsync(), Times.Once);
        _mockGistManager.Verify(x => x.UpdateGistPackagesAsync(It.IsAny<PackageCollection>()), Times.Once);
    }

    [Fact]
    public async Task AfterUninstallAsync_WithPackageId_ShouldUpdateGist()
    {
        // Arrange
        var packageId = "Git.Git";
        var existingPackages = new PackageCollection();
        existingPackages.Add(new PackageDefinition(packageId));
        _mockGistManager.Setup(x => x.GetGistPackagesAsync()).ReturnsAsync(existingPackages);

        // Act
        await _gistSyncService.AfterUninstallAsync(packageId);

        // Assert
        _mockGistManager.Verify(x => x.GetGistPackagesAsync(), Times.Once);
        _mockGistManager.Verify(x => x.UpdateGistPackagesAsync(It.IsAny<PackageCollection>()), Times.Once);
    }

    #endregion

    #region ExecuteRebootAsync Tests

    [Fact]
    public async Task ExecuteRebootAsync_ShouldDelegateToOsService()
    {
        // Arrange - Business層: IOsServiceへの委譲を検証
        _mockOsService.Setup(x => x.ExecuteRebootAsync()).Returns(Task.CompletedTask);

        // Act
        await _gistSyncService.ExecuteRebootAsync();

        // Assert - IOsServiceの正しい呼び出し検証（実際のshutdownコマンドは実行されない）
        _mockOsService.Verify(x => x.ExecuteRebootAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteRebootAsync_WhenOsServiceThrowsException_ShouldPropagateException()
    {
        // Arrange - Business層: IOsService例外時の適切な伝播検証
        var exception = new InvalidOperationException("Reboot failed");
        _mockOsService.Setup(x => x.ExecuteRebootAsync()).ThrowsAsync(exception);

        // Act & Assert - 例外の適切な伝播検証
        var thrownException =
            await Should.ThrowAsync<InvalidOperationException>(async () => await _gistSyncService.ExecuteRebootAsync());
        thrownException.ShouldBe(exception);
        _mockOsService.Verify(x => x.ExecuteRebootAsync(), Times.Once);
    }

    #endregion
}