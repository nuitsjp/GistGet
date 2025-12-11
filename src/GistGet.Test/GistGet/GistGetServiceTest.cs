using Moq;
using Xunit;
using GistGet;

namespace GistGet.Test.GistGet;

public class GistGetServiceTest
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IConsoleService> _consoleServiceMock;
    private readonly Mock<ICredentialService> _credentialServiceMock;
    private readonly GistGetService _target;

    delegate bool TryGetCredentialDelegate(string target, out string? username, out string? token);

    public GistGetServiceTest()
    {
        _authServiceMock = new Mock<IAuthService>();
        _consoleServiceMock = new Mock<IConsoleService>();
        _credentialServiceMock = new Mock<ICredentialService>();
        _target = new GistGetService(_authServiceMock.Object, _consoleServiceMock.Object, _credentialServiceMock.Object);
    }

    [Fact]
    public async Task AuthLoginAsync_CallsAuthServiceLogin()
    {
        // Arrange
        _authServiceMock.Setup(x => x.LoginAsync()).Returns(Task.CompletedTask);

        // Act
        await _target.AuthLoginAsync();

        // Assert
        _authServiceMock.Verify(x => x.LoginAsync(), Times.Once);
    }

    [Fact]
    public async Task AuthLogoutAsync_CallsAuthServiceLogout_AndPrintsMessage()
    {
        // Arrange
        _authServiceMock.Setup(x => x.LogoutAsync()).Returns(Task.CompletedTask);

        // Act
        await _target.AuthLogoutAsync();

        // Assert
        _authServiceMock.Verify(x => x.LogoutAsync(), Times.Once);
        // Note: Exact message wording might vary but checking for a call is good
        _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("Logged out") || s.Contains("Log out"))), Times.Once); 
        // Or if we don't care about the message content yet:
        // _consoleServiceMock.Verify(x => x.WriteInfo(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AuthStatusAsync_WhenNotAuthenticated_PrintsWarning()
    {
        // Arrange
        // Not mocked to return credential, so returns false by default or explicit setup
        _credentialServiceMock
            .Setup(x => x.TryGetCredential(It.IsAny<string>(), out It.Ref<string?>.IsAny, out It.Ref<string?>.IsAny))
            .Returns(new TryGetCredentialDelegate((string target, out string? u, out string? t) => 
            {
                u = null;
                t = null;
                return false;
            }));

        // Act
        await _target.AuthStatusAsync();

        // Assert
        // gh cli output for not logged in: "You are not logged in to any hosts." or similar
        _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("not logged in"))), Times.Once);
    }

    [Fact]
    public async Task AuthStatusAsync_WhenAuthenticated_PrintsStatus()
    {
        // Arrange
        string? token = "gho_1234567890";
        string? user = "testuser";
        
        _credentialServiceMock
            .Setup(x => x.TryGetCredential("git:https://github.com", out It.Ref<string?>.IsAny, out It.Ref<string?>.IsAny))
            .Returns(new TryGetCredentialDelegate((string target, out string? u, out string? t) => 
            {
                u = user;
                t = token;
                return true;
            }));

        // Act
        await _target.AuthStatusAsync();

        // Assert
        // Check for key parts of gh cli status output
        _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("github.com"))), Times.AtLeastOnce, "Should mention host");
        _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("testuser"))), Times.AtLeastOnce, "Should mention username");
        _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("Token: **********"))), Times.AtLeastOnce, "Should mention token masked");
    }
}
