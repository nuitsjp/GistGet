using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.WinGetClient;
using NuitsJp.GistGet.WinGetClient.Abstractions;
using NuitsJp.GistGet.WinGetClient.Models;
using Shouldly;

namespace NuitsJp.GistGet.Test.WinGetClient;

public class ExportCliTests
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

            // Simulate winget --version success by default
            if (arguments.StartsWith("--version"))
            {
                return Task.FromResult(new ProcessResult(0, "1.9.0", string.Empty, TimeSpan.Zero));
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
    public async Task Should_Export_Success_CLI()
    {
        // Arrange
        var fake = new FakeProcessRunner
        {
            NextExitCode = 0,
            NextStdOut = "Exported",
            NextStdErr = string.Empty
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, fake);

        // Act
        var result = await client.ExportPackagesAsync(
            outputPath: "packages.json",
            options: new ExportOptions { IncludeVersions = true, AcceptSourceAgreements = true },
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.UsedComApi.ShouldBeFalse();
        fake.LastFileName.ShouldBe("winget");
        fake.LastArguments.ShouldNotBeNull();
        fake.LastArguments!.ShouldContain("export");
        fake.LastArguments.ShouldContain("-o \"packages.json\"");
        fake.LastArguments.ShouldContain("--include-versions");
        fake.LastArguments.ShouldContain("--accept-source-agreements");
    }

    [Fact]
    public async Task Should_Export_Fail_When_CLI_Fails()
    {
        // Arrange
        var fake = new FakeProcessRunner
        {
            NextExitCode = 1,
            NextStdOut = string.Empty,
            NextStdErr = "boom"
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, fake);

        // Act
        var result = await client.ExportPackagesAsync(
            outputPath: "packages.json",
            options: new ExportOptions(),
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ExitCode.ShouldBe(1);
        fake.LastFileName.ShouldBe("winget");
        fake.LastArguments.ShouldNotBeNull();
        fake.LastArguments!.ShouldContain("export");
    }
}
