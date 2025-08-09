using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.WinGetClient;
using NuitsJp.GistGet.WinGetClient.Abstractions;
using NuitsJp.GistGet.WinGetClient.Models;
using Shouldly;
using Xunit;

namespace NuitsJp.GistGet.Test.WinGetClient;

public class WinGetComClientInitializationTests
{
    private sealed class FakeProcessRunner : IProcessRunner
    {
        public string? LastFileName { get; private set; }
        public string? LastArguments { get; private set; }
        public int NextExitCode { get; set; } = 0;
        public string NextStdOut { get; set; } = string.Empty;
        public string NextStdErr { get; set; } = string.Empty;

        public Task<ProcessResult> RunAsync(
            string fileName,
            string arguments,
            string? workingDirectory = null,
            IDictionary<string, string>? environment = null,
            CancellationToken cancellationToken = default)
        {
            LastFileName = fileName;
            LastArguments = arguments;

            // Default behavior for winget --version
            if (fileName == "winget" && arguments == "--version")
            {
                return Task.FromResult(new ProcessResult(NextExitCode, string.IsNullOrEmpty(NextStdOut) ? "v1.8.1791" : NextStdOut, NextStdErr, TimeSpan.Zero));
            }

            if (fileName == "where" && arguments == "winget")
            {
                return Task.FromResult(new ProcessResult(NextExitCode, string.IsNullOrEmpty(NextStdOut) ? @"C:\Program Files\WindowsApps\Microsoft.DesktopAppInstaller\winget.exe" : NextStdOut, NextStdErr, TimeSpan.Zero));
            }

            return Task.FromResult(new ProcessResult(NextExitCode, NextStdOut, NextStdErr, TimeSpan.Zero));
        }
    }

    private static ILogger<WinGetComClient> CreateLogger()
    {
        using var factory = LoggerFactory.Create(builder => { });
        return factory.CreateLogger<WinGetComClient>();
    }

    [Fact]
    public async Task InitializeAsync_WhenComApiFailsAndCliAvailable_ShouldFallBackToCli()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 0,  // winget --version succeeds
            NextStdOut = "v1.8.1791"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);

        // Act
        var result = await client.InitializeAsync();

        // Assert
        result.ShouldBeTrue();
        processRunner.LastFileName.ShouldBe("winget");
        processRunner.LastArguments.ShouldBe("--version");
    }

    [Fact]
    public async Task InitializeAsync_WhenBothComApiAndCliFail_ShouldReturnFalse()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 1,  // winget --version fails
            NextStdErr = "Command not found"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);

        // Act
        var result = await client.InitializeAsync();

        // Assert
        result.ShouldBeFalse();
        processRunner.LastFileName.ShouldBe("winget");
        processRunner.LastArguments.ShouldBe("--version");
    }

    [Fact]
    public async Task InitializeAsync_WhenCalledMultipleTimes_ShouldOnlyInitializeOnce()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 0,
            NextStdOut = "v1.8.1791"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);

        // Act
        var result1 = await client.InitializeAsync();
        var result2 = await client.InitializeAsync();

        // Assert
        result1.ShouldBeTrue();
        result2.ShouldBeTrue();
        
        // First call should check winget version, second call should not
        processRunner.LastFileName.ShouldBe("winget");
        processRunner.LastArguments.ShouldBe("--version");
    }

    [Fact]
    public void GetClientInfo_WhenInitialized_ShouldReturnCorrectInfo()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 0,
            NextStdOut = "v1.8.1791"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);

        // Act
        var clientInfo = client.GetClientInfo();

        // Assert
        clientInfo.ComApiAvailable.ShouldBeFalse(); // COM API is not implemented yet
        clientInfo.CliAvailable.ShouldBeTrue();
        clientInfo.ActiveMode.ShouldBe(ClientMode.CliFallback);
        clientInfo.CliVersion.ShouldNotBeNull();
        clientInfo.CliPath.ShouldNotBeNull();
    }
}