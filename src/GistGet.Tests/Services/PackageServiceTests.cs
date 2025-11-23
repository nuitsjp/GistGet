using GistGet.Application.Services;
using GistGet.Infrastructure.WinGet;
using GistGet.Models;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace GistGet.Tests.Services;

public class PackageServiceTests
{
    private readonly Mock<IWinGetRepository> _mockRepository;
    private readonly Mock<IWinGetExecutor> _mockExecutor;
    private readonly Mock<IGistService> _mockGistService;
    private readonly PackageService _packageService;

    public PackageServiceTests()
    {
        _mockRepository = new Mock<IWinGetRepository>();
        _mockExecutor = new Mock<IWinGetExecutor>();
        _mockGistService = new Mock<IGistService>();
        _packageService = new PackageService(_mockRepository.Object, _mockExecutor.Object, _mockGistService.Object);
    }

    [Fact]
    public async Task GetInstalledPackagesAsync_ShouldDelegatToRepository()
    {
        // Arrange
        var expectedPackages = new Dictionary<string, GistGetPackage>();
        _mockRepository.Setup(x => x.GetInstalledPackagesAsync()).ReturnsAsync(expectedPackages);

        // Act
        var result = await _packageService.GetInstalledPackagesAsync();

        // Assert
        Assert.Same(expectedPackages, result);
        _mockRepository.Verify(x => x.GetInstalledPackagesAsync(), Times.Once);
    }

    [Fact]
    public async Task InstallPackageAsync_ShouldDelegateToExecutor()
    {
        // Arrange
        var package = new GistGetPackage { Id = "Test" };
        _mockExecutor.Setup(x => x.InstallPackageAsync(package)).ReturnsAsync(true);

        // Act
        var result = await _packageService.InstallPackageAsync(package);

        // Assert
        Assert.True(result);
        _mockExecutor.Verify(x => x.InstallPackageAsync(package), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_ShouldInstallMissingPackages()
    {
        // Arrange
        var gistPackages = new Dictionary<string, GistGetPackage>
        {
            { "New.Package", new GistGetPackage { Id = "New.Package" } }
        };
        var localPackages = new Dictionary<string, GistGetPackage>();

        _mockExecutor.Setup(x => x.InstallPackageAsync(It.IsAny<GistGetPackage>())).ReturnsAsync(true);

        // Act
        var result = await _packageService.SyncAsync(gistPackages, localPackages);

        // Assert
        Assert.Single(result.Installed);
        Assert.Equal("New.Package", result.Installed[0].Id);
        _mockExecutor.Verify(x => x.InstallPackageAsync(It.Is<GistGetPackage>(p => p.Id == "New.Package")), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_ShouldUninstallMarkedPackages()
    {
        // Arrange
        var gistPackages = new Dictionary<string, GistGetPackage>
        {
            { "Old.Package", new GistGetPackage { Id = "Old.Package", Uninstall = true } }
        };
        var localPackages = new Dictionary<string, GistGetPackage>
        {
            { "Old.Package", new GistGetPackage { Id = "Old.Package" } }
        };

        _mockExecutor.Setup(x => x.UninstallPackageAsync(It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _packageService.SyncAsync(gistPackages, localPackages);

        // Assert
        Assert.Single(result.Uninstalled);
        Assert.Equal("Old.Package", result.Uninstalled[0].Id);
        _mockExecutor.Verify(x => x.UninstallPackageAsync("Old.Package"), Times.Once);
    }

    [Fact]
    public async Task RunPassthroughAsync_ShouldDelegateToExecutor()
    {
        // Arrange
        var command = "search";
        var args = new[] { "vscode" };
        _mockExecutor.Setup(x => x.RunPassthroughAsync(command, args)).Returns(Task.CompletedTask);

        // Act
        await _packageService.RunPassthroughAsync(command, args);

        // Assert
        _mockExecutor.Verify(x => x.RunPassthroughAsync(command, args), Times.Once);
    }

    [Fact]
    public async Task InstallAndSaveAsync_ShouldInstallAndSave()
    {
        // Arrange
        var package = new GistGetPackage { Id = "Test" };
        _mockExecutor.Setup(x => x.InstallPackageAsync(package)).ReturnsAsync(true);
        _mockGistService.Setup(x => x.GetPackagesAsync(null)).ReturnsAsync(new Dictionary<string, GistGetPackage>());

        // Act
        var result = await _packageService.InstallAndSaveAsync(package);

        // Assert
        Assert.True(result);
        _mockExecutor.Verify(x => x.InstallPackageAsync(package), Times.Once);
        _mockGistService.Verify(x => x.SavePackagesAsync(It.Is<Dictionary<string, GistGetPackage>>(d => d.ContainsKey("Test"))), Times.Once);
    }

    [Fact]
    public async Task UninstallAndSaveAsync_ShouldUninstallAndMark()
    {
        // Arrange
        var packageId = "Test";
        _mockExecutor.Setup(x => x.UninstallPackageAsync(packageId)).ReturnsAsync(true);
        var packages = new Dictionary<string, GistGetPackage> { { packageId, new GistGetPackage { Id = packageId } } };
        _mockGistService.Setup(x => x.GetPackagesAsync(null)).ReturnsAsync(packages);

        // Act
        var result = await _packageService.UninstallAndSaveAsync(packageId);

        // Assert
        Assert.True(result);
        _mockExecutor.Verify(x => x.UninstallPackageAsync(packageId), Times.Once);
        _mockGistService.Verify(x => x.SavePackagesAsync(It.Is<Dictionary<string, GistGetPackage>>(d => d[packageId].Uninstall)), Times.Once);
    }
}
