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

        // Default setup for Pin/Unpin to avoid null reference if not setup in specific tests
        _mockExecutor.Setup(x => x.PinPackageAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _mockExecutor.Setup(x => x.UnpinPackageAsync(It.IsAny<string>())).ReturnsAsync(true);
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
        _mockExecutor.Setup(x => x.RunPassthroughAsync(command, args)).ReturnsAsync(0);

        // Act
        var exitCode = await _packageService.RunPassthroughAsync(command, args);

        // Assert
        Assert.Equal(0, exitCode);
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
    [Fact]
    public async Task InstallAndSaveAsync_ShouldPin_WhenVersionSpecified()
    {
        // Arrange
        var package = new GistGetPackage { Id = "Test", Version = "1.0.0" };
        _mockExecutor.Setup(x => x.InstallPackageAsync(package)).ReturnsAsync(true);
        _mockGistService.Setup(x => x.GetPackagesAsync(null)).ReturnsAsync(new Dictionary<string, GistGetPackage>());

        // Act
        await _packageService.InstallAndSaveAsync(package);

        // Assert
        _mockExecutor.Verify(x => x.PinPackageAsync("Test", "1.0.0"), Times.Once);
        _mockExecutor.Verify(x => x.UnpinPackageAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InstallAndSaveAsync_ShouldUnpin_WhenVersionNotSpecified()
    {
        // Arrange
        var package = new GistGetPackage { Id = "Test", Version = null };
        _mockExecutor.Setup(x => x.InstallPackageAsync(package)).ReturnsAsync(true);
        _mockGistService.Setup(x => x.GetPackagesAsync(null)).ReturnsAsync(new Dictionary<string, GistGetPackage>());

        // Act
        await _packageService.InstallAndSaveAsync(package);

        // Assert
        _mockExecutor.Verify(x => x.UnpinPackageAsync("Test"), Times.Once);
        _mockExecutor.Verify(x => x.PinPackageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpgradeAndSaveAsync_ShouldPin_WhenVersionSpecified()
    {
        // Arrange
        var packageId = "Test";
        var version = "2.0.0";
        _mockExecutor.Setup(x => x.UpgradePackageAsync(packageId, version)).ReturnsAsync(true);
        _mockGistService.Setup(x => x.GetPackagesAsync(null)).ReturnsAsync(new Dictionary<string, GistGetPackage>());

        // Act
        await _packageService.UpgradeAndSaveAsync(packageId, version);

        // Assert
        _mockExecutor.Verify(x => x.PinPackageAsync(packageId, version), Times.Once);
    }

    [Fact]
    public async Task UpgradeAndSaveAsync_ShouldUnpin_WhenVersionNotSpecified()
    {
        // Arrange
        var packageId = "Test";
        _mockExecutor.Setup(x => x.UpgradePackageAsync(packageId, null)).ReturnsAsync(true);
        _mockGistService.Setup(x => x.GetPackagesAsync(null)).ReturnsAsync(new Dictionary<string, GistGetPackage>());

        // Act
        await _packageService.UpgradeAndSaveAsync(packageId, null);

        // Assert
        _mockExecutor.Verify(x => x.UnpinPackageAsync(packageId), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_ShouldEnforcePinning()
    {
        // Arrange
        var gistPackages = new Dictionary<string, GistGetPackage>
        {
            { "Pinned", new GistGetPackage { Id = "Pinned", Version = "1.0.0" } },
            { "Unpinned", new GistGetPackage { Id = "Unpinned", Version = null } }
        };
        var localPackages = new Dictionary<string, GistGetPackage>
        {
            { "Pinned", new GistGetPackage { Id = "Pinned" } },
            { "Unpinned", new GistGetPackage { Id = "Unpinned" } }
        };

        // Act
        await _packageService.SyncAsync(gistPackages, localPackages);

        // Assert
        _mockExecutor.Verify(x => x.PinPackageAsync("Pinned", "1.0.0"), Times.Once);
        _mockExecutor.Verify(x => x.UnpinPackageAsync("Unpinned"), Times.Once);
    }
    [Fact]
    public async Task InstallAndSaveAsync_ShouldReturnFalse_WhenInstallFails()
    {
        // Arrange
        var package = new GistGetPackage { Id = "Test" };
        _mockExecutor.Setup(x => x.InstallPackageAsync(package)).ReturnsAsync(false);

        // Act
        var result = await _packageService.InstallAndSaveAsync(package);

        // Assert
        Assert.False(result);
        _mockGistService.Verify(x => x.SavePackagesAsync(It.IsAny<Dictionary<string, GistGetPackage>>()), Times.Never);
    }

    [Fact]
    public async Task UninstallAndSaveAsync_ShouldReturnFalse_WhenUninstallFails()
    {
        // Arrange
        var packageId = "Test";
        _mockExecutor.Setup(x => x.UninstallPackageAsync(packageId)).ReturnsAsync(false);

        // Act
        var result = await _packageService.UninstallAndSaveAsync(packageId);

        // Assert
        Assert.False(result);
        _mockGistService.Verify(x => x.SavePackagesAsync(It.IsAny<Dictionary<string, GistGetPackage>>()), Times.Never);
    }

    [Fact]
    public async Task UpgradeAndSaveAsync_ShouldReturnFalse_WhenUpgradeFails()
    {
        // Arrange
        var packageId = "Test";
        _mockExecutor.Setup(x => x.UpgradePackageAsync(packageId, null)).ReturnsAsync(false);

        // Act
        var result = await _packageService.UpgradeAndSaveAsync(packageId);

        // Assert
        Assert.False(result);
        _mockGistService.Verify(x => x.SavePackagesAsync(It.IsAny<Dictionary<string, GistGetPackage>>()), Times.Never);
    }

    [Fact]
    public async Task UpgradeAndSaveAsync_ShouldUpdateExistingPackage()
    {
        // Arrange
        var packageId = "Test";
        var version = "2.0.0";
        _mockExecutor.Setup(x => x.UpgradePackageAsync(packageId, version)).ReturnsAsync(true);

        var existingPackage = new GistGetPackage { Id = packageId, Version = "1.0.0", Uninstall = true };
        var packages = new Dictionary<string, GistGetPackage> { { packageId, existingPackage } };
        _mockGistService.Setup(x => x.GetPackagesAsync(null)).ReturnsAsync(packages);

        // Act
        await _packageService.UpgradeAndSaveAsync(packageId, version);

        // Assert
        Assert.Equal(version, existingPackage.Version);
        Assert.False(existingPackage.Uninstall);
        _mockGistService.Verify(x => x.SavePackagesAsync(packages), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_ShouldHandleFailures()
    {
        // Arrange
        var gistPackages = new Dictionary<string, GistGetPackage>
        {
            { "InstallFail", new GistGetPackage { Id = "InstallFail" } },
            { "UninstallFail", new GistGetPackage { Id = "UninstallFail", Uninstall = true } }
        };
        var localPackages = new Dictionary<string, GistGetPackage>
        {
            { "UninstallFail", new GistGetPackage { Id = "UninstallFail" } }
        };

        _mockExecutor.Setup(x => x.InstallPackageAsync(It.Is<GistGetPackage>(p => p.Id == "InstallFail"))).ReturnsAsync(false);
        _mockExecutor.Setup(x => x.UninstallPackageAsync("UninstallFail")).ReturnsAsync(false);

        // Act
        var result = await _packageService.SyncAsync(gistPackages, localPackages);

        // Assert
        Assert.Contains(result.Failed, p => p.Id == "InstallFail");
        Assert.Contains(result.Failed, p => p.Id == "UninstallFail");
        Assert.Equal(2, result.Errors.Count);
    }
}
