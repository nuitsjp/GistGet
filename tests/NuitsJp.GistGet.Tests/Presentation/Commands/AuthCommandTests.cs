using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Presentation.Auth;
using NuitsJp.GistGet.Presentation.Console;
using Shouldly;
using Xunit;

namespace NuitsJp.GistGet.Tests.Presentation.Commands;

/// <summary>
/// AuthCommandのPresentation層テスト（t-wada式TDD対応）
/// UI制御・コンソール出力・終了コードの検証に特化
/// Infrastructure層は完全にモック化
/// </summary>
public class AuthCommandTests
{
    private readonly Mock<IGitHubAuthService> _mockAuthService;
    private readonly Mock<IAuthConsole> _mockConsole;
    private readonly Mock<ILogger<AuthCommand>> _mockLogger;
    private readonly AuthCommand _authCommand;

    public AuthCommandTests()
    {
        _mockAuthService = new Mock<IGitHubAuthService>();
        _mockConsole = new Mock<IAuthConsole>();
        _mockLogger = new Mock<ILogger<AuthCommand>>();
        _authCommand = new AuthCommand(_mockAuthService.Object, _mockConsole.Object, _mockLogger.Object);
    }

    #region UI Control Tests - Success Scenarios

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenAuthenticationSucceeds()
    {
        // Arrange - Presentation層: UI制御の成功パターンテスト
        _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(true);
        var args = new[] { "auth" };

        // Act
        var result = await _authCommand.ExecuteAsync(args);

        // Assert - UI制御: 成功時の終了コード検証
        result.ShouldBe(0);
        _mockAuthService.Verify(x => x.AuthenticateAsync(), Times.Once);
    }

    #endregion

    #region UI Control Tests - Error Scenarios

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenAuthenticationFails()
    {
        // Arrange - Presentation層: UI制御の失敗パターンテスト
        _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(false);
        var args = new[] { "auth" };

        // Act
        var result = await _authCommand.ExecuteAsync(args);

        // Assert - UI制御: 失敗時の終了コード検証
        result.ShouldBe(1);
        _mockAuthService.Verify(x => x.AuthenticateAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange - Presentation層: 例外発生時のUI制御テスト
        _mockAuthService.Setup(x => x.AuthenticateAsync()).ThrowsAsync(new InvalidOperationException("Auth failed"));
        var args = new[] { "auth" };

        // Act
        var result = await _authCommand.ExecuteAsync(args);

        // Assert - UI制御: 例外時の終了コード検証とログ出力確認
        result.ShouldBe(1);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("認証フロー実行中にエラーが発生しました")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Argument Processing Tests

    [Theory]
    [InlineData("auth")]
    [InlineData("auth", "--verbose")]
    [InlineData("auth", "extra", "args")]
    public async Task ExecuteAsync_ShouldIgnoreExtraArguments_WhenProcessingAuthCommand(params string[] args)
    {
        // Arrange - Presentation層: 引数処理の柔軟性テスト
        _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(true);

        // Act
        var result = await _authCommand.ExecuteAsync(args);

        // Assert - UI制御: 余分な引数があっても正常動作すること
        result.ShouldBe(0);
        _mockAuthService.Verify(x => x.AuthenticateAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleNullArgs_Gracefully()
    {
        // Arrange - Presentation層: NULL引数の場合、例外が発生してエラー終了することをテスト
        _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(true);

        // Act
        var result = await _authCommand.ExecuteAsync(null!);

        // Assert - UI制御: NULL引数の場合、例外処理により終了コード1を返すこと
        result.ShouldBe(1);
        // AuthenticateAsyncは呼ばれない（null参照例外により早期終了）
        _mockAuthService.Verify(x => x.AuthenticateAsync(), Times.Never);
    }

    #endregion

    #region Business Layer Isolation Tests

    [Fact]
    public async Task ExecuteAsync_ShouldOnlyCallAuthService_NotOtherServices()
    {
        // Arrange - Presentation層: Business層の完全分離検証
        _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(true);
        var args = new[] { "auth" };

        // Act
        await _authCommand.ExecuteAsync(args);

        // Assert - Presentation層テスト: AuthServiceのみが呼ばれることを確認
        _mockAuthService.Verify(x => x.AuthenticateAsync(), Times.Once);
        _mockAuthService.VerifyNoOtherCalls();
    }

    #endregion
}