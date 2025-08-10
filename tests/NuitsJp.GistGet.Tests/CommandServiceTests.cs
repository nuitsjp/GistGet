using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Services;
using NuitsJp.GistGet.Tests.Mocks;
using Shouldly;
using Xunit;

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
        
        _commandService = new CommandService(
            _mockWinGetClient,
            _mockPassthroughClient,
            _mockGistSyncService,
            _logger);
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
}

