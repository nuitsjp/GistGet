using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Infrastructure.Os;
using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Presentation.WinGet;
using NuitsJp.GistGet.Tests.Mocks;
using Shouldly;
using Xunit;

namespace NuitsJp.GistGet.Tests.Presentation.Commands;

/// <summary>
/// WinGetCommandのテスト（Presentation層: UI制御・引数解析・終了コード検証）
/// </summary>
[Trait("Category", "Unit")]
public class WinGetCommandTests
{
    private readonly Mock<IWinGetClient> _mockWinGetClient;
    private readonly Mock<IGistSyncService> _mockGistSyncService;
    private readonly Mock<IOsService> _mockOsService;
    private readonly Mock<IWinGetConsole> _mockConsole;
    private readonly Mock<ILogger<WinGetCommand>> _mockLogger;
    private readonly WinGetCommand _winGetCommand;

    public WinGetCommandTests()
    {
        _mockWinGetClient = new Mock<IWinGetClient>();
        _mockGistSyncService = new Mock<IGistSyncService>();
        _mockOsService = new Mock<IOsService>();
        _mockConsole = new Mock<IWinGetConsole>();
        _mockLogger = new Mock<ILogger<WinGetCommand>>();

        _winGetCommand = new WinGetCommand(
            _mockWinGetClient.Object,
            _mockGistSyncService.Object,
            _mockOsService.Object,
            _mockConsole.Object,
            _mockLogger.Object);
    }

    #region ExecuteInstallAsync Tests

    [Fact]
    public async Task ExecuteInstallAsync_WithValidPackageId_ShouldReturnSuccessCode()
    {
        // Arrange
        var args = new[] { "install", "Git.Git" };
        _mockWinGetClient.Setup(x => x.InstallPackageAsync(args)).ReturnsAsync(0);
        _mockGistSyncService.Setup(x => x.AfterInstallAsync("Git.Git")).Returns(Task.CompletedTask);

        // Act
        var result = await _winGetCommand.ExecuteInstallAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockConsole.Verify(x => x.NotifyInstallStarting("Git.Git"), Times.Once);
        _mockConsole.Verify(x => x.NotifyOperationSuccess("インストール", "Git.Git"), Times.Once);
        _mockWinGetClient.Verify(x => x.InstallPackageAsync(args), Times.Once);
        _mockGistSyncService.Verify(x => x.AfterInstallAsync("Git.Git"), Times.Once);
    }

    [Fact]
    public async Task ExecuteInstallAsync_WithoutPackageId_ShouldReturnErrorCode()
    {
        // Arrange
        var args = new[] { "install" };

        // Act
        var result = await _winGetCommand.ExecuteInstallAsync(args);

        // Assert
        result.ShouldBe(1);
        _mockConsole.Verify(x => x.ShowError(It.IsAny<ArgumentException>(), "パッケージIDが指定されていません"), Times.Once);
        _mockWinGetClient.Verify(x => x.InstallPackageAsync(It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteInstallAsync_WhenWinGetClientFails_ShouldReturnErrorCode()
    {
        // Arrange
        var args = new[] { "install", "Git.Git" };
        _mockWinGetClient.Setup(x => x.InstallPackageAsync(args)).ReturnsAsync(1);

        // Act
        var result = await _winGetCommand.ExecuteInstallAsync(args);

        // Assert
        result.ShouldBe(1);
        _mockConsole.Verify(x => x.NotifyInstallStarting("Git.Git"), Times.Once);
        _mockConsole.Verify(x => x.NotifyOperationSuccess(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockGistSyncService.Verify(x => x.AfterInstallAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ExecuteUninstallAsync Tests

    [Fact]
    public async Task ExecuteUninstallAsync_WithValidPackageId_ShouldReturnSuccessCode()
    {
        // Arrange
        var args = new[] { "uninstall", "Git.Git" };
        _mockWinGetClient.Setup(x => x.UninstallPackageAsync(args)).ReturnsAsync(0);
        _mockGistSyncService.Setup(x => x.AfterUninstallAsync("Git.Git")).Returns(Task.CompletedTask);

        // Act
        var result = await _winGetCommand.ExecuteUninstallAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockConsole.Verify(x => x.NotifyUninstallStarting("Git.Git"), Times.Once);
        _mockConsole.Verify(x => x.NotifyOperationSuccess("アンインストール", "Git.Git"), Times.Once);
        _mockWinGetClient.Verify(x => x.UninstallPackageAsync(args), Times.Once);
        _mockGistSyncService.Verify(x => x.AfterUninstallAsync("Git.Git"), Times.Once);
    }

    #endregion

    #region ExecuteUpgradeAsync Tests

    [Fact]
    public async Task ExecuteUpgradeAsync_WithValidPackageId_ShouldReturnSuccessCode()
    {
        // Arrange
        var args = new[] { "upgrade", "Git.Git" };
        _mockWinGetClient.Setup(x => x.UpgradePackageAsync(args)).ReturnsAsync(0);

        // Act
        var result = await _winGetCommand.ExecuteUpgradeAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockConsole.Verify(x => x.NotifyUpgradeStarting("Git.Git"), Times.Once);
        _mockConsole.Verify(x => x.NotifyOperationSuccess("アップグレード", "Git.Git"), Times.Once);
        _mockWinGetClient.Verify(x => x.UpgradePackageAsync(args), Times.Once);
    }

    #endregion

    #region ExecutePassthroughAsync Tests

    [Fact]
    public async Task ExecutePassthroughAsync_WithValidArgs_ShouldReturnWinGetClientResult()
    {
        // Arrange
        var args = new[] { "list" };
        _mockWinGetClient.Setup(x => x.ExecutePassthroughAsync(args)).ReturnsAsync(0);

        // Act
        var result = await _winGetCommand.ExecutePassthroughAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockWinGetClient.Verify(x => x.ExecutePassthroughAsync(args), Times.Once);
    }

    [Fact]
    public async Task ExecutePassthroughAsync_WhenWinGetClientThrows_ShouldReturnErrorCode()
    {
        // Arrange
        var args = new[] { "list" };
        _mockWinGetClient.Setup(x => x.ExecutePassthroughAsync(args))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await _winGetCommand.ExecutePassthroughAsync(args);

        // Assert
        result.ShouldBe(1);
        _mockConsole.Verify(x => x.ShowError(It.IsAny<InvalidOperationException>(), "WinGetコマンドの実行に失敗しました"), Times.Once);
    }

    #endregion
}