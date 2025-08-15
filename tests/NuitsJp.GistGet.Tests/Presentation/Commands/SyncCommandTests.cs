using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Business.Models;
using NuitsJp.GistGet.Business.Services;
using NuitsJp.GistGet.Presentation.Console;
using NuitsJp.GistGet.Presentation.Sync;
using Shouldly;
using Xunit;

namespace NuitsJp.GistGet.Tests.Presentation.Commands;

/// <summary>
/// SyncCommandのPresentation層テスト（t-wada式TDD対応）
/// UI制御・コンソール出力・終了コードの検証に特化
/// Business層は完全にモック化
/// </summary>
public class SyncCommandTests
{
    private readonly Mock<IGistSyncService> _mockSyncService;
    private readonly Mock<ISyncConsole> _mockConsole;
    private readonly Mock<ILogger<SyncCommand>> _mockLogger;
    private readonly SyncCommand _syncCommand;

    public SyncCommandTests()
    {
        _mockSyncService = new Mock<IGistSyncService>();
        _mockConsole = new Mock<ISyncConsole>();
        _mockLogger = new Mock<ILogger<SyncCommand>>();
        _syncCommand = new SyncCommand(_mockSyncService.Object, _mockConsole.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitializeCorrectly()
    {
        // Act & Assert - Presentation層: 依存注入の正常初期化テスト
        Should.NotThrow(() => new SyncCommand(
            _mockSyncService.Object,
            _mockConsole.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullSyncService_ShouldThrowArgumentNullException()
    {
        // Act & Assert - Presentation層: null依存の適切な例外処理テスト
        Should.Throw<ArgumentNullException>(() => new SyncCommand(
            null!,
            _mockConsole.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullConsole_ShouldThrowArgumentNullException()
    {
        // Act & Assert - Presentation層: null依存の適切な例外処理テスト
        Should.Throw<ArgumentNullException>(() => new SyncCommand(
            _mockSyncService.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert - Presentation層: null依存の適切な例外処理テスト
        Should.Throw<ArgumentNullException>(() => new SyncCommand(
            _mockSyncService.Object,
            _mockConsole.Object,
            null!));
    }

    #endregion

    #region UI Control Tests - Success Scenarios

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenSyncSucceeds()
    {
        // Arrange - Presentation層: UI制御の成功パターンテスト
        var successResult = new SyncResult
        {
            ExitCode = 0,
            InstalledPackages = { "Git.Git" },
            UninstalledPackages = { },
            FailedPackages = { },
            RebootRequired = false
        };

        _mockSyncService.Setup(x => x.SyncAsync()).ReturnsAsync(successResult);
        _mockConsole.Setup(x => x.ShowSyncResultAndGetAction(It.IsAny<SyncResult>()))
            .Returns(SyncUserAction.Continue);

        var args = new[] { "sync" };

        // Act
        var result = await _syncCommand.ExecuteAsync(args);

        // Assert - UI制御: 成功時の終了コード検証
        result.ShouldBe(0);
        _mockSyncService.Verify(x => x.SyncAsync(), Times.Once);
        _mockConsole.Verify(x => x.NotifySyncStarting(), Times.Once);
        _mockConsole.Verify(x => x.ShowSyncResultAndGetAction(successResult), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenSyncFails()
    {
        // Arrange - Presentation層: UI制御の失敗パターンテスト
        var failureResult = new SyncResult
        {
            ExitCode = 1,
            InstalledPackages = { },
            UninstalledPackages = { },
            FailedPackages = { "Git.Git" },
            RebootRequired = false
        };

        _mockSyncService.Setup(x => x.SyncAsync()).ReturnsAsync(failureResult);
        _mockConsole.Setup(x => x.ShowSyncResultAndGetAction(It.IsAny<SyncResult>()))
            .Returns(SyncUserAction.Continue);

        var args = new[] { "sync" };

        // Act
        var result = await _syncCommand.ExecuteAsync(args);

        // Assert - UI制御: 失敗時の終了コード検証
        result.ShouldBe(1);
        _mockSyncService.Verify(x => x.SyncAsync(), Times.Once);
        _mockConsole.Verify(x => x.NotifySyncStarting(), Times.Once);
        _mockConsole.Verify(x => x.ShowSyncResultAndGetAction(failureResult), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithDryRunOption_ShouldReturnErrorWithUnimplementedMessage()
    {
        // Arrange - Presentation層: --dry-runオプションのUI制御テスト
        var args = new[] { "sync", "--dry-run" };

        // Act
        var result = await _syncCommand.ExecuteAsync(args);

        // Assert - UI制御: 未実装機能の適切な処理検証
        result.ShouldBe(1);
        _mockConsole.Verify(x => x.NotifyUnimplementedFeature("ドライランモード"), Times.Once);
        _mockSyncService.Verify(x => x.SyncAsync(), Times.Never);
    }

    #endregion

    #region Reboot Handling Tests

    [Fact]
    public async Task ExecuteAsync_WithRebootRequired_ShouldPromptUserAndExecuteReboot()
    {
        // Arrange - Presentation層: 再起動要求時のUI制御フローテスト
        var rebootResult = new SyncResult
        {
            ExitCode = 0,
            InstalledPackages = { "Microsoft.VisualStudio.2022.Community" },
            UninstalledPackages = { },
            FailedPackages = { },
            RebootRequired = true
        };

        _mockSyncService.Setup(x => x.SyncAsync()).ReturnsAsync(rebootResult);
        _mockConsole.Setup(x => x.ShowSyncResultAndGetAction(It.IsAny<SyncResult>()))
            .Returns(SyncUserAction.Continue);
        _mockConsole.Setup(x => x.ConfirmRebootWithPackageList(It.IsAny<List<string>>()))
            .Returns(true);

        var args = new[] { "sync" };

        // Act
        var result = await _syncCommand.ExecuteAsync(args);

        // Assert - UI制御: 再起動フローの適切な処理検証
        result.ShouldBe(0);
        _mockConsole.Verify(x => x.ConfirmRebootWithPackageList(
            It.Is<List<string>>(list => list.Contains("Microsoft.VisualStudio.2022.Community"))), Times.Once);
        _mockConsole.Verify(x => x.NotifyRebootExecuting(), Times.Once);
        _mockSyncService.Verify(x => x.ExecuteRebootAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithRebootRequiredButUserDeclines_ShouldNotExecuteReboot()
    {
        // Arrange - Presentation層: 再起動拒否時のUI制御フローテスト
        var rebootResult = new SyncResult
        {
            ExitCode = 0,
            InstalledPackages = { "Microsoft.VisualStudio.2022.Community" },
            UninstalledPackages = { },
            FailedPackages = { },
            RebootRequired = true
        };

        _mockSyncService.Setup(x => x.SyncAsync()).ReturnsAsync(rebootResult);
        _mockConsole.Setup(x => x.ShowSyncResultAndGetAction(It.IsAny<SyncResult>()))
            .Returns(SyncUserAction.Continue);
        _mockConsole.Setup(x => x.ConfirmRebootWithPackageList(It.IsAny<List<string>>()))
            .Returns(false);

        var args = new[] { "sync" };

        // Act
        var result = await _syncCommand.ExecuteAsync(args);

        // Assert - UI制御: 再起動拒否時の適切な処理検証
        result.ShouldBe(0);
        _mockConsole.Verify(x => x.ConfirmRebootWithPackageList(It.IsAny<List<string>>()), Times.Once);
        _mockConsole.Verify(x => x.NotifyRebootExecuting(), Times.Never);
        _mockSyncService.Verify(x => x.ExecuteRebootAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithForceRebootOption_ShouldExecuteRebootWithoutPrompt()
    {
        // Arrange - Presentation層: --force-rebootオプションのUI制御テスト
        var rebootResult = new SyncResult
        {
            ExitCode = 0,
            InstalledPackages = { "Microsoft.VisualStudio.2022.Community" },
            UninstalledPackages = { },
            FailedPackages = { },
            RebootRequired = true
        };

        _mockSyncService.Setup(x => x.SyncAsync()).ReturnsAsync(rebootResult);
        _mockConsole.Setup(x => x.ShowSyncResultAndGetAction(It.IsAny<SyncResult>()))
            .Returns(SyncUserAction.Continue);

        var args = new[] { "sync", "--force-reboot" };

        // Act
        var result = await _syncCommand.ExecuteAsync(args);

        // Assert - UI制御: 強制再起動時の適切な処理検証
        result.ShouldBe(0);
        _mockConsole.Verify(x => x.ConfirmRebootWithPackageList(It.IsAny<List<string>>()), Times.Never);
        _mockConsole.Verify(x => x.NotifyRebootExecuting(), Times.Once);
        _mockSyncService.Verify(x => x.ExecuteRebootAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithSkipRebootOption_ShouldNotExecuteReboot()
    {
        // Arrange - Presentation層: --skip-rebootオプションのUI制御テスト
        var rebootResult = new SyncResult
        {
            ExitCode = 0,
            InstalledPackages = { "Microsoft.VisualStudio.2022.Community" },
            UninstalledPackages = { },
            FailedPackages = { },
            RebootRequired = true
        };

        _mockSyncService.Setup(x => x.SyncAsync()).ReturnsAsync(rebootResult);
        _mockConsole.Setup(x => x.ShowSyncResultAndGetAction(It.IsAny<SyncResult>()))
            .Returns(SyncUserAction.Continue);

        var args = new[] { "sync", "--skip-reboot" };

        // Act
        var result = await _syncCommand.ExecuteAsync(args);

        // Assert - UI制御: 再起動スキップ時の適切な処理検証
        result.ShouldBe(0);
        _mockConsole.Verify(x => x.ConfirmRebootWithPackageList(It.IsAny<List<string>>()), Times.Never);
        _mockConsole.Verify(x => x.NotifyRebootExecuting(), Times.Never);
        _mockSyncService.Verify(x => x.ExecuteRebootAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithSkipRebootUserAction_ShouldNotExecuteReboot()
    {
        // Arrange - Presentation層: ユーザーアクションでの再起動スキップUI制御テスト
        var rebootResult = new SyncResult
        {
            ExitCode = 0,
            InstalledPackages = { "Microsoft.VisualStudio.2022.Community" },
            UninstalledPackages = { },
            FailedPackages = { },
            RebootRequired = true
        };

        _mockSyncService.Setup(x => x.SyncAsync()).ReturnsAsync(rebootResult);
        _mockConsole.Setup(x => x.ShowSyncResultAndGetAction(It.IsAny<SyncResult>()))
            .Returns(SyncUserAction.SkipReboot);

        var args = new[] { "sync" };

        // Act
        var result = await _syncCommand.ExecuteAsync(args);

        // Assert - UI制御: ユーザーアクションでの再起動スキップ検証
        result.ShouldBe(0);
        _mockConsole.Verify(x => x.ConfirmRebootWithPackageList(It.IsAny<List<string>>()), Times.Never);
        _mockConsole.Verify(x => x.NotifyRebootExecuting(), Times.Never);
        _mockSyncService.Verify(x => x.ExecuteRebootAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithForceRebootUserAction_ShouldExecuteReboot()
    {
        // Arrange - Presentation層: ユーザーアクションでの強制再起動UI制御テスト
        var rebootResult = new SyncResult
        {
            ExitCode = 0,
            InstalledPackages = { "Microsoft.VisualStudio.2022.Community" },
            UninstalledPackages = { },
            FailedPackages = { },
            RebootRequired = true
        };

        _mockSyncService.Setup(x => x.SyncAsync()).ReturnsAsync(rebootResult);
        _mockConsole.Setup(x => x.ShowSyncResultAndGetAction(It.IsAny<SyncResult>()))
            .Returns(SyncUserAction.ForceReboot);

        var args = new[] { "sync" };

        // Act
        var result = await _syncCommand.ExecuteAsync(args);

        // Assert - UI制御: ユーザーアクションでの強制再起動検証
        result.ShouldBe(0);
        _mockConsole.Verify(x => x.ConfirmRebootWithPackageList(It.IsAny<List<string>>()), Times.Never);
        _mockConsole.Verify(x => x.NotifyRebootExecuting(), Times.Once);
        _mockSyncService.Verify(x => x.ExecuteRebootAsync(), Times.Once);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task ExecuteAsync_WithException_ShouldReturnErrorAndShowErrorMessage()
    {
        // Arrange - Presentation層: 例外発生時のUI制御とエラー表示テスト
        var exception = new InvalidOperationException("Sync operation failed");
        _mockSyncService.Setup(x => x.SyncAsync()).ThrowsAsync(exception);

        var args = new[] { "sync" };

        // Act
        var result = await _syncCommand.ExecuteAsync(args);

        // Assert - UI制御: 例外時のエラー処理とUI表示検証
        result.ShouldBe(1);
        _mockConsole.Verify(x => x.NotifySyncStarting(), Times.Once);
        _mockConsole.Verify(x => x.ShowError(exception, "同期処理でエラーが発生しました"), Times.Once);
        _mockConsole.Verify(x => x.ShowSyncResultAndGetAction(It.IsAny<SyncResult>()), Times.Never);
    }

    #endregion
}