using System;
using Moq;
using Xunit;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GistGet.Infrastructure;

namespace GistGet;

public class GistGetServiceTests
{
    protected readonly Mock<IGitHubService> _authServiceMock;
    protected readonly Mock<IConsoleService> _consoleServiceMock;
    protected readonly Mock<ICredentialService> _credentialServiceMock;
    protected readonly Mock<IWinGetPassthroughRunner> _passthroughRunnerMock;
    protected readonly Mock<IWinGetService> _winGetServiceMock;
    protected readonly IWinGetArgumentBuilder _argumentBuilder; // Use real instance
    protected readonly GistGetService _target;

    protected delegate bool TryGetCredentialDelegate(out Credential? credential);

    public GistGetServiceTests()
    {
        _authServiceMock = new Mock<IGitHubService>();
        _consoleServiceMock = new Mock<IConsoleService>();
        _credentialServiceMock = new Mock<ICredentialService>();
        _passthroughRunnerMock = new Mock<IWinGetPassthroughRunner>();
        _winGetServiceMock = new Mock<IWinGetService>();
        _argumentBuilder = new Infrastructure.WinGetArgumentBuilder(); // Real instance
        _target = new GistGetService(
            _authServiceMock.Object, 
            _consoleServiceMock.Object, 
            _credentialServiceMock.Object, 
            _passthroughRunnerMock.Object, 
            _winGetServiceMock.Object,
            _argumentBuilder);
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
                .Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
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
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
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
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
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
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
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
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
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

        [Fact]
        public async Task PassesCustomOption_ToWinget()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var customArgs = "/VERYSILENT /NORESTART";
            var installOptions = new InstallOptions
            {
                Id = packageId,
                Custom = customArgs
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
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--custom") && args.Contains(customArgs))))
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

        [Fact]
        public async Task WhenInstallFails_ReturnsNonZeroExitCode()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Fail.Package";
            var expectedExitCode = 1;

            var credential = new Credential("user", "token");
            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(expectedExitCode);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.InstallAndSaveAsync(new InstallOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe(expectedExitCode);
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Never);
        }
    }

    public class UpgradeAndSaveAsync : GistGetServiceTests
    {
        [Fact]
        public async Task WhenPinnedAndNoVersion_UpdatesPinWithUsableVersion()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var credential = new Credential("user", "token");
            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "1.0.0", PinType = "blocking", Silent = true }
            };

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _winGetServiceMock
                .Setup(x => x.FindById(It.Is<PackageId>(p => p.AsPrimitive() == packageId)))
                .Returns(new WinGetPackage("Test Package", new PackageId(packageId), new Version("1.0.0"), new Version("2.0.0")));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            var sequence = new MockSequence();

            _passthroughRunnerMock
                .InSequence(sequence)
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            _passthroughRunnerMock
                .InSequence(sequence)
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "add" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains("2.0.0") &&
                    args.Contains("--blocking") &&
                    args.Contains("--force"))))
                .ReturnsAsync(0);

            _authServiceMock
                .Setup(x => x.SavePackagesAsync(
                    credential.Token,
                    "",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<IReadOnlyList<GistGetPackage>>(list =>
                        list.Any(p => p.Id == packageId && p.Pin == "2.0.0" && p.PinType == "blocking" && p.Version == "2.0.0" && p.Silent)
                    )))
                .Returns(Task.CompletedTask);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UpgradeAndSaveAsync(new UpgradeOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _winGetServiceMock.Verify(x => x.FindById(It.Is<PackageId>(p => p.AsPrimitive() == packageId)), Times.Once);
            _passthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args => args[0] == "upgrade" && args.Contains(packageId))), Times.Once);
            _passthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args => args[0] == "pin" && args[1] == "add")), Times.Once);
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Once);
        }

        [Fact]
        public async Task WhenExistingEntryHasNoPin_DoesNotUpdateGist()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var credential = new Credential("user", "token");
            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Silent = true, Uninstall = false }
            };

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UpgradeAndSaveAsync(new UpgradeOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _passthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args => args[0] == "pin")), Times.Never);
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Never);
            _winGetServiceMock.Verify(x => x.FindById(It.IsAny<PackageId>()), Times.Never);
        }

        [Fact]
        public async Task WhenNoExistingEntry_AddsNewEntryAfterUpgrade()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "New.Package";
            var version = "5.1.0";
            var credential = new Credential("user", "token");

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains(version))))
                .ReturnsAsync(0);

            _authServiceMock
                .Setup(x => x.SavePackagesAsync(
                    credential.Token,
                    "",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<IReadOnlyList<GistGetPackage>>(list =>
                        list.Count == 1 &&
                        list.Any(p => p.Id == packageId && p.Pin == null && p.Version == null && !p.Uninstall)
                    )))
                .Returns(Task.CompletedTask);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UpgradeAndSaveAsync(new UpgradeOptions { Id = packageId, Version = version });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _passthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args => args[0] == "pin")), Times.Never);
            _winGetServiceMock.Verify(x => x.FindById(It.IsAny<PackageId>()), Times.Never);
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Once);
        }

        [Fact]
        public async Task WhenUpgradeFails_DoesNotChangeGist()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Fail.Package";
            var credential = new Credential("user", "token");

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(1);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UpgradeAndSaveAsync(new UpgradeOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _passthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args => args[0] == "pin")), Times.Never);
            _authServiceMock.Verify(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Never);
        }

        [Fact]
        public async Task WhenNotLoggedIn_CallsLogin_ThenProceeds()
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

            _credentialServiceMock.Setup(x => x.SaveCredential(It.IsAny<Credential>()))
                .Callback<Credential>((c) => currentCredential = c);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UpgradeAndSaveAsync(new UpgradeOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            _passthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args =>
                args[0] == "upgrade" && args.Contains(packageId))), Times.Once);
        }

        [Fact]
        public async Task WhenUpgradeFails_ReturnsNonZeroExitCode()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Fail.Package";
            var expectedExitCode = 1;

            var credential = new Credential("user", "token");
            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(expectedExitCode);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.UpgradeAndSaveAsync(new UpgradeOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe(expectedExitCode);
            _authServiceMock.Verify(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Never);
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
                .Setup(x => x.GetPackagesAsync(savedCredential.Token, It.IsAny<string>(), It.IsAny<string>()))
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
            await _target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

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
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
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
            await _target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

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
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(1);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

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
        public async Task WhenUninstallFails_ReturnsNonZeroExitCode()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Fail.Package";
            var expectedExitCode = 1;

            var credential = new Credential("user", "token");
            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(expectedExitCode);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe(expectedExitCode);
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
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            // pin remove は常に呼ばれる（エラーは無視）
            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "remove" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            // ローカルのpinを確実に削除するため、常に pin remove が呼ばれる
            _passthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "pin" && args.Contains("remove"))
            ), Times.Once);

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
        public async Task WhenNoPinInGist_StillCallsPinRemove()
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
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            // pin remove は常に呼ばれる（ローカルにpinがある可能性があるため）
            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "remove" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            // Gist側にPinがなくても、ローカルのpinを確実に削除するため pin remove が呼ばれる
            _passthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "pin" && args.Contains("remove"))
            ), Times.Once);

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

    public class PinAddAndSaveAsync : GistGetServiceTests
    {
        [Fact]
        public async Task WhenPinSucceeds_SavesPinToGist()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var version = "1.2.3";
            var credential = new Credential("user", "token");

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "add" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains(version) &&
                    !args.Contains("--force"))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.PinAddAndSaveAsync(packageId, version);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Count == 1 &&
                    list.Any(p =>
                        p.Id == packageId &&
                        p.Uninstall == false &&
                        p.Pin == version &&
                        p.Version == version &&
                        p.PinType == null)
                )), Times.Once);
        }

        [Fact]
        public async Task WhenPinFails_DoesNotSaveToGist()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var version = "1.2.3";
            var credential = new Credential("user", "token");

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "add" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains(version))))
                .ReturnsAsync(1);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.PinAddAndSaveAsync(packageId, version);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()
            ), Times.Never);
        }

        [Fact]
        public async Task WhenExistingPackageHasBlockingPinType_PassesBlockingAndPreservesSettings()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var version = "1.2.3";
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
                new GistGetPackage { Id = packageId, Uninstall = true, PinType = "blocking", Silent = true, Scope = "user" }
            };

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "add" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains(version) &&
                    args.Contains("--force") &&
                    args.Contains("--blocking"))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.PinAddAndSaveAsync(packageId, version, force: true);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Count == 1 &&
                    list.Any(p =>
                        p.Id == packageId &&
                        p.Uninstall == false &&
                        p.Pin == version &&
                        p.Version == version &&
                        p.PinType == "blocking" &&
                        p.Silent &&
                        p.Scope == "user")
                )), Times.Once);
        }

        [Fact]
        public async Task WhenNotLoggedIn_CallsLogin_ThenProceeds()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var version = "1.2.3";
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

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock.Setup(x => x.RunAsync(It.IsAny<string[]>())).ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.PinAddAndSaveAsync(packageId, version);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            _passthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args =>
                args[0] == "pin" && args[1] == "add" && args.Contains(packageId))), Times.Once);
        }

        [Fact]
        public async Task WhenExistingPackageHasGatingPinType_PassesGating()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var version = "1.7.*";
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
                new GistGetPackage { Id = packageId, PinType = "gating" }
            };

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "add" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains(version) &&
                    args.Contains("--force") &&
                    args.Contains("--gating"))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.PinAddAndSaveAsync(packageId, version, force: true);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _passthroughRunnerMock.VerifyAll();
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Count == 1 &&
                    list.Any(p =>
                        p.Id == packageId &&
                        p.Pin == version &&
                        p.PinType == "gating")
                )), Times.Once);
        }
    }

    public class PinRemoveAndSaveAsync : GistGetServiceTests
    {
        [Fact]
        public async Task WhenPinRemoveSucceeds_RemovesPinFromGist()
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
                new GistGetPackage { Id = packageId, Pin = "1.0.0", PinType = "blocking", Silent = true, Scope = "user" }
            };

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "remove" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.PinRemoveAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Count == 1 &&
                    list.Any(p =>
                        p.Id == packageId &&
                        p.Pin == null &&
                        p.PinType == null &&
                        p.Version == null &&
                        p.Silent &&
                        p.Scope == "user")
                )), Times.Once);
        }

        [Fact]
        public async Task WhenPinRemoveFails_DoesNotSaveToGist()
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
                new GistGetPackage { Id = packageId, Pin = "1.0.0" }
            };

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "remove" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(1);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.PinRemoveAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()
            ), Times.Never);
        }

        [Fact]
        public async Task WhenNoExistingPackage_CreatesNewEntryWithoutPin()
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

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "remove" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.PinRemoveAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Count == 1 &&
                    list.Any(p =>
                        p.Id == packageId &&
                        p.Pin == null &&
                        p.PinType == null &&
                        p.Version == null)
                )), Times.Once);
        }

        [Fact]
        public async Task WhenNotLoggedIn_CallsLogin_ThenProceeds()
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

            _credentialServiceMock.Setup(x => x.SaveCredential(It.IsAny<Credential>()))
                .Callback<Credential>((c) => currentCredential = c);

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _passthroughRunnerMock.Setup(x => x.RunAsync(It.IsAny<string[]>())).ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.PinRemoveAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            _passthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args =>
                args[0] == "pin" && args[1] == "remove" && args.Contains(packageId))), Times.Once);
        }

        [Fact]
        public async Task WhenPackageHasUninstallTrue_PreservesUninstallFlag()
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
                new GistGetPackage { Id = packageId, Pin = "1.0.0", Uninstall = true }
            };

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "remove" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.PinRemoveAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Count == 1 &&
                    list.Any(p =>
                        p.Id == packageId &&
                        p.Pin == null &&
                        p.PinType == null &&
                        p.Uninstall == true)
                )), Times.Once);
        }
    }

    public class SyncAsync : GistGetServiceTests
    {
        [Fact]
        public async Task WhenGistHasPackage_AndNotInstalled_InstallsPackage()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "New.Package";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Silent = true }
            };

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--silent"))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.Installed.Count.ShouldBe(1);
            result.Installed[0].Id.ShouldBe(packageId);
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenInstallingPackage_WritesProgressLog()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "New.Package";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId }
            };

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s =>
                s.Contains("install", StringComparison.OrdinalIgnoreCase) &&
                s.Contains(packageId))), Times.AtLeastOnce);
        }

        [Fact]
        public async Task WhenGistHasUninstallTrue_AndInstalled_UninstallsPackage()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Old.Package";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Uninstall = true }
            };

            var localPackages = new List<WinGetPackage>
            {
                new WinGetPackage("Old Package", new PackageId(packageId), new Version("1.0.0"), null)
            };

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(localPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" && args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "remove")))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.Uninstalled.Count.ShouldBe(1);
            result.Uninstalled[0].Id.ShouldBe(packageId);
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenUninstallingPackage_WritesProgressLog()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Old.Package";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Uninstall = true }
            };

            var localPackages = new List<WinGetPackage>
            {
                new WinGetPackage("Old Package", new PackageId(packageId), new Version("1.0.0"), null)
            };

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(localPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" && args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "remove")))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s =>
                s.Contains("uninstall", StringComparison.OrdinalIgnoreCase) &&
                s.Contains(packageId))), Times.AtLeastOnce);
        }

        [Fact]
        public async Task WhenGistHasPin_AndLocalHasNoPin_AddsPinLocally()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Existing.Package";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "1.5.0", PinType = "blocking" }
            };

            var localPackages = new List<WinGetPackage>
            {
                new WinGetPackage("Existing Package", new PackageId(packageId), new Version("1.5.0"), null)
            };

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(localPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "add" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains("1.5.0") &&
                    args.Contains("--blocking") && args.Contains("--force"))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.PinUpdated.Count.ShouldBe(1);
            result.PinUpdated[0].Id.ShouldBe(packageId);
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenSyncingPin_WritesProgressLog()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Existing.Package";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "1.5.0", PinType = "blocking" }
            };

            var localPackages = new List<WinGetPackage>
            {
                new WinGetPackage("Existing Package", new PackageId(packageId), new Version("1.5.0"), null)
            };

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(localPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "add" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--version") && args.Contains("1.5.0") &&
                    args.Contains("--blocking") && args.Contains("--force"))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await _target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s =>
                s.Contains("pin", StringComparison.OrdinalIgnoreCase) &&
                s.Contains(packageId))), Times.AtLeastOnce);
        }

        [Fact]
        public async Task WhenGistHasNoPin_AndLocalPackageExists_RemovesPinLocally()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Existing.Package";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId } // No pin
            };

            var localPackages = new List<WinGetPackage>
            {
                new WinGetPackage("Existing Package", new PackageId(packageId), new Version("1.5.0"), null)
            };

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(localPackages);

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "remove" && args.Contains("--id") && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.PinRemoved.Count.ShouldBe(1);
            result.PinRemoved[0].Id.ShouldBe(packageId);
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenInstallFails_ContinuesWithOtherPackages_AndReportsError()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var failingPackageId = "Failing.Package";
            var successPackageId = "Success.Package";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = failingPackageId },
                new GistGetPackage { Id = successPackageId }
            };

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            // First package fails
            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" && args.Contains(failingPackageId))))
                .ReturnsAsync(1);

            // Second package succeeds
            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" && args.Contains(successPackageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.Failed.Count.ShouldBe(1);
            result.Failed[0].Id.ShouldBe(failingPackageId);
            result.Installed.Count.ShouldBe(1);
            result.Installed[0].Id.ShouldBe(successPackageId);
            result.Success.ShouldBeFalse();
            result.Errors.Count.ShouldBe(1);
        }

        [Fact]
        public async Task WhenNotLoggedIn_CallsLogin_ThenProceeds()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
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

            _authServiceMock
                .Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            _authServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WithLocalFile_LoadsPackagesWithoutRemoteAccess()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Local.Package";
            var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.yaml");
            try
            {
                var yaml = GistGetPackageSerializer.Serialize(new List<GistGetPackage>
                {
                    new GistGetPackage { Id = packageId, Silent = true }
                });
                await File.WriteAllTextAsync(tempFile, yaml);

                _winGetServiceMock
                    .Setup(x => x.GetAllInstalledPackages())
                    .Returns(new List<WinGetPackage>());

                _passthroughRunnerMock
                    .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                        args[0] == "install" &&
                        args.Contains("--id") && args.Contains(packageId) &&
                        args.Contains("--silent"))))
                    .ReturnsAsync(0);

                // -------------------------------------------------------------------
                // Act
                // -------------------------------------------------------------------
                var result = await _target.SyncAsync(filePath: tempFile);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                result.Success.ShouldBeTrue();
                result.Installed.Count.ShouldBe(1);
                result.Installed[0].Id.ShouldBe(packageId);

                _authServiceMock.Verify(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
                _authServiceMock.Verify(x => x.GetPackagesFromUrlAsync(It.IsAny<string>()), Times.Never);
                _credentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Never);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task WithLocalFileMissing_ThrowsFileNotFound()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.yaml");

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            await Should.ThrowAsync<FileNotFoundException>(() => _target.SyncAsync(filePath: missingPath));

            _authServiceMock.Verify(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _authServiceMock.Verify(x => x.GetPackagesFromUrlAsync(It.IsAny<string>()), Times.Never);
            _credentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Never);
        }

        [Fact]
        public async Task WithExternalUrl_FetchesFromUrl_AndInstallsPackages()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var url = "https://example.com/raw/packages.yaml";
            var packageId = "External.Package";
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Silent = true }
            };

            // URL からパッケージ取得をモック
            _authServiceMock
                .Setup(x => x.GetPackagesFromUrlAsync(url))
                .ReturnsAsync(gistPackages);

            // ローカルにはインストールされていない
            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            _passthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id") && args.Contains(packageId) &&
                    args.Contains("--silent"))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.SyncAsync(url);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.Installed.Count.ShouldBe(1);
            result.Installed[0].Id.ShouldBe(packageId);
            result.Success.ShouldBeTrue();

            // URL 指定時は認証が不要であることを確認
            _credentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Never);
            _authServiceMock.Verify(x => x.LoginAsync(), Times.Never);
            _authServiceMock.Verify(x => x.GetPackagesFromUrlAsync(url), Times.Once);
        }

        [Fact]
        public async Task WithExternalUrl_DoesNotRequireAuthentication()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var url = "https://raw.githubusercontent.com/user/repo/packages.yaml";

            _authServiceMock
                .Setup(x => x.GetPackagesFromUrlAsync(url))
                .ReturnsAsync(new List<GistGetPackage>());

            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.SyncAsync(url);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.Success.ShouldBeTrue();
            // 認証関連のメソッドが呼ばれないことを確認
            _credentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Never);
            _authServiceMock.Verify(x => x.LoginAsync(), Times.Never);
        }
    }

    public class ExportAsync : GistGetServiceTests
    {
        [Fact]
        public async Task ExportsInstalledPackageIds()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var installedPackages = new List<WinGetPackage>
            {
                new WinGetPackage("Package A", new PackageId("Test.PackageA"), new Version("1.0.0"), null),
                new WinGetPackage("Package B", new PackageId("Test.PackageB"), new Version("2.0.0"), null)
            };

            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(installedPackages);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.ExportAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldContain("Test.PackageA");
            result.ShouldContain("Test.PackageB");
        }

        [Fact]
        public async Task WithOutputPath_WritesToFile()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var tempFile = Path.GetTempFileName();
            try
            {
                var installedPackages = new List<WinGetPackage>
                {
                    new WinGetPackage("Package A", new PackageId("Test.PackageA"), new Version("1.0.0"), null)
                };

                _winGetServiceMock
                    .Setup(x => x.GetAllInstalledPackages())
                    .Returns(installedPackages);

                // -------------------------------------------------------------------
                // Act
                // -------------------------------------------------------------------
                await _target.ExportAsync(tempFile);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                var content = await File.ReadAllTextAsync(tempFile);
                content.ShouldContain("Test.PackageA");
                _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("Exported") && s.Contains(tempFile))), Times.Once);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task DoesNotRequireAuthentication()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var installedPackages = new List<WinGetPackage>
            {
                new WinGetPackage("Package A", new PackageId("Test.PackageA"), new Version("1.0.0"), null)
            };

            _winGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(installedPackages);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await _target.ExportAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldContain("Test.PackageA");
            // No authentication calls should be made
            _credentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Never);
            _authServiceMock.Verify(x => x.GetPackagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    public class ImportAsync : GistGetServiceTests
    {
        [Fact]
        public async Task WhenAuthenticated_SavesPackagesToGist()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credential = new Credential("user", "token");
            var tempFile = Path.GetTempFileName();
            try
            {
                var yamlContent = @"Test.PackageA:
  pin: ""1.0.0""
  silent: true
Test.PackageB:
  scope: user
";
                await File.WriteAllTextAsync(tempFile, yamlContent);

                _credentialServiceMock
                    .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                    .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                    {
                        c = credential;
                        return true;
                    }));

                _authServiceMock
                    .Setup(x => x.SavePackagesAsync(
                        credential.Token,
                        "",
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.Is<IReadOnlyList<GistGetPackage>>(list =>
                            list.Count == 2 &&
                            list.Any(p => p.Id == "Test.PackageA" && p.Pin == "1.0.0" && p.Silent) &&
                            list.Any(p => p.Id == "Test.PackageB" && p.Scope == "user"))))
                    .Returns(Task.CompletedTask);

                // -------------------------------------------------------------------
                // Act
                // -------------------------------------------------------------------
                await _target.ImportAsync(tempFile);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                _authServiceMock.Verify(x => x.SavePackagesAsync(
                    credential.Token,
                    "",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Once);

                _consoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("Imported") && s.Contains("2"))), Times.Once);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task WhenNotAuthenticated_PerformsLogin_ThenSaves()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var savedCredential = new Credential("user", "token");
            Credential? currentCredential = null;
            var tempFile = Path.GetTempFileName();
            try
            {
                var yamlContent = @"Test.PackageA: {}
";
                await File.WriteAllTextAsync(tempFile, yamlContent);

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

                _authServiceMock
                    .Setup(x => x.SavePackagesAsync(
                        It.IsAny<string>(),
                        "",
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<IReadOnlyList<GistGetPackage>>()))
                    .Returns(Task.CompletedTask);

                // -------------------------------------------------------------------
                // Act
                // -------------------------------------------------------------------
                await _target.ImportAsync(tempFile);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                _authServiceMock.Verify(x => x.LoginAsync(), Times.Once);
                _authServiceMock.Verify(x => x.SavePackagesAsync(
                    It.IsAny<string>(),
                    "",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Once);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task WhenFileNotFound_ThrowsFileNotFoundException()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credential = new Credential("user", "token");
            var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".yaml");

            _credentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            await Should.ThrowAsync<FileNotFoundException>(async () =>
            {
                await _target.ImportAsync(nonExistentFile);
            });
        }

        [Fact]
        public async Task OverwritesExistingGistContent()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credential = new Credential("user", "token");
            var tempFile = Path.GetTempFileName();
            try
            {
                // Import only one package (simulating complete replacement)
                var yamlContent = @"New.Package:
  pin: ""3.0.0""
";
                await File.WriteAllTextAsync(tempFile, yamlContent);

                _credentialServiceMock
                    .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                    .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                    {
                        c = credential;
                        return true;
                    }));

                _authServiceMock
                    .Setup(x => x.SavePackagesAsync(
                        credential.Token,
                        "",
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.Is<IReadOnlyList<GistGetPackage>>(list =>
                            list.Count == 1 &&
                            list.Any(p => p.Id == "New.Package" && p.Pin == "3.0.0"))))
                    .Returns(Task.CompletedTask);

                // -------------------------------------------------------------------
                // Act
                // -------------------------------------------------------------------
                await _target.ImportAsync(tempFile);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                // Verify that SavePackagesAsync was called with only the imported content
                // (no merge with existing Gist content)
                _authServiceMock.Verify(x => x.GetPackagesAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

                _authServiceMock.Verify(x => x.SavePackagesAsync(
                    credential.Token,
                    "",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<IReadOnlyList<GistGetPackage>>(list => list.Count == 1)), Times.Once);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
