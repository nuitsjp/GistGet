using Moq;
using Xunit;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GistGet;

public class GistGetServiceTests
{
    protected readonly Mock<IGitHubService> _authServiceMock;
    protected readonly Mock<IConsoleService> _consoleServiceMock;
    protected readonly Mock<ICredentialService> _credentialServiceMock;
    protected readonly Mock<IWinGetPassthroughRunner> _passthroughRunnerMock;
    protected readonly GistGetService _target;

    protected delegate bool TryGetCredentialDelegate(out Credential? credential);

    public GistGetServiceTests()
    {
        _authServiceMock = new Mock<IGitHubService>();
        _consoleServiceMock = new Mock<IConsoleService>();
        _credentialServiceMock = new Mock<ICredentialService>();
        _passthroughRunnerMock = new Mock<IWinGetPassthroughRunner>();
        _target = new GistGetService(_authServiceMock.Object, _consoleServiceMock.Object, _credentialServiceMock.Object, _passthroughRunnerMock.Object);
    }

    public class AuthLoginAsync : GistGetServiceTests
    {
        [Fact]
        public async Task CallsAuthServiceLogin_AndSavesCredential()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credential = new Credential("testuser", "gho_token");
            _authServiceMock.Setup(x => x.LoginAsync()).ReturnsAsync(credential);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.AuthLoginAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            _credentialServiceMock.Verify(x => x.SaveCredential(credential), Times.Once);
        }
    }

    public class AuthLogout : GistGetServiceTests
    {
        [Fact]
        public void CallsAuthServiceLogout_AndPrintsMessage()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            _credentialServiceMock.Setup(x => x.DeleteCredential()).Returns(true);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            _target.AuthLogout();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _credentialServiceMock.Verify(x => x.DeleteCredential(), Times.Once);
            _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("Logged out") || s.Contains("Log out"))), Times.Once);
        }
    }

    public class AuthStatus : GistGetServiceTests
    {
        [Fact]
        public async Task WhenNotAuthenticated_PrintsWarning()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = null;
                    return false;
                }));

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.AuthStatusAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("not logged in"))), Times.Once);
        }

        [Fact]
        public async Task WhenAuthenticated_PrintsStatus()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credential = new Credential("testuser", "gho_1234567890");
            var scopes = new List<string> { "gist", "read:org" };
            
            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetTokenStatusAsync(credential.Token))
                .ReturnsAsync(new TokenStatus("testuser", scopes));

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.AuthStatusAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _consoleServiceMock.Verify(x => x.WriteInfo("github.com"), Times.Once);
            _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("✓ Logged in to github.com account testuser (keyring)"))), Times.Once);
            _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("- Active account: true"))), Times.Once);
            _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("- Git operations protocol: https"))), Times.Once);
            _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("- Token: gho_**********"))), Times.Once);
            _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("- Token scopes: 'gist', 'read:org'"))), Times.Once);
        }
    }

    public class RunPassthroughAsync : GistGetServiceTests
    {
        [Fact]
        public async Task CallsRunner_WithCommandAndArgs()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var command = "search";
            var args = new[] { "vscode", "--source", "winget" };
            var expectedArgs = new[] { "search", "vscode", "--source", "winget" };
            var expectedExitCode = 0;

            _passthroughRunnerMock.Setup(x => x.RunAsync(It.IsAny<string[]>()))
                .ReturnsAsync(expectedExitCode);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.RunPassthroughAsync(command, args);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe(expectedExitCode);
            _passthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(a => a.SequenceEqual(expectedArgs))
            ), Times.Once);
        }
    }

    public class InstallAndSaveAsync : GistGetServiceTests
    {
        [Fact]
        public async Task WhenNotLoggedIn_CallsLogin_ThenProceeds()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var package = new GistGetPackage { Id = "Test.Package" };
            var savedCredential = new Credential("user", "token");
            Credential? currentCredential = null;

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = currentCredential;
                    return c != null;
                }));

            _authServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(savedCredential);

            _credentialServiceMock.Setup(x => x.SaveCredential(It.IsAny<Credential>()))
                .Callback<Credential>((c) => currentCredential = c);

            // Mock passthrough to succeed so it doesn't fail later
            _passthroughRunnerMock.Setup(x => x.RunAsync(It.IsAny<string[]>())).ReturnsAsync(0);

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.InstallAndSaveAsync(package);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            _passthroughRunnerMock.Verify(x => x.RunAsync(It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task CallsWinGet_AndUpdatesGist_WithPinLogic()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var installPackage = new GistGetPackage { Id = packageId, Silent = true }; // Version null
            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "1.2.3", PinType = "blocking" }
            };

            // Credential setup
            var credential = new Credential("user", "token");
            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            // Gist setup
            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, "", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            // Runner setup
            // Expect Install command with version 1.2.3 (from Pin)
            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains("1.2.3") &&
                    args.Contains("--silent"))))
                .ReturnsAsync(0);

            // Expect Pin Add command (restore pin)
            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "add" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains("1.2.3") &&
                    args.Contains("--blocking"))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.InstallAndSaveAsync(installPackage);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _passthroughRunnerMock.VerifyAll();
            // Verify SavePackagesAsync called with updated list (Pin/PinType preserved)
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Any(p => p.Id == packageId && p.Pin == "1.2.3" && p.PinType == "blocking" && p.Silent == true)
                )), Times.Once);
        }

        [Fact]
        public async Task WithExplicitVersion_UpdatesPin()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var explicitVersion = "2.0.0";
            var installPackage = new GistGetPackage { Id = packageId, Version = explicitVersion };
            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "1.0.0" }
            };

            // Credential setup
            var credential = new Credential("user", "token");
            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            // Gist setup
            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, "", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            // Runner setup
            // Expect Install command with explicit version 2.0.0
            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains(explicitVersion))))
                .ReturnsAsync(0);

            // Expect Pin Add command (update pin to 2.0.0)
            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "add" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains(explicitVersion))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.InstallAndSaveAsync(installPackage);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _passthroughRunnerMock.VerifyAll();
            // Verify SavePackagesAsync called with updated Pin
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Any(p => p.Id == packageId && p.Pin == explicitVersion)
                )), Times.Once);
        }
    }
}
