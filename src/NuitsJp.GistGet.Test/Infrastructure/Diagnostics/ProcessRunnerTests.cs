using System.Diagnostics;
using NuitsJp.GistGet.Infrastructure.Diagnostics;
using Shouldly;

namespace NuitsJp.GistGet.Test.Infrastructure.Diagnostics;

public class ProcessRunnerTests
{
    public class RunAndCapture : ProcessRunnerTests
    {
        [Fact]
        public void NonZeroExit_CapturesBothStreamsAndExitCode()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = new ProcessRunner();
            var startInfo = new ProcessStartInfo("cmd.exe", "/c echo standard output& echo standard error 1>&2& exit 17")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = target.RunAndCapture(startInfo);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ExitCode.ShouldBe(17);
            result.StandardOutput.Trim().ShouldBe("standard output");
            result.StandardError.Trim().ShouldBe("standard error");
        }

        [Fact]
        public async Task LargeOutputOnBothStreams_CompletesWithoutDeadlock()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = new ProcessRunner();
            var startInfo = new ProcessStartInfo("cmd.exe", "/c for /l %i in (1,1,4096) do @(echo stdout-%i& echo stderr-%i 1>&2)")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await Task.Run(() => target.RunAndCapture(startInfo)).WaitAsync(TimeSpan.FromSeconds(15));

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ExitCode.ShouldBe(0);
            result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length.ShouldBe(4096);
            result.StandardError.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length.ShouldBe(4096);
            result.StandardOutput.ShouldContain("stdout-4096");
            result.StandardError.ShouldContain("stderr-4096");
        }
    }

    public class RunAsync : ProcessRunnerTests
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
}




