using System;
using System.Threading.Tasks;
using GistGet.Infrastructure.OS;
using GistGet.Infrastructure.WinGet;
using GistGet.Models;
using Moq;
using Xunit;

namespace GistGet.Tests.Infrastructure.WinGet;

public class WinGetExecutorTests
{
    private readonly Mock<IProcessRunner> _processRunner = new();
    private readonly WinGetExecutor _executor;

    public WinGetExecutorTests()
    {
        _executor = new WinGetExecutor(_processRunner.Object);
    }

    [Fact]
    public async Task InstallPackageAsync_UsesPassthroughAndReturnsSuccessOnZeroExitCode()
    {
        var package = new GistGetPackage { Id = "Test.Package", Silent = true };
        _processRunner.Setup(r => r.RunPassthroughAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(0);

        var result = await _executor.InstallPackageAsync(package);

        Assert.True(result);
        _processRunner.Verify(r => r.RunPassthroughAsync(
            It.Is<string>(p => p.EndsWith("winget.exe")),
            It.Is<string>(args => args.Contains("install") && args.Contains("--id Test.Package"))), Times.Once);
    }

    [Fact]
    public async Task InstallPackageAsync_ReturnsFalseOnNonZeroExitCode()
    {
        var package = new GistGetPackage { Id = "Test.Package" };
        _processRunner.Setup(r => r.RunPassthroughAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(1);

        var result = await _executor.InstallPackageAsync(package);

        Assert.False(result);
    }

    [Fact]
    public async Task InstallPackageAsync_ThrowsWhenIdIsMissing()
    {
        var package = new GistGetPackage { Id = "" };

        await Assert.ThrowsAsync<ArgumentException>(() => _executor.InstallPackageAsync(package));
    }

    [Fact]
    public async Task RunPassthroughAsync_ReturnsExitCode()
    {
        _processRunner.Setup(r => r.RunPassthroughAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(5);

        var exitCode = await _executor.RunPassthroughAsync("list", []);

        Assert.Equal(5, exitCode);
    }
}
