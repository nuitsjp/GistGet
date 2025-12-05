using GistGet.Application.Services;
using GistGet.Models;
using GistGet.Presentation;
using Moq;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;
using Xunit;

namespace GistGet.Tests.Presentation;

public class CliCommandBuilderTests
{
    private readonly Mock<IPackageService> _mockPackageService;
    private readonly Mock<IGistService> _mockGistService;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly CliCommandBuilder _builder;

    public CliCommandBuilderTests()
    {
        _mockPackageService = new Mock<IPackageService>();
        _mockGistService = new Mock<IGistService>();
        _mockAuthService = new Mock<IAuthService>();
        _builder = new CliCommandBuilder(_mockPackageService.Object, _mockGistService.Object, _mockAuthService.Object);
    }

    [Fact]
    public void Build_ShouldReturnRootCommandWithSubcommands()
    {
        // Act
        var root = _builder.Build();

        // Assert
        Assert.Contains(root.Subcommands, c => c.Name == "sync");
        Assert.Contains(root.Subcommands, c => c.Name == "export");
        Assert.Contains(root.Subcommands, c => c.Name == "import");
        Assert.Contains(root.Subcommands, c => c.Name == "auth");
        Assert.Contains(root.Subcommands, c => c.Name == "install"); // passthrough
    }

    [Fact]
    public async Task SyncCommand_ShouldInvokeServiceAndPersistDefinitions()
    {
        // Arrange
        var root = _builder.Build();
        var gistPackages = new Dictionary<string, GistGetPackage>();
        _mockGistService.Setup(x => x.GetPackagesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(gistPackages);
        _mockGistService.Setup(x => x.SavePackagesAsync(gistPackages, It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _mockPackageService.Setup(x => x.GetInstalledPackagesAsync())
            .ReturnsAsync(new Dictionary<string, GistGetPackage>());
        _mockPackageService.Setup(x => x.SyncAsync(It.IsAny<Dictionary<string, GistGetPackage>>(), It.IsAny<Dictionary<string, GistGetPackage>>()))
            .ReturnsAsync(new SyncResult());

        // Act
        await root.InvokeAsync("sync");

        // Assert
        _mockPackageService.Verify(x => x.SyncAsync(It.IsAny<Dictionary<string, GistGetPackage>>(), It.IsAny<Dictionary<string, GistGetPackage>>()), Times.Once);
        _mockGistService.Verify(x => x.SavePackagesAsync(gistPackages, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }
    [Fact]
    public async Task AuthStatusCommand_ShouldShowUserInfo_WhenAuthenticated()
    {
        // Arrange
        var root = _builder.Build();
        _mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(true);
        _mockAuthService.Setup(x => x.GetUserInfoAsync()).ReturnsAsync(new GitHubUserInfo { Login = "testuser" });

        // Act
        await root.InvokeAsync("auth status");

        // Assert
        _mockAuthService.Verify(x => x.GetUserInfoAsync(), Times.Once);
    }
    [Fact]
    public async Task InstallCommand_ShouldParseOptionsAndInvokeService()
    {
        // Arrange
        var root = _builder.Build();
        _mockPackageService.Setup(x => x.InstallAndSaveAsync(It.IsAny<GistGetPackage>()))
            .ReturnsAsync(true);

        // Act
        await root.InvokeAsync("install --id MyPackage --version 1.0.0 --scope user --interactive --silent --force");

        // Assert
        _mockPackageService.Verify(x => x.InstallAndSaveAsync(It.Is<GistGetPackage>(p =>
            p.Id == "MyPackage" &&
            p.Version == "1.0.0" &&
            p.Scope == "user" &&
            p.Interactive == true &&
            p.Silent == true &&
            p.Force == true
        )), Times.Once);
    }
    [Fact]
    public async Task InstallCommand_ShouldAcceptIdOption()
    {
        // Arrange
        var root = _builder.Build();
        _mockPackageService.Setup(x => x.InstallAndSaveAsync(It.IsAny<GistGetPackage>()))
            .ReturnsAsync(true);

        // Act
        await root.InvokeAsync("install --id MyPackage --version 1.2.3");

        // Assert
        _mockPackageService.Verify(x => x.InstallAndSaveAsync(It.Is<GistGetPackage>(p =>
            p.Id == "MyPackage" &&
            p.Version == "1.2.3")), Times.Once);
    }

    [Fact]
    public async Task InstallCommand_ShouldFailWithoutId()
    {
        // Arrange
        var root = _builder.Build();

        // Act
        var exitCode = await root.InvokeAsync("install --version 1.0.0");

        // Assert
        Assert.NotEqual(0, exitCode);
        _mockPackageService.Verify(x => x.InstallAndSaveAsync(It.IsAny<GistGetPackage>()), Times.Never);
    }
    [Fact]
    public async Task PinAddCommand_ShouldInvokeService()
    {
        // Arrange
        var root = _builder.Build();
        _mockPackageService.Setup(x => x.PinAddAndSaveAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await root.InvokeAsync("pin add MyPackage --version 1.0.0");

        // Assert
        _mockPackageService.Verify(x => x.PinAddAndSaveAsync("MyPackage", "1.0.0"), Times.Once);
    }

    [Fact]
    public async Task PinRemoveCommand_ShouldInvokeService()
    {
        // Arrange
        var root = _builder.Build();
        _mockPackageService.Setup(x => x.PinRemoveAndSaveAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await root.InvokeAsync("pin remove MyPackage");

        // Assert
        _mockPackageService.Verify(x => x.PinRemoveAndSaveAsync("MyPackage"), Times.Once);
    }

    [Fact]
    public async Task UninstallCommand_ShouldRequireIdOption()
    {
        // Arrange
        var root = _builder.Build();
        _mockPackageService.Setup(x => x.UninstallAndSaveAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await root.InvokeAsync("uninstall --id MyPackage");

        // Assert
        _mockPackageService.Verify(x => x.UninstallAndSaveAsync("MyPackage"), Times.Once);
    }

    [Fact]
    public async Task UninstallCommand_ShouldFailWithoutId()
    {
        // Arrange
        var root = _builder.Build();

        // Act
        var exitCode = await root.InvokeAsync("uninstall");

        // Assert
        Assert.NotEqual(0, exitCode);
        _mockPackageService.Verify(x => x.UninstallAndSaveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpgradeCommand_ShouldAcceptIdOption()
    {
        // Arrange
        var root = _builder.Build();
        _mockPackageService.Setup(x => x.UpgradeAndSaveAsync(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        // Act
        await root.InvokeAsync("upgrade --id MyPackage --version 2.0.0");

        // Assert
        _mockPackageService.Verify(x => x.UpgradeAndSaveAsync("MyPackage", "2.0.0"), Times.Once);
    }
}
