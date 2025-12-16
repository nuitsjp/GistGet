using System.Diagnostics;
using GistGet.Infrastructure.Diagnostics;
using Shouldly;

namespace GistGet.Test.Infrastructure.Diagnostics;

public class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_ReturnsProcessExitCode()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var target = new ProcessRunner();
        var startInfo = new ProcessStartInfo("cmd.exe", "/c exit 7")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        var exitCode = await target.RunAsync(startInfo);

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        exitCode.ShouldBe(7);
    }
}
