using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Presentation.Login;

namespace NuitsJp.GistGet.Tests.Presentation.Commands;

/// <summary>
/// LogoutCommandのテスト
/// </summary>
public class LogoutCommandTests
{
    private readonly Mock<IGitHubAuthService> _mockAuthService;
    private readonly Mock<ILogoutConsole> _mockConsole;
    private readonly LogoutCommand _command;

    public LogoutCommandTests()
    {
        _mockAuthService = new Mock<IGitHubAuthService>();
        _mockConsole = new Mock<ILogoutConsole>();
        var mockLogger = new Mock<ILogger<LogoutCommand>>();

        _command = new LogoutCommand(
            _mockAuthService.Object,
            _mockConsole.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithConfirmation_ShouldLogoutSuccessfully()
    {
        // Arrange
        _mockConsole.Setup(x => x.ConfirmLogout()).Returns(true);
        _mockAuthService.Setup(x => x.LogoutAsync()).ReturnsAsync(true);

        // Act
        var result = await _command.ExecuteAsync(["logout"]);

        // Assert
        Assert.Equal(0, result);
        _mockConsole.Verify(x => x.ConfirmLogout(), Times.Once);
        _mockAuthService.Verify(x => x.LogoutAsync(), Times.Once);
        _mockConsole.Verify(x => x.NotifyLogoutSuccess(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithSilentMode_ShouldSkipConfirmation()
    {
        // Arrange
        _mockAuthService.Setup(x => x.LogoutAsync()).ReturnsAsync(true);

        // Act
        var result = await _command.ExecuteAsync(["logout", "--silent"]);

        // Assert
        Assert.Equal(0, result);
        _mockConsole.Verify(x => x.ConfirmLogout(), Times.Never);
        _mockAuthService.Verify(x => x.LogoutAsync(), Times.Once);
        _mockConsole.Verify(x => x.NotifyLogoutSuccess(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UserDeclines_ShouldCancelLogout()
    {
        // Arrange
        _mockConsole.Setup(x => x.ConfirmLogout()).Returns(false);

        // Act
        var result = await _command.ExecuteAsync(["logout"]);

        // Assert
        Assert.Equal(0, result); // キャンセルは正常終了扱い
        _mockConsole.Verify(x => x.ConfirmLogout(), Times.Once);
        _mockAuthService.Verify(x => x.LogoutAsync(), Times.Never);
        _mockConsole.Verify(x => x.NotifyLogoutSuccess(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_LogoutFails_ShouldReturnError()
    {
        // Arrange
        _mockConsole.Setup(x => x.ConfirmLogout()).Returns(true);
        _mockAuthService.Setup(x => x.LogoutAsync()).ReturnsAsync(false);

        // Act
        var result = await _command.ExecuteAsync(["logout"]);

        // Assert
        Assert.Equal(1, result);
        _mockConsole.Verify(x => x.NotifyLogoutFailure("ログアウト処理に失敗しました"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ExceptionThrown_ShouldHandleGracefully()
    {
        // Arrange
        _mockConsole.Setup(x => x.ConfirmLogout()).Returns(true);
        _mockAuthService.Setup(x => x.LogoutAsync()).ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _command.ExecuteAsync(["logout"]);

        // Assert
        Assert.Equal(1, result);
        _mockConsole.Verify(x => x.ShowError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }
}