using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.WinGetClient;
using NuitsJp.GistGet.WinGetClient.Abstractions;
using NuitsJp.GistGet.WinGetClient.Models;
using Shouldly;

namespace NuitsJp.GistGet.Test.WinGetClient;

public class ImportCliTests
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
    public async Task Should_Import_Success_CLI()
    {
        // Arrange
        var fake = new FakeProcessRunner
        {
            NextExitCode = 0,
            NextStdOut = "Imported",
            NextStdErr = string.Empty
        };
        var logger = CreateLogger();
        var client = new WinGetComClient(logger, fake);

        // Act
        var result = await client.ImportPackagesAsync(
            inputPath: "packages.json",
            options: new ImportOptions
            {
                IgnoreUnavailable = true,
                IgnoreVersions = true,
                AcceptPackageAgreements = true,
                AcceptSourceAgreements = true
            },
            progress: null,
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.UsedComApi.ShouldBeFalse();
        fake.LastFileName.ShouldBe("winget");
        fake.LastArguments.ShouldNotBeNull();
        fake.LastArguments!.ShouldContain("import");
        fake.LastArguments.ShouldContain("-i \"packages.json\"");
        fake.LastArguments.ShouldContain("--ignore-unavailable");
        fake.LastArguments.ShouldContain("--ignore-versions");
        fake.LastArguments.ShouldContain("--accept-package-agreements");
        fake.LastArguments.ShouldContain("--accept-source-agreements");
    }

    [Fact]
    public async Task Should_Import_Fail_When_CLI_Fails()
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
        var result = await client.ImportPackagesAsync(
            inputPath: "packages.json",
            options: new ImportOptions(),
            progress: null,
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ExitCode.ShouldBe(1);
        fake.LastFileName.ShouldBe("winget");
        fake.LastArguments.ShouldNotBeNull();
        fake.LastArguments!.ShouldContain("import");
    }
}
