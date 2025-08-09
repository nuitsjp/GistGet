using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.WinGetClient;
using NuitsJp.GistGet.WinGetClient.Abstractions;
using NuitsJp.GistGet.WinGetClient.Models;
using Shouldly;
using Xunit;
using WinGetListOptions = NuitsJp.GistGet.WinGetClient.Models.ListOptions;

namespace NuitsJp.GistGet.Test.WinGetClient;

public class WinGetComClientListTests
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
                return Task.FromResult(new ProcessResult(0, "v1.8.1791", string.Empty, TimeSpan.Zero));
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
    public async Task ListInstalledPackagesAsync_ShouldReturnPackages()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 0,
            NextStdOut = "Name\tId\tVersion\nVisual Studio Code\tMicrosoft.VisualStudioCode\t1.75.0\nGoogle Chrome\tGoogle.Chrome\t109.0.5414.120"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);
        await client.InitializeAsync();

        var listOptions = new WinGetListOptions();

        // Act
        var result = await client.ListInstalledPackagesAsync(listOptions);

        // Assert  
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThan(0);
        processRunner.LastFileName.ShouldBe("winget");
        processRunner.LastArguments.ShouldBe("list");
    }

    [Fact]
    public async Task ListInstalledPackagesAsync_WithQuery_ShouldPassQueryToCommand()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 0,
            NextStdOut = "Name\tId\tVersion\nVisual Studio Code\tMicrosoft.VisualStudioCode\t1.75.0"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);
        await client.InitializeAsync();

        var listOptions = new WinGetListOptions
        {
            Query = "Visual Studio"
        };

        // Act
        var result = await client.ListInstalledPackagesAsync(listOptions);

        // Assert
        result.ShouldNotBeNull();
        processRunner.LastArguments.ShouldContain("\"Visual Studio\"");
    }

    [Fact]
    public async Task ListInstalledPackagesAsync_WithId_ShouldPassIdToCommand()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 0,
            NextStdOut = "Name\tId\tVersion\nVisual Studio Code\tMicrosoft.VisualStudioCode\t1.75.0"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);
        await client.InitializeAsync();

        var listOptions = new WinGetListOptions
        {
            Id = "Microsoft.VisualStudioCode"
        };

        // Act
        var result = await client.ListInstalledPackagesAsync(listOptions);

        // Assert
        result.ShouldNotBeNull();
        processRunner.LastArguments.ShouldContain("--id \"Microsoft.VisualStudioCode\"");
    }

    [Fact]
    public async Task ListInstalledPackagesAsync_WithUpgradeAvailable_ShouldIncludeUpgradeFlag()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 0,
            NextStdOut = "Name\tId\tVersion\tAvailable\nOutdated App\tCompany.OutdatedApp\t1.0.0\t2.0.0"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);
        await client.InitializeAsync();

        var listOptions = new WinGetListOptions
        {
            UpgradeAvailable = true
        };

        // Act
        var result = await client.ListInstalledPackagesAsync(listOptions);

        // Assert
        result.ShouldNotBeNull();
        processRunner.LastArguments.ShouldContain("--upgrade-available");
    }

    [Fact]
    public async Task ListInstalledPackagesAsync_WithProgressCallback_ShouldReportProgress()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 0,
            NextStdOut = "Name\tId\tVersion\nTest App\tTest.App\t1.0.0"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);
        await client.InitializeAsync();

        var listOptions = new WinGetListOptions();
        var progressReports = new List<OperationProgress>();
        var progress = new Progress<OperationProgress>(p => progressReports.Add(p));

        // Act
        var result = await client.ListInstalledPackagesAsync(listOptions, progress);

        // Assert
        result.ShouldNotBeNull();
        progressReports.Count.ShouldBeGreaterThan(0);
        
        // Should have progress reports for different phases
        progressReports.ShouldContain(p => p.Phase == "Listing");
        progressReports.ShouldContain(p => p.Phase == "Completed");
    }

    [Fact]
    public async Task ListInstalledPackagesAsync_WhenWingetFails_ShouldReturnEmptyList()
    {
        // Arrange
        var processRunner = new FakeProcessRunner
        {
            NextExitCode = 1,
            NextStdErr = "An error occurred"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, processRunner);
        await client.InitializeAsync();

        var listOptions = new WinGetListOptions();

        // Act
        var result = await client.ListInstalledPackagesAsync(listOptions);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }
}