using Moq;

namespace GistGet.Test;

public class GistGetServiceInitTests : GistGetServiceTests
{
    public new class InitAsync : GistGetServiceInitTests
    {
        [Fact]
        public async Task NotAuthenticated_TriggersLogin()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var savedCredential = new Credential("user", "token");
            Credential? currentCredential = null;

            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = currentCredential;
                    return c != null;
                }));

            AuthServiceMock.Setup(x => x.LoginAsync())
                .ReturnsAsync(savedCredential);

            CredentialServiceMock.Setup(x => x.SaveCredential(It.IsAny<Credential>()))
                .Callback<Credential>((c) => currentCredential = c);

            WinGetServiceMock.Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.InitAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.LoginAsync(), Times.Once);
        }

        [Fact]
        public async Task NoInstalledPackages_SavesEmptyList()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credential = new Credential("user", "token");
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            WinGetServiceMock.Setup(x => x.GetAllInstalledPackages())
                .Returns(new List<WinGetPackage>());

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.InitAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Never);
            ConsoleServiceMock.Verify(x => x.WriteInfo(It.Is<string>(s => s.Contains("cancelled") || s.Contains("キャンセル"))), Times.Once);
        }

        [Fact]
        public async Task UserConfirmsAll_SavesAllPackages()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credential = new Credential("user", "token");
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            var installedPackages = new List<WinGetPackage>
            {
                new WinGetPackage("Pkg1", new PackageId("Id1"), new Version("1.0.0"), null, "winget"),
                new WinGetPackage("Pkg2", new PackageId("Id2"), new Version("2.0.0"), null, "winget")
            };
            WinGetServiceMock.Setup(x => x.GetAllInstalledPackages()).Returns(installedPackages);

            ConsoleServiceMock.Setup(x => x.Confirm(It.Is<string>(s => s.Contains("Id1")), It.IsAny<bool>())).Returns(true);
            ConsoleServiceMock.Setup(x => x.Confirm(It.Is<string>(s => s.Contains("Id2")), It.IsAny<bool>())).Returns(true);
            ConsoleServiceMock.Setup(x => x.Confirm(It.Is<string>(s => s.Contains("2")), It.IsAny<bool>())).Returns(true); // Final confirm

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.InitAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription,
                It.Is<IReadOnlyList<GistGetPackage>>(l => l.Count == 2 && l.Any(p => p.Id == "Id1") && l.Any(p => p.Id == "Id2"))
            ), Times.Once);
        }

        [Fact]
        public async Task UserRejectsAll_SavesEmptyList()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credential = new Credential("user", "token");
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            var installedPackages = new List<WinGetPackage>
            {
                new WinGetPackage("Pkg1", new PackageId("Id1"), new Version("1.0.0"), null, "winget"),
                new WinGetPackage("Pkg2", new PackageId("Id2"), new Version("2.0.0"), null, "winget")
            };
            WinGetServiceMock.Setup(x => x.GetAllInstalledPackages()).Returns(installedPackages);

            ConsoleServiceMock.Setup(x => x.Confirm(It.IsAny<string>(), It.IsAny<bool>())).Returns(false);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.InitAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Never);
        }

        [Fact]
        public async Task UserSelectsPartial_SavesOnlySelectedPackages()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credential = new Credential("user", "token");
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            var installedPackages = new List<WinGetPackage>
            {
                new WinGetPackage("Pkg1", new PackageId("Id1"), new Version("1.0.0"), null, "winget"),
                new WinGetPackage("Pkg2", new PackageId("Id2"), new Version("2.0.0"), null, "winget")
            };
            WinGetServiceMock.Setup(x => x.GetAllInstalledPackages()).Returns(installedPackages);

            ConsoleServiceMock.Setup(x => x.Confirm(It.Is<string>(s => s.Contains("Id1")), It.IsAny<bool>())).Returns(true);
            ConsoleServiceMock.Setup(x => x.Confirm(It.Is<string>(s => s.Contains("Id2")), It.IsAny<bool>())).Returns(false);
            ConsoleServiceMock.Setup(x => x.Confirm(It.Is<string>(s => s.Contains("1")), It.IsAny<bool>())).Returns(true); // Final confirm

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.InitAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
                credential.Token,
                "",
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription,
                It.Is<IReadOnlyList<GistGetPackage>>(l => l.Count == 1 && l.All(p => p.Id == "Id1"))
            ), Times.Once);
        }

        [Fact]
        public async Task UserCancelsFinalConfirm_DoesNotSave()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credential = new Credential("user", "token");
            CredentialServiceMock
                .Setup(x => x.TryGetCredential(out It.Ref<Credential?>.IsAny))
                .Returns(new TryGetCredentialDelegate((out Credential? c) =>
                {
                    c = credential;
                    return true;
                }));

            var installedPackages = new List<WinGetPackage>
            {
                new WinGetPackage("Pkg1", new PackageId("Id1"), new Version("1.0.0"), null, "winget")
            };
            WinGetServiceMock.Setup(x => x.GetAllInstalledPackages()).Returns(installedPackages);

            ConsoleServiceMock.Setup(x => x.Confirm(It.Is<string>(s => s.Contains("Id1")), It.IsAny<bool>())).Returns(true);
            ConsoleServiceMock.Setup(x => x.Confirm(It.Is<string>(s => s.Contains("1")), It.IsAny<bool>())).Returns(false); // Final confirm

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await Target.InitAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            AuthServiceMock.Verify(x => x.SavePackagesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<GistGetPackage>>()), Times.Never);
        }
    }
}
