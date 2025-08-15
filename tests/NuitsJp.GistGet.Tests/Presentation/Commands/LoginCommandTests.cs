using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Presentation.Login;
using Xunit;

namespace NuitsJp.GistGet.Tests.Presentation.Commands;

public class LoginCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WithNoArgs_StartsAuthenticationFlow()
    {
        // Arrange
        var authService = new Mock<IGitHubAuthService>();
        var console = new Mock<ILoginConsole>();
        var logger = new Mock<ILogger<LoginCommand>>();

        authService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(true);

        var command = new LoginCommand(authService.Object, console.Object, logger.Object);

        // Act
        var result = await command.ExecuteAsync(new[] { "login" });

        // Assert
        Assert.Equal(0, result);
        authService.Verify(x => x.AuthenticateAsync(), Times.Once);
        console.Verify(x => x.NotifyAuthSuccess(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithStatusArg_ShowsAuthenticationStatus()
    {
        // Arrange
        var authService = new Mock<IGitHubAuthService>();
        var console = new Mock<ILoginConsole>();
        var logger = new Mock<ILogger<LoginCommand>>();

        authService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(true);

        var command = new LoginCommand(authService.Object, console.Object, logger.Object);

        // Act
        var result = await command.ExecuteAsync(new[] { "login", "status" });

        // Assert
        Assert.Equal(0, result);
        authService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        console.Verify(x => x.ShowAuthStatus(true, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AuthenticationFails_ReturnsErrorCode()
    {
        // Arrange
        var authService = new Mock<IGitHubAuthService>();
        var console = new Mock<ILoginConsole>();
        var logger = new Mock<ILogger<LoginCommand>>();

        authService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(false);

        var command = new LoginCommand(authService.Object, console.Object, logger.Object);

        // Act
        var result = await command.ExecuteAsync(new[] { "login" });

        // Assert
        Assert.Equal(1, result);
        console.Verify(x => x.NotifyAuthFailure(It.IsAny<string>()), Times.Once);
    }
}