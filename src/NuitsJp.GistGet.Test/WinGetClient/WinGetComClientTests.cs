using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.WinGetClient;
using NuitsJp.GistGet.WinGetClient.Abstractions;
using NuitsJp.GistGet.WinGetClient.Models;
using Moq;
using Shouldly;
using Xunit;
using WinGetDeployment = Microsoft.Management.Deployment;

namespace NuitsJp.GistGet.Test.WinGetClient;

/// <summary>
/// Tests for simplified WinGetComClient (CLI fallback removed - Phase 3.5)
/// Following t-wada TDD methodology: Red-Green-Refactor
/// </summary>
public class WinGetComClientTests
{
    private readonly Mock<ILogger<WinGetComClient>> _mockLogger;
    private readonly Mock<IComInteropWrapper> _mockComInterop;
    private readonly WinGetComClient _client;

    public WinGetComClientTests()
    {
        _mockLogger = new Mock<ILogger<WinGetComClient>>();
        _mockComInterop = new Mock<IComInteropWrapper>();
        
        // Default mock setup for common test scenarios
        _mockComInterop.Setup(x => x.IsComApiAvailable()).Returns(true);
        
        _client = new WinGetComClient(_mockLogger.Object, _mockComInterop.Object);
    }

    #region Constructor Tests (第1イテレーション)

    [Fact]
    public void Constructor_ShouldCreateInstance_WhenAllParametersAreProvided()
    {
        // Act
        var client = new WinGetComClient(_mockLogger.Object, _mockComInterop.Object);

        // Assert
        client.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new WinGetComClient(null!, _mockComInterop.Object))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenComInteropWrapperIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new WinGetComClient(_mockLogger.Object, null!))
            .ParamName.ShouldBe("comInteropWrapper");
    }

    #endregion

    #region Initialization Tests (第2イテレーション)

    [Fact]
    public async Task InitializeAsync_ShouldReturnTrue_WhenComApiIsAvailable()
    {
        // Arrange
        // PackageManagerはsealedクラスのため、モック不可。nullを返してもIsComApiAvailable()でtrueなら成功とする
        _mockComInterop.Setup(x => x.CreatePackageManager())
                      .Returns((WinGetDeployment.PackageManager?)null);

        // Act
        var result = await _client.InitializeAsync();

        // Assert
        result.ShouldBeTrue();
        _mockComInterop.Verify(x => x.IsComApiAvailable(), Times.Once);
        _mockComInterop.Verify(x => x.CreatePackageManager(), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnFalse_WhenComApiIsNotAvailable()
    {
        // Arrange
        _mockComInterop.Setup(x => x.IsComApiAvailable()).Returns(false);

        // Act
        var result = await _client.InitializeAsync();

        // Assert
        result.ShouldBeFalse();
        _mockComInterop.Verify(x => x.IsComApiAvailable(), Times.Once);
        // CreatePackageManager should not be called when IsComApiAvailable returns false
        _mockComInterop.Verify(x => x.CreatePackageManager(), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnFalse_WhenComApiThrowsException()
    {
        // Arrange
        _mockComInterop.Setup(x => x.CreatePackageManager())
                      .Throws(new InvalidOperationException("COM API not available"));

        // Act
        var result = await _client.InitializeAsync();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnTrue_WhenCalledMultipleTimes()
    {
        // Arrange
        _mockComInterop.Setup(x => x.CreatePackageManager())
                      .Returns((WinGetDeployment.PackageManager?)null);

        // Act
        var result1 = await _client.InitializeAsync();
        var result2 = await _client.InitializeAsync();

        // Assert
        result1.ShouldBeTrue();
        result2.ShouldBeTrue();
        _mockComInterop.Verify(x => x.CreatePackageManager(), Times.Once);
    }

    #endregion

    #region Client Info Tests (第3イテレーション)

    [Fact]
    public void GetClientInfo_ShouldReturnCorrectInfo_WhenNotInitialized()
    {
        // Arrange
        _mockComInterop.Setup(x => x.IsComApiAvailable()).Returns(false);
        
        // Act
        var info = _client.GetClientInfo();

        // Assert
        info.ComApiAvailable.ShouldBeFalse();
        info.CliAvailable.ShouldBeFalse();
        info.CliVersion.ShouldBeNull();
        info.CliPath.ShouldBeNull();
        info.ActiveMode.ShouldBe(ClientMode.Unavailable);
    }

    [Fact]
    public async Task GetClientInfo_ShouldReturnCorrectInfo_WhenInitialized()
    {
        // Arrange
        // Mock the wrapper to return success without needing actual PackageManager
        _mockComInterop.Setup(x => x.CreatePackageManager())
                      .Returns((WinGetDeployment.PackageManager?)null); // Return null but IsComApiAvailable returns true
        
        await _client.InitializeAsync();

        // Act
        var info = _client.GetClientInfo();

        // Assert
        info.ComApiAvailable.ShouldBeTrue();
        info.CliAvailable.ShouldBeFalse();
        info.CliVersion.ShouldBeNull();
        info.CliPath.ShouldBeNull();
        info.ActiveMode.ShouldBe(ClientMode.ComApi);
        // Note: ComApiVersion and AvailableSources may be null in minimal implementation
    }

    #endregion

    #region Operation Tests - Uninitialized (第4イテレーション)

    [Fact]
    public async Task SearchPackagesAsync_ShouldThrowInvalidOperationException_WhenNotInitialized()
    {
        // Arrange
        _mockComInterop.Setup(x => x.IsComApiAvailable()).Returns(false);
        var options = new SearchOptions { Query = "test" };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _client.SearchPackagesAsync(options));
        
        exception.Message.ShouldContain("WinGet COM API could not be initialized");
    }

    [Fact]
    public async Task ListInstalledPackagesAsync_ShouldThrowInvalidOperationException_WhenNotInitialized()
    {
        // Arrange
        _mockComInterop.Setup(x => x.IsComApiAvailable()).Returns(false);
        var options = new ListOptions();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _client.ListInstalledPackagesAsync(options));
        
        exception.Message.ShouldContain("WinGet COM API could not be initialized");
    }

    [Fact]
    public async Task InstallPackageAsync_ShouldThrowInvalidOperationException_WhenNotInitialized()
    {
        // Arrange
        _mockComInterop.Setup(x => x.IsComApiAvailable()).Returns(false);
        var options = new NuitsJp.GistGet.WinGetClient.Models.InstallOptions { Id = "test.app" };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _client.InstallPackageAsync(options));
        
        exception.Message.ShouldContain("WinGet COM API could not be initialized");
    }

    #endregion

    #region Basic Operation Tests - Initialized (第5イテレーション)

    [Fact]
    public async Task SearchPackagesAsync_ShouldReturnEmptyList_WhenInitialized()
    {
        // Arrange
        _mockComInterop.Setup(x => x.CreatePackageManager())
                      .Returns((WinGetDeployment.PackageManager?)null); // No actual PackageManager needed for test
        await _client.InitializeAsync();
        
        var options = new SearchOptions { Query = "test" };

        // Act
        var result = await _client.SearchPackagesAsync(options);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0); // Placeholder implementation returns empty list
    }

    [Fact]
    public async Task ListInstalledPackagesAsync_ShouldReturnEmptyList_WhenInitialized()
    {
        // Arrange
        _mockComInterop.Setup(x => x.CreatePackageManager())
                      .Returns((WinGetDeployment.PackageManager?)null); // No actual PackageManager needed for test
        await _client.InitializeAsync();
        
        var options = new ListOptions();

        // Act
        var result = await _client.ListInstalledPackagesAsync(options);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0); // Placeholder implementation returns empty list
    }

    [Fact]
    public async Task InstallPackageAsync_ShouldReturnSuccess_WhenInitialized()
    {
        // Arrange
        _mockComInterop.Setup(x => x.CreatePackageManager())
                      .Returns((WinGetDeployment.PackageManager?)null); // No actual PackageManager needed for test
        await _client.InitializeAsync();
        
        var options = new NuitsJp.GistGet.WinGetClient.Models.InstallOptions { Id = "test.app" };

        // Act
        var result = await _client.InstallPackageAsync(options);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.UsedComApi.ShouldBeTrue();
        result.Message.ShouldContain("COM API");
    }

    #endregion

    #region Dispose Tests (第6イテレーション)

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => _client.Dispose());
    }

    [Fact]
    public void Dispose_ShouldBeCallableMultipleTimes()
    {
        // Act & Assert
        Should.NotThrow(() => 
        {
            _client.Dispose();
            _client.Dispose();
        });
    }

    #endregion
}
