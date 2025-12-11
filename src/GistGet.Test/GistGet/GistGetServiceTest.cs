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

    delegate bool TryGetCredentialDelegate(string target, out Credential? credential);

    public GistGetServiceTest()
    {
        _authServiceMock = new Mock<IAuthService>();
        _consoleServiceMock = new Mock<IConsoleService>();
        _credentialServiceMock = new Mock<ICredentialService>();
        _target = new GistGetService(_authServiceMock.Object, _consoleServiceMock.Object, _credentialServiceMock.Object);
    }

    [Fact]
    public async Task AuthLoginAsync_CallsAuthServiceLogin_AndSavesCredential()
    {
        // Arrange
        var credential = new Credential("testuser", "gho_token");
        _authServiceMock.Setup(x => x.LoginAsync()).ReturnsAsync(credential);

        // Act
        await _target.AuthLoginAsync();

        // Assert
        _authServiceMock.Verify(x => x.LoginAsync(), Times.Once);
        _credentialServiceMock.Verify(x => x.SaveCredential("git:https://github.com", credential), Times.Once);
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
        _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("Logged out") || s.Contains("Log out"))), Times.Once); 
    }

    [Fact]
    public void AuthStatus_WhenNotAuthenticated_PrintsWarning()
    {
        // Arrange
        // Not mocked to return credential, so returns false by default or explicit setup
        _credentialServiceMock
            .Setup(x => x.TryGetCredential(It.IsAny<string>(), out It.Ref<Credential?>.IsAny))
            .Returns(new TryGetCredentialDelegate((string target, out Credential? c) => 
            {
                c = null;
                return false;
            }));

        // Act
        _target.AuthStatus();

        // Assert
        _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("not logged in"))), Times.Once);
    }

    [Fact]
    public void AuthStatus_WhenAuthenticated_PrintsStatus()
    {
        // Arrange
        var credential = new Credential("testuser", "gho_1234567890");
        
        _credentialServiceMock
            .Setup(x => x.TryGetCredential("git:https://github.com", out It.Ref<Credential?>.IsAny))
            .Returns(new TryGetCredentialDelegate((string target, out Credential? c) => 
            {
                c = credential;
                return true;
            }));

        // Act
        _target.AuthStatus();

        // Assert
        _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("github.com"))), Times.AtLeastOnce, "Should mention host");
        _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("testuser"))), Times.AtLeastOnce, "Should mention username");
        _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("Token: **********"))), Times.AtLeastOnce, "Should mention token masked");
    }


}
