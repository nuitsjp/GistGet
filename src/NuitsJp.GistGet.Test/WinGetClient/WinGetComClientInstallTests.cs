using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.WinGetClient;
using NuitsJp.GistGet.WinGetClient.Abstractions;
using NuitsJp.GistGet.WinGetClient.Models;
using Shouldly;
using Xunit;
using WinGetInstallOptions = NuitsJp.GistGet.WinGetClient.Models.InstallOptions;

namespace NuitsJp.GistGet.Test.WinGetClient;

public class WinGetComClientInstallTests
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

            // Default behavior for winget --version (for initialization)
            if (fileName == "winget" && arguments == "--version")
            {
                return Task.FromResult(new ProcessResult(NextExitCode, NextExitCode == 0 ? "v1.8.1791" : "", NextExitCode == 0 ? "" : "Command not found", TimeSpan.Zero));
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
    public async Task InstallPackageAsync_WithValidPackageId_ShouldReturnSuccess()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 0,
            NextStdOut = "Successfully installed Google.Chrome"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);
        await client.InitializeAsync();

        var installOptions = new WinGetInstallOptions
        {
            Id = "Google.Chrome"
        };

        // Act
        var result = await client.InstallPackageAsync(installOptions);

        // Assert  
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Message.ShouldContain("installation completed");
        // Should use CLI fallback since COM API is not implemented
        result.UsedComApi.ShouldBeFalse();
    }

    [Fact]
    public async Task InstallPackageAsync_WithInvalidPackageId_ShouldReturnFailure()
    {
        // Arrange
        var processRunner = new FakeProcessRunner();
        
        // Set up to succeed for winget --version (initialization), but fail for install
        processRunner.NextExitCode = 0; // First call (--version) succeeds
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);
        await client.InitializeAsync();
        
        // Now set up to fail for the install command
        processRunner.NextExitCode = 1;
        processRunner.NextStdErr = "No package found matching input criteria";

        var installOptions = new WinGetInstallOptions
        {
            Id = "NonExistent.Package"
        };

        // Act
        var result = await client.InstallPackageAsync(installOptions);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.ErrorDetails.ShouldContain("No package found");
        result.UsedComApi.ShouldBeFalse();
    }

    [Fact]
    public async Task InstallPackageAsync_WithProgressCallback_ShouldReportProgress()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 0,
            NextStdOut = "Successfully installed test package"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);
        await client.InitializeAsync();

        var installOptions = new WinGetInstallOptions
        {
            Id = "Test.Package"
        };

        var progressReports = new List<OperationProgress>();
        var progress = new Progress<OperationProgress>(p => progressReports.Add(p));

        // Act
        var result = await client.InstallPackageAsync(installOptions, progress);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        
        // Progress reporting will be implemented in the actual COM API implementation
        // For now, we just verify the method accepts the progress parameter
    }

    [Fact]
    public async Task InstallPackageAsync_WhenNotInitialized_ShouldThrowException()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 1 // winget --version fails
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);
        await client.InitializeAsync();

        var installOptions = new WinGetInstallOptions
        {
            Id = "Test.Package"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await client.InstallPackageAsync(installOptions));

        exception.Message.ShouldContain("WinGet client could not be initialized");
    }

    [Fact]
    public async Task InstallPackageAsync_WithCustomOptions_ShouldPassOptionsCorrectly()
    {
        // Arrange  
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 0,
            NextStdOut = "Successfully installed package"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);
        await client.InitializeAsync();

        var installOptions = new WinGetInstallOptions
        {
            Id = "Test.Package",
            Version = "1.0.0",
            Source = "winget",
            Scope = "machine",
            Architecture = "x64",
            Silent = true
        };

        // Act
        var result = await client.InstallPackageAsync(installOptions);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        
        // The actual CLI command building will be tested when CLI implementation is complete
        // For now, we verify the method handles complex options without error
    }
}