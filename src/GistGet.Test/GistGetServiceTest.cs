using System.Net;
using GistGet.Infrastructure;
using Moq;
using Octokit;
using Shouldly;

namespace GistGet.Test;

public class GistGetServiceTests
{
    protected readonly Mock<IGitHubService> AuthServiceMock;
    protected readonly Mock<IConsoleService> ConsoleServiceMock;
    protected readonly Mock<ICredentialService> CredentialServiceMock;
    protected readonly Mock<IWinGetPassthroughRunner> PassthroughRunnerMock;
    protected readonly Mock<IWinGetService> WinGetServiceMock;
    protected readonly IWinGetArgumentBuilder ArgumentBuilder; // Use real instance
    protected readonly GistGetService Target;

    protected delegate bool TryGetCredentialDelegate(out Credential? credential);

    public GistGetServiceTests()
    {
        AuthServiceMock = new Mock<IGitHubService>();
        ConsoleServiceMock = new Mock<IConsoleService>();
        CredentialServiceMock = new Mock<ICredentialService>();
        PassthroughRunnerMock = new Mock<IWinGetPassthroughRunner>();
        WinGetServiceMock = new Mock<IWinGetService>();
        ArgumentBuilder = new WinGetArgumentBuilder(); // Real instance
        Target = new GistGetService(
            AuthServiceMock.Object,
            ConsoleServiceMock.Object,
            CredentialServiceMock.Object,
            PassthroughRunnerMock.Object,
            WinGetServiceMock.Object,
            ArgumentBuilder);
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
            AuthServiceMock.Setup(x => x.LoginAsync()).ReturnsAsync(credential);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.AuthLoginAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            CredentialServiceMock.Verify(x => x.SaveCredential(credential), Times.Once);
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
            CredentialServiceMock.Setup(x => x.DeleteCredential()).Returns(true);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            Target.AuthLogout();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            CredentialServiceMock.Verify(x => x.DeleteCredential(), Times.Once);
            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("Logged out") || s.Contains("Log out"))), Times.Once);
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
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = null;
                    return false;
                }));

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.AuthStatusAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("not logged in"))), Times.Once);
        }

        [Fact]
        public async Task WhenAuthenticated_PrintsStatus()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credential = new Credential("testuser", "gho_1234567890");
            var scopes = new List<string> { "gist", "read:org" };

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetTokenStatusAsync(credential.Token))
                .ReturnsAsync(new TokenStatus("testuser", scopes));

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.AuthStatusAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            ConsoleServiceMock.Verify(x => x.WriteInfo("github.com"), Times.Once);
            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("✓ Logged in to github.com account testuser (keyring)"))), Times.Once);
            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("- Active account: true"))), Times.Once);
            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("- Git operations protocol: https"))), Times.Once);
            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("- Token: gho_**********"))), Times.Once);
            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("- Token scopes: 'gist', 'read:org'"))), Times.Once);
        }

        [Fact]
        public async Task WhenAuthenticated_WithNonGhoToken_PrintsMaskedPrefix()
        {
            var credential = new Credential("testuser", "abcd1234");
            var scopes = new List<string> { "gist" };

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetTokenStatusAsync(credential.Token))
                .ReturnsAsync(new TokenStatus("testuser", scopes));

            await Target.AuthStatusAsync();

            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("- Token: abcd**********"))), Times.Once);
        }

        [Fact]
        public async Task WhenApiExceptionThrown_LogsError()
        {
            var credential = new Credential("testuser", "gho_1234567890");

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetTokenStatusAsync(credential.Token))
                .ThrowsAsync(new ApiException("api error", HttpStatusCode.BadRequest));

            await Target.AuthStatusAsync();

            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s =>
                s.Contains("Failed to retrieve status from GitHub", StringComparison.OrdinalIgnoreCase) &&
                s.Contains("api error", StringComparison.OrdinalIgnoreCase))), Times.Once);
        }

        [Fact]
        public async Task WhenStatusLookupFails_LogsError()
        {
            var credential = new Credential("testuser", "gho_1234567890");

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetTokenStatusAsync(credential.Token))
                .ThrowsAsync(new HttpRequestException("network error"));

            await Target.AuthStatusAsync();

            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s =>
                s.Contains("Failed to retrieve status from GitHub", StringComparison.OrdinalIgnoreCase) &&
                s.Contains("network error", StringComparison.OrdinalIgnoreCase))), Times.Once);
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

            PassthroughRunnerMock.Setup(x => x.RunAsync(It.IsAny<string[]>()))
                .ReturnsAsync(expectedExitCode);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.RunPassthroughAsync(command, args);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe(expectedExitCode);
            PassthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(a => a.SequenceEqual(expectedArgs, StringComparer.Ordinal))
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = currentCredential;
                    return c != null;
                }));

            AuthServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(savedCredential);

            CredentialServiceMock.Setup(x => x.SaveCredential(It.IsAny<Credential>()))
                .Callback<Credential>((c) => currentCredential = c);

            // Mock passthrough to succeed so it doesn't fail later
            PassthroughRunnerMock.Setup(x => x.RunAsync(It.IsAny<string[]>())).ReturnsAsync(0);

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.InstallAndSaveAsync(options);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            PassthroughRunnerMock.Verify(x => x.RunAsync(It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task WhenCredentialsUnavailableAfterLogin_Throws()
        {
            var options = new InstallOptions { Id = "Test.Package" };
            var savedCredential = new Credential("user", "token");
            var credentialAttempts = 0;

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = null;
                    credentialAttempts++;
                    return false;
                }));

            AuthServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(savedCredential);

            await Should.ThrowAsync<InvalidOperationException>(async () => await Target.InstallAndSaveAsync(options));

            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            CredentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Exactly(2));
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
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            // Gist setup
            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            // Runner setup
            // Expect Install command with version 1.2.3 (from Pin)
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains("1.2.3", StringComparer.Ordinal) &&
                    args.Contains("--silent", StringComparer.Ordinal))))
                .ReturnsAsync(0);

            // Expect Pin Add command (restore pin)
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "add" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains("1.2.3", StringComparer.Ordinal) &&
                    args.Contains("--blocking", StringComparer.Ordinal))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.InstallAndSaveAsync(installOptions);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            PassthroughRunnerMock.VerifyAll();
            // Verify SavePackagesAsync called with updated list (Pin/PinType preserved)
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            // Gist setup
            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            // Runner setup
            // Expect Install command with explicit version 2.0.0
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains(explicitVersion))))
                .ReturnsAsync(0);

            // Expect Pin Add command (update pin to 2.0.0)
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "add" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains(explicitVersion))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.InstallAndSaveAsync(installOptions);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            PassthroughRunnerMock.VerifyAll();
            // Verify SavePackagesAsync called with updated Pin
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            // Gist setup: no existing entry -> no pin
            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            // Runner setup: only install should be called
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains(explicitVersion))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.InstallAndSaveAsync(installOptions);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            PassthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args => args[0] == "pin")), Times.Never);
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--ignore-security-hash", StringComparer.Ordinal) &&
                    args.Contains("--skip-dependencies", StringComparer.Ordinal) &&
                    args.Contains("--installer-type", StringComparer.Ordinal) && args.Contains("msi", StringComparer.Ordinal) &&
                    args.Contains("--locale", StringComparer.Ordinal) && args.Contains("ja-JP", StringComparer.Ordinal))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.InstallAndSaveAsync(installOptions);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            PassthroughRunnerMock.VerifyAll();
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
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--custom", StringComparer.Ordinal) && args.Contains(customArgs))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.InstallAndSaveAsync(installOptions);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            PassthroughRunnerMock.VerifyAll();
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
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(expectedExitCode);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.InstallAndSaveAsync(new InstallOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe(expectedExitCode);
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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
        public async Task WhenPinnedPackageNoVersionOption_FindsVersionFromWinGet()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            // This test covers the branch in UpgradeAndSaveAsync where hasPin=true
            // and resolvedVersion=null, triggering WinGetService.FindById call.
            var packageId = "Test.Package";
            var installedVersion = "3.0.0";
            var upgradeOptions = new UpgradeOptions { Id = packageId }; // Version is null

            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "2.0.0", PinType = "blocking" }
            };

            // Credential setup
            var credential = new Credential("user", "token");
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            // First call: upgrade command
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            // WinGetService returns the installed package with updated version
            WinGetServiceMock
                .Setup(x => x.FindById(It.Is<PackageId>(p => p.AsPrimitive() == packageId)))
                .Returns(new WinGetPackage(
                    Name: "Test Package",
                    Id: new PackageId(packageId),
                    Version: new Version(installedVersion),
                    UsableVersion: null
                ));

            // Expect pin add with resolved version from WinGet
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "add" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains(installedVersion))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.UpgradeAndSaveAsync(upgradeOptions);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            WinGetServiceMock.Verify(x => x.FindById(It.Is<PackageId>(p => p.AsPrimitive() == packageId)), Times.Once);
            PassthroughRunnerMock.VerifyAll();
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Any(p => p.Id == packageId && p.Pin == installedVersion && p.Version == installedVersion)
                )), Times.Once);
        }

        [Fact]
        public async Task WhenPinnedPackageNoVersionOption_AndWinGetReturnsNull_UsesFallbackPin()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            // This test covers the branch where hasPin=true, resolvedVersion=null,
            // and WinGetService.FindById returns null (package not found).
            var packageId = "Test.Package";
            var originalPin = "2.0.0";
            var upgradeOptions = new UpgradeOptions { Id = packageId }; // Version is null

            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = originalPin, PinType = "gating" }
            };

            // Credential setup
            var credential = new Credential("user", "token");
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            // First call: upgrade command
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            // WinGetService returns null (package not found after upgrade)
            WinGetServiceMock
                .Setup(x => x.FindById(It.Is<PackageId>(p => p.AsPrimitive() == packageId)))
                .Returns((WinGetPackage?)null);

            // Expect pin add with fallback to original pin
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "add" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains(originalPin))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.UpgradeAndSaveAsync(upgradeOptions);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            WinGetServiceMock.Verify(x => x.FindById(It.Is<PackageId>(p => p.AsPrimitive() == packageId)), Times.Once);
            PassthroughRunnerMock.VerifyAll();
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Any(p => p.Id == packageId && p.Pin == originalPin)
                )), Times.Once);
        }

        [Fact]
        public async Task WhenCredentialsUnavailableAfterLogin_Throws()
        {
            var options = new UpgradeOptions { Id = "Test.Package" };
            var savedCredential = new Credential("user", "token");
            var attempts = 0;

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = null;
                    attempts++;
                    return false;
                }));

            AuthServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(savedCredential);

            await Should.ThrowAsync<InvalidOperationException>(async () => await Target.UpgradeAndSaveAsync(options));

            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            CredentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Exactly(2));
        }

        [Fact]
        public async Task WhenFlagsProvided_SavesFlagsToGist()
        {
            var options = new UpgradeOptions
            {
                Id = "Test.Package",
                Force = true,
                AcceptPackageAgreements = true,
                AcceptSourceAgreements = true,
                AllowHashMismatch = true,
                SkipDependencies = true,
                InstallerType = "exe",
                Header = "Custom",
                Custom = "--foo"
            };

            var credential = new Credential("user", "token");
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.IsAny<string[]>()))
                .ReturnsAsync(0);

            List<GistGetPackage>? savedPackages = null;
            AuthServiceMock
                .Setup(x => x.SavePackagesAsync(
                    credential.Token,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<GistGetPackage>>()))
                .Callback<string, string, string, string, IReadOnlyList<GistGetPackage>>((_, _, _, _, list) =>
                {
                    savedPackages = list.ToList();
                })
                .Returns(Task.CompletedTask);

            await Target.UpgradeAndSaveAsync(options);

            savedPackages.ShouldNotBeNull();
            var pkg = savedPackages!.Single(p => p.Id == options.Id);
            pkg.Force.ShouldBeTrue();
            pkg.AcceptPackageAgreements.ShouldBeTrue();
            pkg.AcceptSourceAgreements.ShouldBeTrue();
            pkg.AllowHashMismatch.ShouldBeTrue();
            pkg.SkipDependencies.ShouldBeTrue();
            pkg.InstallerType.ShouldBe("exe");
            pkg.Header.ShouldBe("Custom");
            pkg.Custom.ShouldBe("--foo");
        }

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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            WinGetServiceMock
                .Setup(x => x.FindById(It.Is<PackageId>(p => p.AsPrimitive() == packageId)))
                .Returns(new WinGetPackage("Test Package", new PackageId(packageId), new Version("1.0.0"), new Version("2.0.0")));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            var sequence = new MockSequence();

            PassthroughRunnerMock
                .InSequence(sequence)
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            PassthroughRunnerMock
                .InSequence(sequence)
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "add" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains("2.0.0", StringComparer.Ordinal) &&
                    args.Contains("--blocking", StringComparer.Ordinal) &&
                    args.Contains("--force", StringComparer.Ordinal))))
                .ReturnsAsync(0);

            AuthServiceMock
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
            await Target.UpgradeAndSaveAsync(new UpgradeOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            WinGetServiceMock.Verify(x => x.FindById(It.Is<PackageId>(p => p.AsPrimitive() == packageId)), Times.Once);
            PassthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args => args[0] == "upgrade" && args.Contains(packageId))), Times.Once);
            PassthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args => args[0] == "pin" && args[1] == "add")), Times.Once);
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.UpgradeAndSaveAsync(new UpgradeOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            PassthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args => args[0] == "pin")), Times.Never);
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Never);
            WinGetServiceMock.Verify(x => x.FindById(It.IsAny<PackageId>()), Times.Never);
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains(version))))
                .ReturnsAsync(0);

            AuthServiceMock
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
            await Target.UpgradeAndSaveAsync(new UpgradeOptions { Id = packageId, Version = version });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            PassthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args => args[0] == "pin")), Times.Never);
            WinGetServiceMock.Verify(x => x.FindById(It.IsAny<PackageId>()), Times.Never);
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(1);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.UpgradeAndSaveAsync(new UpgradeOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            PassthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args => args[0] == "pin")), Times.Never);
            AuthServiceMock.Verify(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = currentCredential;
                    return c != null;
                }));

            AuthServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(savedCredential);

            CredentialServiceMock.Setup(x => x.SaveCredential(It.IsAny<Credential>()))
                .Callback<Credential>((c) => currentCredential = c);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.UpgradeAndSaveAsync(new UpgradeOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            PassthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args =>
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
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "upgrade" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(expectedExitCode);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.UpgradeAndSaveAsync(new UpgradeOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe(expectedExitCode);
            AuthServiceMock.Verify(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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
        public async Task WhenCredentialsUnavailableAfterLogin_Throws()
        {
            var options = new UninstallOptions { Id = "Test.Package" };
            var savedCredential = new Credential("user", "token");
            var attempts = 0;

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = null;
                    attempts++;
                    return false;
                }));

            AuthServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(savedCredential);

            await Should.ThrowAsync<InvalidOperationException>(async () => await Target.UninstallAndSaveAsync(options));

            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            CredentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Exactly(2));
        }

        [Fact]
        public async Task WhenNotLoggedIn_PerformsLogin_AndSavesUninstallEntry()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var savedCredential = new Credential("user", "token");
            Credential? currentCredential = null;

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = currentCredential;
                    return c != null;
                }));

            AuthServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(savedCredential);

            CredentialServiceMock
                .Setup(x => x.SaveCredential(It.IsAny<Credential>()))
                .Callback<Credential>(c => currentCredential = c);

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(savedCredential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            // ローカルにインストール済み
            WinGetServiceMock
                .Setup(x => x.FindById(It.Is<PackageId>(id => id.AsPrimitive() == packageId)))
                .Returns(new WinGetPackage(packageId, new PackageId(packageId), new Version("1.0.0"), null));

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            AuthServiceMock
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
            await Target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            PassthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "uninstall" && args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))
            ), Times.Once);
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            // ローカルにインストール済み
            WinGetServiceMock
                .Setup(x => x.FindById(It.Is<PackageId>(id => id.AsPrimitive() == packageId)))
                .Returns(new WinGetPackage(packageId, new PackageId(packageId), new Version("1.0.0"), null));

            var sequence = new MockSequence();

            PassthroughRunnerMock
                .InSequence(sequence)
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            PassthroughRunnerMock
                .InSequence(sequence)
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "remove" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            AuthServiceMock
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
            await Target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            PassthroughRunnerMock.VerifyAll();
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "1.2.3" }
            };

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            // ローカルにインストール済み
            WinGetServiceMock
                .Setup(x => x.FindById(It.Is<PackageId>(id => id.AsPrimitive() == packageId)))
                .Returns(new WinGetPackage(packageId, new PackageId(packageId), new Version("1.0.0"), null));

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(1);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            PassthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "pin" && args.Contains("remove", StringComparer.Ordinal))
            ), Times.Never);
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            // ローカルにインストール済み
            WinGetServiceMock
                .Setup(x => x.FindById(It.Is<PackageId>(id => id.AsPrimitive() == packageId)))
                .Returns(new WinGetPackage(packageId, new PackageId(packageId), new Version("1.0.0"), null));

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(expectedExitCode);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe(expectedExitCode);
            PassthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "pin" && args.Contains("remove", StringComparer.Ordinal))
            ), Times.Never);
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            // Gist に該当パッケージのエントリがない
            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            // ローカルにインストール済み
            WinGetServiceMock
                .Setup(x => x.FindById(It.Is<PackageId>(id => id.AsPrimitive() == packageId)))
                .Returns(new WinGetPackage(packageId, new PackageId(packageId), new Version("1.0.0"), null));

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            // pin remove は常に呼ばれる（エラーは無視）
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "remove" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            // ローカルのpinを確実に削除するため、常に pin remove が呼ばれる
            PassthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "pin" && args.Contains("remove", StringComparer.Ordinal))
            ), Times.Once);

            // 新規エントリが作成され、uninstall: true で保存される
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            // Gist にエントリはあるが Pin は設定されていない
            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Silent = true, Scope = "user" }
            };

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            // ローカルにインストール済み
            WinGetServiceMock
                .Setup(x => x.FindById(It.Is<PackageId>(id => id.AsPrimitive() == packageId)))
                .Returns(new WinGetPackage(packageId, new PackageId(packageId), new Version("1.0.0"), null));

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            // pin remove は常に呼ばれる（ローカルにpinがある可能性があるため）
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "remove" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            // Gist側にPinがなくても、ローカルのpinを確実に削除するため pin remove が呼ばれる
            PassthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "pin" && args.Contains("remove", StringComparer.Ordinal))
            ), Times.Once);

            // 既存のプロパティが保持され、uninstall: true で保存される
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Any(p => p.Id == packageId && p.Uninstall && p.Silent && p.Scope == "user")
                )), Times.Once);
        }

        [Fact]
        public async Task WhenPackageNotInstalledLocally_SkipsWinGetUninstallAndUpdatesGist()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "NotInstalled.Package";
            var credential = new Credential("user", "token");

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            // Gist にはエントリが存在する（インストール状態）
            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Silent = true }
            };

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            // ローカルには未インストール
            WinGetServiceMock
                .Setup(x => x.FindById(It.Is<PackageId>(id => id.AsPrimitive() == packageId)))
                .Returns((WinGetPackage?)null);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.UninstallAndSaveAsync(new UninstallOptions { Id = packageId });

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            // ローカルに未インストールなので、winget uninstall は呼ばれない
            PassthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "uninstall")
            ), Times.Never);

            // pin remove も呼ばれない（ローカルにパッケージがないため）
            PassthroughRunnerMock.Verify(x => x.RunAsync(
                It.Is<string[]>(args => args[0] == "pin" && args.Contains("remove", StringComparer.Ordinal))
            ), Times.Never);

            // Gist には uninstall: true で保存される
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<GistGetPackage>>(list =>
                    list.Count == 1 &&
                    list.Any(p => p.Id == packageId && p.Uninstall && p.Silent)
                )), Times.Once);

            // 正常終了
            result.ShouldBe(0);
        }
    }

    public class PinAddAndSaveAsync : GistGetServiceTests
    {
        [Fact]
        public async Task WhenCredentialsUnavailableAfterLogin_Throws()
        {
            var packageId = "Pkg";
            var credential = new Credential("user", "token");
            var attempts = 0;

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = null;
                    attempts++;
                    return false;
                }));

            AuthServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(credential);

            await Should.ThrowAsync<InvalidOperationException>(async () => await Target.PinAddAndSaveAsync(packageId, "1.0.0"));

            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            CredentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Exactly(2));
        }

        [Fact]
        public async Task WhenPinSucceeds_SavesPinToGist()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var version = "1.2.3";
            var credential = new Credential("user", "token");

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "add" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains(version) &&
                    !args.Contains("--force", StringComparer.Ordinal))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.PinAddAndSaveAsync(packageId, version);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "add" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains(version))))
                .ReturnsAsync(1);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.PinAddAndSaveAsync(packageId, version);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Uninstall = true, PinType = "blocking", Silent = true, Scope = "user" }
            };

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "add" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains(version) &&
                    args.Contains("--force", StringComparer.Ordinal) &&
                    args.Contains("--blocking", StringComparer.Ordinal))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.PinAddAndSaveAsync(packageId, version, force: true);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = currentCredential;
                    return c != null;
                }));

            AuthServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(savedCredential);

            CredentialServiceMock.Setup(x => x.SaveCredential(It.IsAny<Credential>()))
                .Callback<Credential>((c) => currentCredential = c);

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            PassthroughRunnerMock.Setup(x => x.RunAsync(It.IsAny<string[]>())).ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.PinAddAndSaveAsync(packageId, version);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            PassthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args =>
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, PinType = "gating" }
            };

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "add" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains(version) &&
                    args.Contains("--force", StringComparer.Ordinal) &&
                    args.Contains("--gating", StringComparer.Ordinal))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.PinAddAndSaveAsync(packageId, version, force: true);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            PassthroughRunnerMock.VerifyAll();
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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
        public async Task WhenCredentialsUnavailableAfterLogin_Throws()
        {
            var packageId = "Pkg";
            var credential = new Credential("user", "token");
            var attempts = 0;

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = null;
                    attempts++;
                    return false;
                }));

            AuthServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(credential);

            await Should.ThrowAsync<InvalidOperationException>(async () => await Target.PinRemoveAndSaveAsync(packageId));

            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            CredentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Exactly(2));
        }

        [Fact]
        public async Task WhenPinRemoveSucceeds_RemovesPinFromGist()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = "Test.Package";
            var credential = new Credential("user", "token");

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "1.0.0", PinType = "blocking", Silent = true, Scope = "user" }
            };

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "remove" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.PinRemoveAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "1.0.0" }
            };

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "remove" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(1);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.PinRemoveAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "remove" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.PinRemoveAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = currentCredential;
                    return c != null;
                }));

            AuthServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(savedCredential);

            CredentialServiceMock.Setup(x => x.SaveCredential(It.IsAny<Credential>()))
                .Callback<Credential>((c) => currentCredential = c);

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            PassthroughRunnerMock.Setup(x => x.RunAsync(It.IsAny<string[]>())).ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.PinRemoveAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
            PassthroughRunnerMock.Verify(x => x.RunAsync(It.Is<string[]>(args =>
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            var existingPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "1.0.0", Uninstall = true }
            };

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingPackages);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args.Length >= 2 &&
                    args[0] == "pin" &&
                    args[1] == "remove" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.PinRemoveAndSaveAsync(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--silent", StringComparer.Ordinal))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.SyncAsync();

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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s =>
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(localPackages);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" && args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "remove")))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.SyncAsync();

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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(localPackages);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "uninstall" && args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "remove")))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s =>
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(localPackages);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "add" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains("1.5.0", StringComparer.Ordinal) &&
                    args.Contains("--blocking", StringComparer.Ordinal) && args.Contains("--force", StringComparer.Ordinal))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.SyncAsync();

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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(localPackages);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "add" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--version", StringComparer.Ordinal) && args.Contains("1.5.0", StringComparer.Ordinal) &&
                    args.Contains("--blocking", StringComparer.Ordinal) && args.Contains("--force", StringComparer.Ordinal))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s =>
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(localPackages);

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "pin" && args[1] == "remove" && args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.SyncAsync();

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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            // First package fails
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" && args.Contains(failingPackageId))))
                .ReturnsAsync(1);

            // Second package succeeds
            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" && args.Contains(successPackageId))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.SyncAsync();

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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = currentCredential;
                    return c != null;
                }));

            AuthServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(savedCredential);

            CredentialServiceMock.Setup(x => x.SaveCredential(It.IsAny<Credential>()))
                .Callback<Credential>((c) => currentCredential = c);

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<GistGetPackage>());

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.SyncAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
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

                WinGetServiceMock
                    .Setup(x => x.GetAllInstalledPackages())
                    .Returns(new List<WinGetPackage>());

                PassthroughRunnerMock
                    .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                        args[0] == "install" &&
                        args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                        args.Contains("--silent", StringComparer.Ordinal))))
                    .ReturnsAsync(0);

                // -------------------------------------------------------------------
                // Act
                // -------------------------------------------------------------------
                var result = await Target.SyncAsync(filePath: tempFile);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                result.Success.ShouldBeTrue();
                result.Installed.Count.ShouldBe(1);
                result.Installed[0].Id.ShouldBe(packageId);

                AuthServiceMock.Verify(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
                AuthServiceMock.Verify(x => x.GetPackagesFromUrlAsync(It.IsAny<string>()), Times.Never);
                CredentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Never);
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
            await Should.ThrowAsync<FileNotFoundException>(() => Target.SyncAsync(filePath: missingPath));

            AuthServiceMock.Verify(x => x.GetPackagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            AuthServiceMock.Verify(x => x.GetPackagesFromUrlAsync(It.IsAny<string>()), Times.Never);
            CredentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Never);
        }

        [Fact]
        public async Task WithExternalUrl_FetchesFromUrl_AndInstallsPackages()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var url = "https://example.com/raw/GistGet.yaml";
            var packageId = "External.Package";
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Silent = true }
            };

            // URL からパッケージ取得をモック
            AuthServiceMock
                .Setup(x => x.GetPackagesFromUrlAsync(url))
                .ReturnsAsync(gistPackages);

            // ローカルにはインストールされていない
            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.Is<string[]>(args =>
                    args[0] == "install" &&
                    args.Contains("--id", StringComparer.Ordinal) && args.Contains(packageId) &&
                    args.Contains("--silent", StringComparer.Ordinal))))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.SyncAsync(url);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.Installed.Count.ShouldBe(1);
            result.Installed[0].Id.ShouldBe(packageId);
            result.Success.ShouldBeTrue();

            // URL 指定時は認証が不要であることを確認
            CredentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Never);
            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Never);
            AuthServiceMock.Verify(x => x.GetPackagesFromUrlAsync(url), Times.Once);
        }

        [Fact]
        public async Task WithExternalUrl_DoesNotRequireAuthentication()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var url = "https://raw.githubusercontent.com/user/repo/GistGet.yaml";

            AuthServiceMock
                .Setup(x => x.GetPackagesFromUrlAsync(url))
                .ReturnsAsync(new List<GistGetPackage>());

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.SyncAsync(url);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.Success.ShouldBeTrue();
            // 認証関連のメソッドが呼ばれないことを確認
            CredentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Never);
            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Never);
        }

        [Fact]
        public async Task WhenUninstallThrows_AddsFailedAndError()
        {
            var packageId = "Throw.Uninstall";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Uninstall = true }
            };

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage> { new("Throw Uninstall", new PackageId(packageId), new Version("1.0.0"), new Version("1.0.0")) });

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.IsAny<string[]>()))
                .ThrowsAsync(new InvalidOperationException("run failed"));

            var result = await Target.SyncAsync();

            result.Failed.ShouldContain(p => p.Id == packageId);
            result.Errors.ShouldContain(s => s.Contains("run failed", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task WhenInstallThrows_AddsFailedAndError()
        {
            var packageId = "Throw.Install";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId }
            };

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.IsAny<string[]>()))
                .ThrowsAsync(new InvalidOperationException("install failed"));

            var result = await Target.SyncAsync();

            result.Failed.ShouldContain(p => p.Id == packageId);
            result.Errors.ShouldContain(s => s.Contains("install failed", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task WhenPinSyncThrows_AddsError()
        {
            var packageId = "Throw.Pin";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage>
            {
                new GistGetPackage { Id = packageId, Pin = "1.0.0" }
            };

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage> { new("Throw Pin", new PackageId(packageId), new Version("1.0.0"), new Version("1.0.0")) });

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.IsAny<string[]>()))
                .ThrowsAsync(new InvalidOperationException("pin failed"));

            var result = await Target.SyncAsync();

            result.Errors.ShouldContain(s => s.Contains("pin failed", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task WhenUninstallExitCodeNonZero_AddsFailedAndError()
        {
            var packageId = "Exit.Uninstall";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage> { new() { Id = packageId, Uninstall = true } };
            var localPackages = new List<WinGetPackage> { new("Exit Uninstall", new PackageId(packageId), new Version("1.0.0"), null) };

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock.Setup(x => x.GetAllInstalledPackages()).Returns(localPackages);

            PassthroughRunnerMock
                .SetupSequence(x => x.RunAsync(It.IsAny<string[]>()))
                .ReturnsAsync(1); // uninstall fail

            var result = await Target.SyncAsync();

            result.Failed.ShouldContain(p => p.Id == packageId);
            result.Errors.ShouldContain(s => s.Contains("Failed to uninstall", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task WhenInstallExitCodeNonZero_AddsFailedAndError()
        {
            var packageId = "Exit.Install";
            var credential = new Credential("user", "token");
            var gistPackages = new List<GistGetPackage> { new() { Id = packageId } };

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            AuthServiceMock
                .Setup(x => x.GetPackagesAsync(credential.Token, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(gistPackages);

            WinGetServiceMock.Setup(x => x.GetAllInstalledPackages()).Returns(new List<WinGetPackage>());

            PassthroughRunnerMock
                .Setup(x => x.RunAsync(It.IsAny<string[]>()))
                .ReturnsAsync(2); // install fail

            var result = await Target.SyncAsync();

            result.Failed.ShouldContain(p => p.Id == packageId);
            result.Errors.ShouldContain(s => s.Contains("Failed to install", StringComparison.OrdinalIgnoreCase));
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

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(installedPackages);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.ExportAsync();

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

                WinGetServiceMock
                    .Setup(x => x.GetAllInstalledPackages())
                    .Returns(installedPackages);

                // -------------------------------------------------------------------
                // Act
                // -------------------------------------------------------------------
                await Target.ExportAsync(tempFile);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                var content = await File.ReadAllTextAsync(tempFile);
                content.ShouldContain("Test.PackageA");
                ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("Exported") && s.Contains(tempFile))), Times.Once);
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

            WinGetServiceMock
                .Setup(x => x.GetAllInstalledPackages())
                .Returns(installedPackages);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Target.ExportAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldContain("Test.PackageA");
            // No authentication calls should be made
            CredentialServiceMock.Verify(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny), Times.Never);
            AuthServiceMock.Verify(x => x.GetPackagesAsync(
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

                CredentialServiceMock
                    .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                    .Returns(new TryGetCredentialDelegate((out c) =>
                    {
                        c = credential;
                        return true;
                    }));

                AuthServiceMock
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
                await Target.ImportAsync(tempFile);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                AuthServiceMock.Verify(x => x.SavePackagesAsync(
                    credential.Token,
                    "",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Once);

                ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("Imported") && s.Contains("2"))), Times.Once);
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

                CredentialServiceMock
                    .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                    .Returns(new TryGetCredentialDelegate((out c) =>
                    {
                        c = currentCredential;
                        return c != null;
                    }));

                AuthServiceMock.Setup(x => x.LoginAsync())
                    .ReturnsAsync(savedCredential);

                CredentialServiceMock.Setup(x => x.SaveCredential(It.IsAny<Credential>()))
                    .Callback<Credential>((c) => currentCredential = c);

                AuthServiceMock
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
                await Target.ImportAsync(tempFile);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
                AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out c) =>
                {
                    c = credential;
                    return true;
                }));

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            await Should.ThrowAsync<FileNotFoundException>(async () =>
            {
                await Target.ImportAsync(nonExistentFile);
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

                CredentialServiceMock
                    .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                    .Returns(new TryGetCredentialDelegate((out c) =>
                    {
                        c = credential;
                        return true;
                    }));

                AuthServiceMock
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
                await Target.ImportAsync(tempFile);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                // Verify that SavePackagesAsync was called with only the imported content
                // (no merge with existing Gist content)
                AuthServiceMock.Verify(x => x.GetPackagesAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

                AuthServiceMock.Verify(x => x.SavePackagesAsync(
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

        [Fact]
        public async Task WhenLoginFails_ThrowsInvalidOperation()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var tempFile = Path.GetTempFileName();
            try
            {
                var yamlContent = @"Test.Package: {}
";
                await File.WriteAllTextAsync(tempFile, yamlContent);

                // 最初のTryGetCredentialは失敗、LoginAsync後も失敗
                CredentialServiceMock
                    .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                    .Returns(new TryGetCredentialDelegate((out c) =>
                    {
                        c = null;
                        return false;
                    }));

                var failedCredential = new Credential("user", "token");
                AuthServiceMock.Setup(x => x.LoginAsync())
                    .ReturnsAsync(failedCredential);

                // SaveCredentialは呼ばれるが、TryGetCredentialは引き続き失敗
                CredentialServiceMock.Setup(x => x.SaveCredential(It.IsAny<Credential>()));

                // -------------------------------------------------------------------
                // Act & Assert
                // -------------------------------------------------------------------
                await Should.ThrowAsync<InvalidOperationException>(async () =>
                {
                    await Target.ImportAsync(tempFile);
                });
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}

