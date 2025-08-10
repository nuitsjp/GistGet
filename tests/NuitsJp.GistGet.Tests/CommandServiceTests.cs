using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Services;
using NuitsJp.GistGet.Tests.Mocks;
using Shouldly;
using Xunit;
using Moq;
using NuitsJp.GistGet.Abstractions;

namespace NuitsJp.GistGet.Tests;

public class CommandServiceTests
{
    private readonly ILogger<CommandService> _logger;
    private readonly MockWinGetClient _mockWinGetClient;
    private readonly MockWinGetPassthroughClient _mockPassthroughClient;
    private readonly MockGistSyncService _mockGistSyncService;
    private readonly CommandService _commandService;

    public CommandServiceTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<CommandService>();
        _mockWinGetClient = new MockWinGetClient();
        _mockPassthroughClient = new MockWinGetPassthroughClient();
        _mockGistSyncService = new MockGistSyncService();
        
        var mockErrorMessageService = new Mock<IErrorMessageService>();
        _commandService = new CommandService(
            _mockWinGetClient,
            _mockPassthroughClient,
            _mockGistSyncService,
            _logger,
            mockErrorMessageService.Object);
    }

    [Theory]
    [InlineData("install")]
    [InlineData("uninstall")]
    [InlineData("upgrade")]
    public async Task ExecuteAsync_ShouldRouteToCOMClient_WhenUsingCOMCommands(string command)
    {
        // Arrange
        var args = new[] { command, "--id", "TestPackage" };

        // Act
        var result = await _commandService.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockWinGetClient.InitializeCalled.ShouldBeTrue();
        _mockWinGetClient.LastCommand.ShouldBe(command);
        _mockWinGetClient.LastArgs.ShouldBe(args);
    }

    [Theory]
    [InlineData("sync")]
    [InlineData("export")]
    [InlineData("import")]
    public async Task ExecuteAsync_ShouldRouteToGistService_WhenUsingGistCommands(string command)
    {
        // Arrange
        var args = new[] { command };

        // Act
        var result = await _commandService.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockGistSyncService.LastCommand.ShouldBe(command);
    }

    [Theory]
    [InlineData("list")]
    [InlineData("search")]
    [InlineData("show")]
    [InlineData("source")]
    [InlineData("settings")]
    public async Task ExecuteAsync_ShouldRouteToPassthrough_WhenUsingPassthroughCommands(string command)
    {
        // Arrange
        var args = new[] { command, "test-arg" };

        // Act
        var result = await _commandService.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockPassthroughClient.LastArgs.ShouldBe(args);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteToPassthrough_WhenNoArgs()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = await _commandService.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockPassthroughClient.LastArgs.ShouldBe(args);
    }

    // Phase 7.1: エラーハンドリング改善 - Red フェーズ（失敗するテストから開始）
    
    [Fact]
    public async Task ExecuteAsync_ShouldDisplayUserFriendlyMessage_WhenWinGetClientThrowsComException()
    {
        // Arrange
        var mockWinGetClient = new Mock<IWinGetClient>();
        var mockPassthroughClient = new Mock<IWinGetPassthroughClient>();
        var mockGistSyncService = new Mock<IGistSyncService>();
        var logger = new Mock<ILogger<CommandService>>();

        // COM例外をスローするように設定
        var comException = new System.Runtime.InteropServices.COMException("COM Error", -2147024891); // E_ACCESSDENIED
        mockWinGetClient.Setup(x => x.InitializeAsync()).ThrowsAsync(comException);
        
        var mockErrorMessageService = new Mock<IErrorMessageService>();
        var service = new CommandService(
            mockWinGetClient.Object,
            mockPassthroughClient.Object,
            mockGistSyncService.Object,
            logger.Object,
            mockErrorMessageService.Object);

        var args = new[] { "install", "--id", "TestPackage.Test" };

        // Act
        var result = await service.ExecuteAsync(args);

        // Assert
        result.ShouldBe(1); // エラー終了コード
        
        // ErrorMessageServiceのHandleComExceptionが呼び出されることを確認
        mockErrorMessageService.Verify(x => x.HandleComException(comException), Times.Once);
    }
    
    [Fact] 
    public async Task ExecuteAsync_ShouldDisplayUserFriendlyMessage_WhenPackageNotFound()
    {
        // Arrange
        var mockWinGetClient = new Mock<IWinGetClient>();
        var mockPassthroughClient = new Mock<IWinGetPassthroughClient>();
        var mockGistSyncService = new Mock<IGistSyncService>();
        var logger = new Mock<ILogger<CommandService>>();

        // パッケージ未発見例外をスローするように設定
        var packageNotFound = new InvalidOperationException("Package 'NonExistent.Package' not found");
        mockWinGetClient.Setup(x => x.InstallPackageAsync(It.IsAny<string[]>())).ThrowsAsync(packageNotFound);
        
        var mockErrorMessageService = new Mock<IErrorMessageService>();
        var service = new CommandService(
            mockWinGetClient.Object,
            mockPassthroughClient.Object,
            mockGistSyncService.Object,
            logger.Object,
            mockErrorMessageService.Object);

        var args = new[] { "install", "--id", "NonExistent.Package" };

        // Act
        var result = await service.ExecuteAsync(args);

        // Assert
        result.ShouldBe(1); // エラー終了コード
        
        // ErrorMessageServiceのHandlePackageNotFoundExceptionが呼び出されることを確認
        mockErrorMessageService.Verify(x => x.HandlePackageNotFoundException(packageNotFound), Times.Once);
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldDisplayHelpfulErrorMessage_WhenNetworkError()
    {
        // Arrange  
        var mockWinGetClient = new Mock<IWinGetClient>();
        var mockPassthroughClient = new Mock<IWinGetPassthroughClient>();
        var mockGistSyncService = new Mock<IGistSyncService>();
        var logger = new Mock<ILogger<CommandService>>();

        // ネットワーク例外をスローするように設定
        var networkError = new HttpRequestException("Unable to connect to the remote server");
        mockWinGetClient.Setup(x => x.InstallPackageAsync(It.IsAny<string[]>())).ThrowsAsync(networkError);
        
        var mockErrorMessageService = new Mock<IErrorMessageService>();
        var service = new CommandService(
            mockWinGetClient.Object,
            mockPassthroughClient.Object,
            mockGistSyncService.Object,
            logger.Object,
            mockErrorMessageService.Object);

        var args = new[] { "install", "--id", "TestPackage.Test" };

        // Act
        var result = await service.ExecuteAsync(args);

        // Assert
        result.ShouldBe(1); // エラー終了コード
        
        // ErrorMessageServiceのHandleNetworkExceptionが呼び出されることを確認
        mockErrorMessageService.Verify(x => x.HandleNetworkException(networkError), Times.Once);
    }
}

