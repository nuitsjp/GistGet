using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.WinGetClient;
using NuitsJp.GistGet.WinGetClient.Models;
using Moq;
using Shouldly;
using Xunit;

namespace NuitsJp.GistGet.Test.WinGetClient;

/// <summary>
/// Tests for simplified WinGetComClient (Phase 4 - Direct COM API)
/// Following t-wada TDD methodology: Red-Green-Refactor
/// </summary>
public class WinGetComClientTests
{
    private readonly Mock<ILogger<WinGetComClient>> _mockLogger;
    private readonly WinGetComClient _client;

    public WinGetComClientTests()
    {
        _mockLogger = new Mock<ILogger<WinGetComClient>>();
        _client = new WinGetComClient(_mockLogger.Object);
    }

    #region Constructor Tests (第1イテレーション)

    [Fact]
    public void Constructor_ShouldCreateInstance_WhenLoggerIsProvided()
    {
        // Act
        var client = new WinGetComClient(_mockLogger.Object);

        // Assert
        client.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new WinGetComClient(null!))
            .ParamName.ShouldBe("logger");
    }

    #endregion

    #region Initialization Tests (第2イテレーション)

    [Fact]
    public async Task InitializeAsync_Always_ReturnsTrue()
    {
        // Act
        var result = await _client.InitializeAsync();
        
        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task InitializeAsync_CalledMultipleTimes_ReturnsTrue()
    {
        // Act
        var result1 = await _client.InitializeAsync();
        var result2 = await _client.InitializeAsync();
        
        // Assert
        result1.ShouldBeTrue();
        result2.ShouldBeTrue();
    }

    #endregion

    #region GetClientInfo Tests (第3イテレーション)

    [Fact]
    public void GetClientInfo_BeforeInitialize_ReturnsUnavailableStatus()
    {
        // Act
        var info = _client.GetClientInfo();
        
        // Assert
        info.ComApiAvailable.ShouldBeFalse();
        info.ActiveMode.ShouldBe(ClientMode.Unavailable);
        info.ComApiVersion.ShouldBeNull();
    }

    [Fact]
    public async Task GetClientInfo_AfterInitialize_ReturnsComApiAvailableStatus()
    {
        // Arrange
        await _client.InitializeAsync();
        
        // Act
        var info = _client.GetClientInfo();
        
        // Assert
        info.ComApiAvailable.ShouldBeTrue();
        info.ActiveMode.ShouldBe(ClientMode.ComApi);
        info.ComApiVersion.ShouldBe("1.11.430");
        info.AvailableSources.ShouldNotBeNull();
        info.AvailableSources.ShouldContain("winget");
        info.AvailableSources.ShouldContain("msstore");
    }

    #endregion

    #region SearchPackagesAsync Tests (第4イテレーション)

    [Fact]
    public async Task SearchPackagesAsync_ReturnsEmptyList()
    {
        // Act
        var result = await _client.SearchPackagesAsync(new SearchOptions { Query = "test" });
        
        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchPackagesAsync_LogsSearchQuery()
    {
        // Arrange
        var searchOptions = new SearchOptions { Query = "git" };
        
        // Act
        await _client.SearchPackagesAsync(searchOptions);
        
        // Assert - Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Searching packages with query: git")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ListInstalledPackagesAsync Tests (第5イテレーション)

    [Fact]
    public async Task ListInstalledPackagesAsync_ReturnsEmptyList()
    {
        // Act
        var result = await _client.ListInstalledPackagesAsync(new ListOptions());
        
        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    #endregion

    #region InstallPackageAsync Tests (第6イテレーション)

    [Fact]
    public async Task InstallPackageAsync_ReturnsSuccessResult()
    {
        // Arrange
        var installOptions = new InstallOptions { Id = "Git.Git" };
        
        // Act
        var result = await _client.InstallPackageAsync(installOptions);
        
        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.UsedComApi.ShouldBeTrue();
        result.Message.ShouldContain("Git.Git");
        result.Message.ShouldContain("installation completed via COM API");
    }

    #endregion

    #region UpgradePackageAsync Tests (第7イテレーション)

    [Fact]
    public async Task UpgradePackageAsync_ReturnsSuccessResult()
    {
        // Arrange
        var upgradeOptions = new UpgradeOptions { Id = "Git.Git" };
        
        // Act
        var result = await _client.UpgradePackageAsync(upgradeOptions);
        
        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.UsedComApi.ShouldBeTrue();
        result.Message.ShouldContain("Git.Git");
        result.Message.ShouldContain("upgrade completed via COM API");
    }

    #endregion

    #region UninstallPackageAsync Tests (第8イテレーション)

    [Fact]
    public async Task UninstallPackageAsync_ReturnsSuccessResult()
    {
        // Arrange
        var uninstallOptions = new UninstallOptions { Id = "Git.Git" };
        
        // Act
        var result = await _client.UninstallPackageAsync(uninstallOptions);
        
        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.UsedComApi.ShouldBeTrue();
        result.Message.ShouldContain("Git.Git");
        result.Message.ShouldContain("uninstall completed via COM API");
    }

    #endregion

    #region ListUpgradablePackagesAsync Tests (第9イテレーション)

    [Fact]
    public async Task ListUpgradablePackagesAsync_ReturnsEmptyList()
    {
        // Act
        var result = await _client.ListUpgradablePackagesAsync(new ListOptions());
        
        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    #endregion

    #region ManageSourceAsync Tests (第10イテレーション)

    [Fact]
    public async Task ManageSourceAsync_ReturnsSuccessResult()
    {
        // Arrange
        var sourceOptions = new SourceOptions { Name = "test-source" };
        
        // Act
        var result = await _client.ManageSourceAsync(SourceOperation.Add, sourceOptions);
        
        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.UsedComApi.ShouldBeTrue();
        result.Message.ShouldContain("Source Add completed via COM API");
    }

    #endregion

    #region ListSourcesAsync Tests (第11イテレーション)

    [Fact]
    public async Task ListSourcesAsync_ReturnsDefaultSources()
    {
        // Act
        var result = await _client.ListSourcesAsync();
        
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.Any(s => s.Name == "winget").ShouldBeTrue();
        result.Any(s => s.Name == "msstore").ShouldBeTrue();
    }

    #endregion

    #region Export/Import Tests (第12イテレーション)

    [Fact]
    public async Task ExportPackagesAsync_ThrowsNotImplementedException()
    {
        // Act & Assert
        await Should.ThrowAsync<NotImplementedException>(async () =>
            await _client.ExportPackagesAsync("test.json", new ExportOptions()));
    }

    [Fact]
    public async Task ImportPackagesAsync_ThrowsNotImplementedException()
    {
        // Act & Assert
        await Should.ThrowAsync<NotImplementedException>(async () =>
            await _client.ImportPackagesAsync("test.json", new ImportOptions()));
    }

    #endregion

    #region GetPackageDetailsAsync Tests (第13イテレーション)

    [Fact]
    public async Task GetPackageDetailsAsync_ReturnsNull()
    {
        // Act
        var result = await _client.GetPackageDetailsAsync("Git.Git");
        
        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Dispose Tests (第14イテレーション)

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => _client.Dispose());
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
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