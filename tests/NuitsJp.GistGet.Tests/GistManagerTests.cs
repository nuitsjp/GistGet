using Shouldly;
using Xunit;
using NuitsJp.GistGet.Services;
using NuitsJp.GistGet.Models;
using NuitsJp.GistGet.Interfaces;
using Moq;
using Microsoft.Extensions.Logging;

namespace NuitsJp.GistGet.Tests;

public class GistManagerTests
{
    private readonly Mock<GitHubGistClient> _mockGistClient;
    private readonly Mock<IGistConfigurationStorage> _mockStorage;
    private readonly Mock<PackageYamlConverter> _mockYamlConverter;
    private readonly Mock<ILogger<GistManager>> _mockLogger;
    private readonly string _testDirectory;

    public GistManagerTests()
    {
        var mockAuthService = new Mock<GitHubAuthService>(Mock.Of<ILogger<GitHubAuthService>>());
        var mockGistLogger = new Mock<ILogger<GitHubGistClient>>();
        _mockGistClient = new Mock<GitHubGistClient>(mockAuthService.Object, mockGistLogger.Object);

        _testDirectory = Path.Combine(Path.GetTempPath(), $"GistGetTest_{Guid.NewGuid()}");
        _mockStorage = new Mock<IGistConfigurationStorage>();
        _mockYamlConverter = new Mock<PackageYamlConverter>();
        _mockLogger = new Mock<ILogger<GistManager>>();
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitializeCorrectly()
    {
        // Act & Assert
        Should.NotThrow(() => new GistManager(
            _mockGistClient.Object,
            _mockStorage.Object,
            _mockYamlConverter.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullGistClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new GistManager(
            null!,
            _mockStorage.Object,
            _mockYamlConverter.Object,
            _mockLogger.Object));
    }

    [Fact]
    public async Task IsConfiguredAsync_WhenStorageReturnsTrue_ShouldReturnTrue()
    {
        // Arrange
        _mockStorage.Setup(s => s.IsConfiguredAsync()).ReturnsAsync(true);
        var manager = new GistManager(_mockGistClient.Object, _mockStorage.Object, _mockYamlConverter.Object, _mockLogger.Object);

        // Act
        var isConfigured = await manager.IsConfiguredAsync();

        // Assert
        isConfigured.ShouldBeTrue();
        _mockStorage.Verify(s => s.IsConfiguredAsync(), Times.Once);
    }

    [Fact]
    public async Task IsConfiguredAsync_WhenStorageReturnsFalse_ShouldReturnFalse()
    {
        // Arrange
        _mockStorage.Setup(s => s.IsConfiguredAsync()).ReturnsAsync(false);
        var manager = new GistManager(_mockGistClient.Object, _mockStorage.Object, _mockYamlConverter.Object, _mockLogger.Object);

        // Act
        var isConfigured = await manager.IsConfiguredAsync();

        // Assert
        isConfigured.ShouldBeFalse();
        _mockStorage.Verify(s => s.IsConfiguredAsync(), Times.Once);
    }

    [Fact]
    public async Task GetGistPackagesAsync_WhenNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync((GistConfiguration?)null);
        var manager = new GistManager(_mockGistClient.Object, _mockStorage.Object, _mockYamlConverter.Object, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => manager.GetGistPackagesAsync());
    }

    [Fact]
    public async Task GetGistPackagesAsync_WithValidConfiguration_ShouldReturnPackages()
    {
        // Arrange
        var config = new GistConfiguration("d239aabb67e60650fbcb2b20a8342be1", "packages.yaml");
        var yamlContent = "packages:\n  - id: AkelPad.AkelPad";
        var expectedPackages = new PackageCollection();
        expectedPackages.Add(new PackageDefinition("AkelPad.AkelPad"));

        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync(config);
        _mockGistClient.Setup(c => c.GetFileContentAsync(config.GistId, config.FileName)).ReturnsAsync(yamlContent);
        _mockYamlConverter.Setup(y => y.FromYaml(yamlContent)).Returns(expectedPackages);

        var manager = new GistManager(_mockGistClient.Object, _mockStorage.Object, _mockYamlConverter.Object, _mockLogger.Object);

        // Act
        var packages = await manager.GetGistPackagesAsync();

        // Assert
        packages.ShouldNotBeNull();
        packages.Count.ShouldBe(1);
        packages.FindById("AkelPad.AkelPad").ShouldNotBeNull();

        _mockStorage.Verify(s => s.LoadGistConfigurationAsync(), Times.Once);
        _mockGistClient.Verify(c => c.GetFileContentAsync(config.GistId, config.FileName), Times.Once);
        _mockYamlConverter.Verify(y => y.FromYaml(yamlContent), Times.Once);
    }

    [Fact]
    public async Task UpdateGistPackagesAsync_WhenNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync((GistConfiguration?)null);
        var manager = new GistManager(_mockGistClient.Object, _mockStorage.Object, _mockYamlConverter.Object, _mockLogger.Object);
        var packages = new PackageCollection();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => manager.UpdateGistPackagesAsync(packages));
    }

    [Fact]
    public async Task UpdateGistPackagesAsync_WithValidConfiguration_ShouldUpdateGist()
    {
        // Arrange
        var config = new GistConfiguration("d239aabb67e60650fbcb2b20a8342be1", "packages.yaml");
        var packages = new PackageCollection();
        packages.Add(new PackageDefinition("AkelPad.AkelPad"));
        var yamlContent = "packages:\n  - id: AkelPad.AkelPad";

        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync(config);
        _mockYamlConverter.Setup(y => y.ToYaml(packages)).Returns(yamlContent);
        _mockGistClient.Setup(c => c.UpdateFileContentAsync(config.GistId, config.FileName, yamlContent))
                      .Returns(Task.CompletedTask);

        var manager = new GistManager(_mockGistClient.Object, _mockStorage.Object, _mockYamlConverter.Object, _mockLogger.Object);

        // Act
        await manager.UpdateGistPackagesAsync(packages);

        // Assert
        _mockStorage.Verify(s => s.LoadGistConfigurationAsync(), Times.Once);
        _mockYamlConverter.Verify(y => y.ToYaml(packages), Times.Once);
        _mockGistClient.Verify(c => c.UpdateFileContentAsync(config.GistId, config.FileName, yamlContent), Times.Once);
    }

    [Fact]
    public async Task UpdateGistPackagesAsync_WithNullPackages_ShouldThrowArgumentNullException()
    {
        // Arrange
        var manager = new GistManager(_mockGistClient.Object, _mockStorage.Object, _mockYamlConverter.Object, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => manager.UpdateGistPackagesAsync(null!));
    }

    [Fact]
    public async Task GetGistPackagesAsync_WhenGistClientThrows_ShouldPropagateException()
    {
        // Arrange
        var config = new GistConfiguration("d239aabb67e60650fbcb2b20a8342be1", "packages.yaml");
        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync(config);
        _mockGistClient.Setup(c => c.GetFileContentAsync(It.IsAny<string>(), It.IsAny<string>()))
                      .ThrowsAsync(new InvalidOperationException("Gist not found"));

        var manager = new GistManager(_mockGistClient.Object, _mockStorage.Object, _mockYamlConverter.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => manager.GetGistPackagesAsync());
        exception.Message.ShouldContain("Gist not found");
    }

    [Fact]
    public async Task UpdateGistPackagesAsync_WhenGistClientThrows_ShouldPropagateException()
    {
        // Arrange
        var config = new GistConfiguration("d239aabb67e60650fbcb2b20a8342be1", "packages.yaml");
        var packages = new PackageCollection();
        var yamlContent = "packages: []";

        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync(config);
        _mockYamlConverter.Setup(y => y.ToYaml(packages)).Returns(yamlContent);
        _mockGistClient.Setup(c => c.UpdateFileContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                      .ThrowsAsync(new InvalidOperationException("Failed to update"));

        var manager = new GistManager(_mockGistClient.Object, _mockStorage.Object, _mockYamlConverter.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => manager.UpdateGistPackagesAsync(packages));
        exception.Message.ShouldContain("Failed to update");
    }
}