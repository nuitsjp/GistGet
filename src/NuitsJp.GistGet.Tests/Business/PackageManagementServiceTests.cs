using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Infrastructure.WinGet;
using Shouldly;

namespace NuitsJp.GistGet.Tests.Business;

/// <summary>
/// PackageManagementServiceのテスト（Business層: WinGet操作とGist同期の統合）
/// </summary>
[Trait("Category", "Unit")]
public class PackageManagementServiceTests
{
    private readonly Mock<IWinGetClient> _mockWinGetClient;
    private readonly Mock<IGistSyncService> _mockGistSyncService;
    private readonly Mock<ILogger<PackageManagementService>> _mockLogger;
    private readonly PackageManagementService _packageManagementService;

    public PackageManagementServiceTests()
    {
        _mockWinGetClient = new Mock<IWinGetClient>();
        _mockGistSyncService = new Mock<IGistSyncService>();
        _mockLogger = new Mock<ILogger<PackageManagementService>>();

        _packageManagementService = new PackageManagementService(
            _mockWinGetClient.Object,
            _mockGistSyncService.Object,
            _mockLogger.Object);
    }

    #region InstallPackageAsync Tests

    [Fact]
    public async Task InstallPackageAsync_WithValidPackageId_ShouldInstallAndUpdateGist()
    {
        // Arrange
        var args = new[] { "install", "Git.Git" };
        _mockWinGetClient.Setup(x => x.InstallPackageAsync(args)).ReturnsAsync(0);
        _mockGistSyncService.Setup(x => x.AfterInstallAsync("Git.Git")).Returns(Task.CompletedTask);

        // Act
        var result = await _packageManagementService.InstallPackageAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockWinGetClient.Verify(x => x.InstallPackageAsync(args), Times.Once);
        _mockGistSyncService.Verify(x => x.AfterInstallAsync("Git.Git"), Times.Once);
    }

    [Fact]
    public async Task InstallPackageAsync_WithoutPackageId_ShouldReturnErrorCode()
    {
        // Arrange
        var args = new[] { "install" };

        // Act
        var result = await _packageManagementService.InstallPackageAsync(args);

        // Assert
        result.ShouldBe(1);
        _mockWinGetClient.Verify(x => x.InstallPackageAsync(It.IsAny<string[]>()), Times.Never);
        _mockGistSyncService.Verify(x => x.AfterInstallAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InstallPackageAsync_WhenWinGetFails_ShouldNotUpdateGist()
    {
        // Arrange
        var args = new[] { "install", "Git.Git" };
        _mockWinGetClient.Setup(x => x.InstallPackageAsync(args)).ReturnsAsync(1);

        // Act
        var result = await _packageManagementService.InstallPackageAsync(args);

        // Assert
        result.ShouldBe(1);
        _mockWinGetClient.Verify(x => x.InstallPackageAsync(args), Times.Once);
        _mockGistSyncService.Verify(x => x.AfterInstallAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InstallPackageAsync_WhenGistSyncFails_ShouldStillReturnSuccess()
    {
        // Arrange
        var args = new[] { "install", "Git.Git" };
        _mockWinGetClient.Setup(x => x.InstallPackageAsync(args)).ReturnsAsync(0);
        _mockGistSyncService.Setup(x => x.AfterInstallAsync("Git.Git"))
            .ThrowsAsync(new Exception("Gist sync failed"));

        // Act
        var result = await _packageManagementService.InstallPackageAsync(args);

        // Assert
        result.ShouldBe(0); // インストール成功はGist同期失敗で妨げられない
        _mockWinGetClient.Verify(x => x.InstallPackageAsync(args), Times.Once);
        _mockGistSyncService.Verify(x => x.AfterInstallAsync("Git.Git"), Times.Once);
    }

    #endregion

    #region UninstallPackageAsync Tests

    [Fact]
    public async Task UninstallPackageAsync_WithValidPackageId_ShouldUninstallAndUpdateGist()
    {
        // Arrange
        var args = new[] { "uninstall", "Git.Git" };
        _mockWinGetClient.Setup(x => x.UninstallPackageAsync(args)).ReturnsAsync(0);
        _mockGistSyncService.Setup(x => x.AfterUninstallAsync("Git.Git")).Returns(Task.CompletedTask);

        // Act
        var result = await _packageManagementService.UninstallPackageAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockWinGetClient.Verify(x => x.UninstallPackageAsync(args), Times.Once);
        _mockGistSyncService.Verify(x => x.AfterUninstallAsync("Git.Git"), Times.Once);
    }

    [Fact]
    public async Task UninstallPackageAsync_WithoutPackageId_ShouldReturnErrorCode()
    {
        // Arrange
        var args = new[] { "uninstall" };

        // Act
        var result = await _packageManagementService.UninstallPackageAsync(args);

        // Assert
        result.ShouldBe(1);
        _mockWinGetClient.Verify(x => x.UninstallPackageAsync(It.IsAny<string[]>()), Times.Never);
        _mockGistSyncService.Verify(x => x.AfterUninstallAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UninstallPackageAsync_WhenWinGetFails_ShouldNotUpdateGist()
    {
        // Arrange
        var args = new[] { "uninstall", "Git.Git" };
        _mockWinGetClient.Setup(x => x.UninstallPackageAsync(args)).ReturnsAsync(1);

        // Act
        var result = await _packageManagementService.UninstallPackageAsync(args);

        // Assert
        result.ShouldBe(1);
        _mockWinGetClient.Verify(x => x.UninstallPackageAsync(args), Times.Once);
        _mockGistSyncService.Verify(x => x.AfterUninstallAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region UpgradePackageAsync Tests

    [Fact]
    public async Task UpgradePackageAsync_WithValidPackageId_ShouldUpgradeAndUpdateGist()
    {
        // Arrange
        var args = new[] { "upgrade", "Git.Git" };
        _mockWinGetClient.Setup(x => x.UpgradePackageAsync(args)).ReturnsAsync(0);
        _mockGistSyncService.Setup(x => x.AfterInstallAsync("Git.Git")).Returns(Task.CompletedTask);

        // Act
        var result = await _packageManagementService.UpgradePackageAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockWinGetClient.Verify(x => x.UpgradePackageAsync(args), Times.Once);
        _mockGistSyncService.Verify(x => x.AfterInstallAsync("Git.Git"), Times.Once); // アップグレードはインストール扱い
    }

    [Fact]
    public async Task UpgradePackageAsync_WithoutPackageId_ShouldReturnErrorCode()
    {
        // Arrange
        var args = new[] { "upgrade" };

        // Act
        var result = await _packageManagementService.UpgradePackageAsync(args);

        // Assert
        result.ShouldBe(1);
        _mockWinGetClient.Verify(x => x.UpgradePackageAsync(It.IsAny<string[]>()), Times.Never);
        _mockGistSyncService.Verify(x => x.AfterInstallAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InstallPackageAsync_WithNoGistOption_ShouldSkipGistUpdate()
    {
        // Arrange
        var args = new[] { "install", "Git.Git", "--no-gist" };
        _mockWinGetClient.Setup(x => x.InstallPackageAsync(args)).ReturnsAsync(0);

        // Act
        var result = await _packageManagementService.InstallPackageAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockWinGetClient.Verify(x => x.InstallPackageAsync(args), Times.Once);
        _mockGistSyncService.Verify(x => x.AfterInstallAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UninstallPackageAsync_WithNoGistOption_ShouldSkipGistUpdate()
    {
        // Arrange
        var args = new[] { "uninstall", "Git.Git", "--no-gist" };
        _mockWinGetClient.Setup(x => x.UninstallPackageAsync(args)).ReturnsAsync(0);

        // Act
        var result = await _packageManagementService.UninstallPackageAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockWinGetClient.Verify(x => x.UninstallPackageAsync(args), Times.Once);
        _mockGistSyncService.Verify(x => x.AfterUninstallAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpgradePackageAsync_WithNoGistOption_ShouldSkipGistUpdate()
    {
        // Arrange
        var args = new[] { "upgrade", "Git.Git", "--no-gist" };
        _mockWinGetClient.Setup(x => x.UpgradePackageAsync(args)).ReturnsAsync(0);

        // Act
        var result = await _packageManagementService.UpgradePackageAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockWinGetClient.Verify(x => x.UpgradePackageAsync(args), Times.Once);
        _mockGistSyncService.Verify(x => x.AfterInstallAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ExtractPackageId Tests

    [Fact]
    public void ExtractPackageId_WithInstallCommand_ShouldReturnPackageId()
    {
        // Arrange
        var args = new[] { "install", "Git.Git" };

        // Act
        var result = _packageManagementService.ExtractPackageId(args);

        // Assert
        result.ShouldBe("Git.Git");
    }

    [Fact]
    public void ExtractPackageId_WithUninstallCommand_ShouldReturnPackageId()
    {
        // Arrange
        var args = new[] { "uninstall", "Microsoft.VisualStudioCode" };

        // Act
        var result = _packageManagementService.ExtractPackageId(args);

        // Assert
        result.ShouldBe("Microsoft.VisualStudioCode");
    }

    [Fact]
    public void ExtractPackageId_WithUpgradeCommand_ShouldReturnPackageId()
    {
        // Arrange
        var args = new[] { "upgrade", "Docker.DockerDesktop" };

        // Act
        var result = _packageManagementService.ExtractPackageId(args);

        // Assert
        result.ShouldBe("Docker.DockerDesktop");
    }

    [Fact]
    public void ExtractPackageId_WithIdFlag_ShouldReturnPackageId()
    {
        // Arrange
        var args = new[] { "install", "--id", "Git.Git" };

        // Act
        var result = _packageManagementService.ExtractPackageId(args);

        // Assert
        result.ShouldBe("Git.Git");
    }

    [Fact]
    public void ExtractPackageId_WithShortIdFlag_ShouldReturnPackageId()
    {
        // Arrange
        var args = new[] { "install", "-i", "Git.Git" };

        // Act
        var result = _packageManagementService.ExtractPackageId(args);

        // Assert
        result.ShouldBe("Git.Git");
    }

    [Fact]
    public void ExtractPackageId_WithOnlyCommand_ShouldReturnNull()
    {
        // Arrange
        var args = new[] { "install" };

        // Act
        var result = _packageManagementService.ExtractPackageId(args);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ExtractPackageId_WithOptionsOnly_ShouldReturnNull()
    {
        // Arrange
        var args = new[] { "install", "--accept-source-agreements" };

        // Act
        var result = _packageManagementService.ExtractPackageId(args);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ExtractPackageId_WithEmptyArgs_ShouldReturnNull()
    {
        // Arrange
        var args = new string[] { };

        // Act
        var result = _packageManagementService.ExtractPackageId(args);

        // Assert
        result.ShouldBeNull();
    }

    #endregion
}