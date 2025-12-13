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
            var options = new InstallOptions { Id = "Test.Package" };
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
            await _target.InstallAndSaveAsync(options);

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
            var installOptions = new InstallOptions { Id = packageId, Silent = true }; // Version null
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
            await _target.InstallAndSaveAsync(installOptions);

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
            var installOptions = new InstallOptions { Id = packageId, Version = explicitVersion };
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
            await _target.InstallAndSaveAsync(installOptions);

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
                    list.Any(p => p.Id == packageId && p.Pin == explicitVersion && p.Version == explicitVersion)
                )), Times.Once);
        }

        [Fact]
        public async Task WithExplicitVersionWithoutPin_DoesNotPersistVersionOrPin()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var explicitVersion = "2.1.0";
            var installOptions = new InstallOptions { Id = packageId, Version = explicitVersion };

            // Credential setup
            var credential = new Credential("user", "token");
            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            // Gist setup: no existing entry -> no pin
            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, "", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            // Runner setup: only install should be called
            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains(explicitVersion))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.InstallAndSaveAsync(installOptions);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _passthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args => args[0] == "pin")), Times.Never);
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Any(p => p.Id == packageId && p.Pin == null && p.Version == null)
                )), Times.Once);
        }

        [Fact]
        public async Task PassesWingetOptions_ToInstall()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var installOptions = new InstallOptions
            {
                Id = packageId,
                AllowHashMismatch = true,
                SkipDependencies = true,
                InstallerType = "msi",
                Locale = "ja-JP"
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

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, "", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--ignore-security-hash") &&
                    args.Contains("--skip-dependencies") &&
                    args.Contains("--installer-type") && args.Contains("msi") &&
                    args.Contains("--locale") && args.Contains("ja-JP"))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.InstallAndSaveAsync(installOptions);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _passthroughRunnerMock.VerifyAll();
        }
    }

    public class UninstallAndSaveAsync : GistGetServiceTests
    {
        [Fact]
        public async Task WhenNotLoggedIn_PerformsLogin_AndSavesUninstallEntry()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
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

            _credentialServiceMock
                .Setup(x => x.SaveCredential(It.IsAny<Credential>()))
                .Callback<Credential>(c => currentCredential = c);

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(savedCredential.Token, "", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            _authServiceMock
                .Setup(x => x.SavePackagesAsync(
                    savedCredential.Token,
                    "",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<IReadOnlyList<GistGetPackage>>(list =>
                        list.Any(p => p.Id == packageId && p.Uninstall))))
                .Returns(Task.CompletedTask);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UninstallAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            _passthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "uninstall" && args.Contains("--id") && args.Contains(packageId))
            ), Times.Once);
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                savedCredential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Once);
        }

        [Fact]
        public async Task WhenPinned_RemovesPinAndMarksUninstall()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var credential = new Credential("user", "token");
            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "1.0.0", PinType = "blocking" }
            };

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, "", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            var sequence = new MockSequence();

            _passthroughRunnerMock
                .InSequence(sequence)
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            _passthroughRunnerMock
                .InSequence(sequence)
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "remove" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            _authServiceMock
                .Setup(x => x.SavePackagesAsync(
                    credential.Token,
                    "",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<IReadOnlyList<GistGetPackage>>(list =>
                        list.Any(p => p.Id == packageId && p.Uninstall && p.Pin == null && p.PinType == null))))
                .Returns(Task.CompletedTask);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UninstallAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _passthroughRunnerMock.VerifyAll();
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Once);
        }

        [Fact]
        public async Task WhenUninstallFails_DoesNotSaveOrRemovePin()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var credential = new Credential("user", "token");

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "1.2.3" }
            };

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, "", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(1);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UninstallAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _passthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "pin" && args.Contains("remove"))
            ), Times.Never);
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Never);
        }

        [Fact]
        public async Task WhenNoGistEntry_CreatesNewEntryWithUninstallTrue()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "New.Package";
            var credential = new Credential("user", "token");

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            // Gist に該当パッケージのエントリがない
            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, "", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UninstallAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            // Pin がないため pin remove は呼ばれない
            _passthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "pin" && args.Contains("remove"))
            ), Times.Never);

            // 新規エントリが作成され、uninstall: true で保存される
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Count == 1 &&
                    list.Any(p => p.Id == packageId && p.Uninstall && p.Pin == null)
                )), Times.Once);
        }

        [Fact]
        public async Task WhenNoPinExists_SkipsPinRemove()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var credential = new Credential("user", "token");

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            // Gist にエントリはあるが Pin は設定されていない
            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Silent = true, Scope = "user" }
            };

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, "", It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UninstallAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            // Pin がないため pin remove は呼ばれない
            _passthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "pin" && args.Contains("remove"))
            ), Times.Never);

            // 既存のプロパティが保持され、uninstall: true で保存される
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Any(p => p.Id == packageId && p.Uninstall && p.Silent && p.Scope == "user")
                )), Times.Once);
        }
    }
}
