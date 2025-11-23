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
    public async Task SyncCommand_ShouldInvokeService()
    {
        // Arrange
        var root = _builder.Build();
        _mockGistService.Setup(x => x.GetPackagesAsync(It.IsAny<string?>()))
            .ReturnsAsync(new Dictionary<string, GistGetPackage>());
        _mockPackageService.Setup(x => x.GetInstalledPackagesAsync())
            .ReturnsAsync(new Dictionary<string, GistGetPackage>());
        _mockPackageService.Setup(x => x.SyncAsync(It.IsAny<Dictionary<string, GistGetPackage>>(), It.IsAny<Dictionary<string, GistGetPackage>>()))
            .ReturnsAsync(new SyncResult());

        // Act
        await root.InvokeAsync("sync");

        // Assert
        _mockPackageService.Verify(x => x.SyncAsync(It.IsAny<Dictionary<string, GistGetPackage>>(), It.IsAny<Dictionary<string, GistGetPackage>>()), Times.Once);
    }
}
