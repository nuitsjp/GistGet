using Shouldly;
using Xunit;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Models;
using NuitsJp.GistGet.Infrastructure.Storage;
using NuitsJp.GistGet.Infrastructure.GitHub;
using Moq;
using Microsoft.Extensions.Logging;

namespace NuitsJp.GistGet.Tests;

public class GistManagerTests
{
    private readonly GitHubGistClient _gistClient;
    private readonly Mock<IGistConfigurationStorage> _mockStorage;
    private readonly PackageYamlConverter _yamlConverter;
    private readonly Mock<ILogger<GistManager>> _mockLogger;
    private readonly string _testDirectory;

    public GistManagerTests()
    {
        var authLogger = new Mock<ILogger<GitHubAuthService>>();
        var authService = new GitHubAuthService(authLogger.Object);
        var gistLogger = new Mock<ILogger<GitHubGistClient>>();
        _gistClient = new GitHubGistClient(authService, gistLogger.Object);

        _testDirectory = Path.Combine(Path.GetTempPath(), $"GistGetTest_{Guid.NewGuid()}");
        _mockStorage = new Mock<IGistConfigurationStorage>();
        _yamlConverter = new PackageYamlConverter();
        _mockLogger = new Mock<ILogger<GistManager>>();
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitializeCorrectly()
    {
        // Act & Assert
        Should.NotThrow(() => new GistManager(
            _gistClient,
            _mockStorage.Object,
            _yamlConverter,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullGistClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new GistManager(
            null!,
            _mockStorage.Object,
            _yamlConverter,
            _mockLogger.Object));
    }

    [Fact]
    public async Task IsConfiguredAsync_WhenStorageReturnsTrue_ShouldReturnTrue()
    {
        // Arrange
        _mockStorage.Setup(s => s.IsConfiguredAsync()).ReturnsAsync(true);
        var manager = new GistManager(_gistClient, _mockStorage.Object, _yamlConverter, _mockLogger.Object);

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
        var manager = new GistManager(_gistClient, _mockStorage.Object, _yamlConverter, _mockLogger.Object);

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
        var manager = new GistManager(_gistClient, _mockStorage.Object, _yamlConverter, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => manager.GetGistPackagesAsync());
    }

    [Fact(Skip = "Integration test - requires network access to Gist API")]
    public async Task GetGistPackagesAsync_WithValidConfiguration_ShouldReturnPackages()
    {
        // Integration test - would require valid Gist credentials
        // Skipping to maintain unit test isolation
        await Task.CompletedTask;
    }

    [Fact]
    public async Task UpdateGistPackagesAsync_WhenNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockStorage.Setup(s => s.LoadGistConfigurationAsync()).ReturnsAsync((GistConfiguration?)null);
        var manager = new GistManager(_gistClient, _mockStorage.Object, _yamlConverter, _mockLogger.Object);
        var packages = new PackageCollection();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => manager.UpdateGistPackagesAsync(packages));
    }

    [Fact(Skip = "Integration test - requires network access to Gist API")]
    public async Task UpdateGistPackagesAsync_WithValidConfiguration_ShouldUpdateGist()
    {
        // Integration test - would require valid Gist credentials
        // Skipping to maintain unit test isolation
        await Task.CompletedTask;
    }

    [Fact]
    public async Task UpdateGistPackagesAsync_WithNullPackages_ShouldThrowArgumentNullException()
    {
        // Arrange
        var manager = new GistManager(_gistClient, _mockStorage.Object, _yamlConverter, _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => manager.UpdateGistPackagesAsync(null!));
    }

    [Fact(Skip = "Integration test - requires network access to Gist API")]
    public async Task GetGistPackagesAsync_WhenGistClientThrows_ShouldPropagateException()
    {
        // Integration test - would require valid Gist credentials to test error scenarios
        // Skipping to maintain unit test isolation
        await Task.CompletedTask;
    }

    [Fact(Skip = "Integration test - requires network access to Gist API")]
    public async Task UpdateGistPackagesAsync_WhenGistClientThrows_ShouldPropagateException()
    {
        // Integration test - would require valid Gist credentials to test error scenarios
        // Skipping to maintain unit test isolation
        await Task.CompletedTask;
    }
}