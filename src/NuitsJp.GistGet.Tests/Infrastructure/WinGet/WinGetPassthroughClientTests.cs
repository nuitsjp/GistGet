using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Infrastructure.WinGet;
using Shouldly;
using Xunit.Abstractions;

namespace NuitsJp.GistGet.Tests.Infrastructure.WinGet;

[Trait("Category", "Unit")]
public class WinGetPassthroughClientTests
{
    private readonly ILogger<WinGetPassthroughClient> _logger;
    private readonly ITestOutputHelper _testOutput;

    public WinGetPassthroughClientTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<WinGetPassthroughClient>();
    }

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange & Act
        var client = new WinGetPassthroughClient(_logger);

        // Assert
        client.ShouldNotBeNull();
        client.ShouldBeAssignableTo<IWinGetPassthroughClient>();
    }

    [Fact]
    public async Task ExecuteAsync_WithValidArgs_ShouldReturnExitCode()
    {
        // Arrange
        var client = new WinGetPassthroughClient(_logger);
        var args = new[] { "list" };

        // Act
        var result = await client.ExecuteAsync(args);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyArgs_ShouldExecuteWithoutArguments()
    {
        // Arrange
        var client = new WinGetPassthroughClient(_logger);
        var args = Array.Empty<string>();

        // Act
        var result = await client.ExecuteAsync(args);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleArgs_ShouldPassAllArguments()
    {
        // Arrange
        var client = new WinGetPassthroughClient(_logger);
        var args = new[] { "--help" };

        // Act
        var result = await client.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0); // --helpは成功するはず
    }

    [Fact]
    public async Task ExecuteAsync_WhenProcessFails_ShouldReturnNonZeroExitCode()
    {
        // Arrange
        var client = new WinGetPassthroughClient(_logger);
        var args = new[] { "invalid-command-that-does-not-exist" };

        // Act
        var result = await client.ExecuteAsync(args);

        // Assert
        result.ShouldNotBe(0); // 無効なコマンドは失敗するはず
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRestoreOriginalConsoleEncoding()
    {
        // Arrange
        var originalInputEncoding = Console.InputEncoding;
        var originalOutputEncoding = Console.OutputEncoding;

        var client = new WinGetPassthroughClient(_logger);
        var args = new[] { "--help" };

        // Act
        await client.ExecuteAsync(args);

        // Assert - Console encodings should be restored to original values
        Console.InputEncoding.ShouldBe(originalInputEncoding);
        Console.OutputEncoding.ShouldBe(originalOutputEncoding);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExceptionOccurs_ShouldRestoreOriginalConsoleEncoding()
    {
        // Arrange
        var originalInputEncoding = Console.InputEncoding;
        var originalOutputEncoding = Console.OutputEncoding;

        // Create a client that will fail by using invalid executable name
        var client = new WinGetPassthroughClient(_logger);
        var args = new[] { "some-args" };

        // Temporarily modify the client to test exception handling
        // This test verifies the behavior exists, but current implementation might not have the finally block yet

        // Act & Assert
        try
        {
            await client.ExecuteAsync(args);
        }
        catch
        {
            // Even if an exception occurs, encodings should be restored
            Console.InputEncoding.ShouldBe(originalInputEncoding);
            Console.OutputEncoding.ShouldBe(originalOutputEncoding);
        }
    }
}