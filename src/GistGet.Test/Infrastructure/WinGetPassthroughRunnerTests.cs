using System.Diagnostics;
using GistGet.Infrastructure;
using Moq;
using Shouldly;

namespace GistGet.Test.Infrastructure;

public class WinGetPassthroughRunnerTests
{
    [Fact]
    public async Task RunAsync_UsesWingetFromPathAndPassesArguments()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var tempDir = Directory.CreateTempSubdirectory();
        var wingetPath = Path.Combine(tempDir.FullName, "winget.exe");
        await File.WriteAllTextAsync(wingetPath, string.Empty);

        var originalPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", $"{tempDir.FullName}{Path.PathSeparator}{originalPath}");

        var processRunner = new Mock<IProcessRunner>();
        ProcessStartInfo? captured = null;
        processRunner
            .Setup(x => x.RunAsync(It.IsAny<ProcessStartInfo>()))
            .Callback<ProcessStartInfo>(info => captured = info)
            .ReturnsAsync(27);

        var target = new WinGetPassthroughRunner(processRunner.Object);

        try
        {
            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await target.RunAsync(["install", "Test.Package"]);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(27);
            captured.ShouldNotBeNull();
            // cmd.exe is used to execute winget for proper shell environment
            captured!.FileName.ShouldBe("cmd.exe");
            captured.Arguments.ShouldContain(wingetPath);
            captured.Arguments.ShouldContain("install");
            captured.Arguments.ShouldContain("Test.Package");
            captured.UseShellExecute.ShouldBeFalse();
            captured.CreateNoWindow.ShouldBeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
            tempDir.Delete(true);
        }
    }

    [Fact]
    public async Task RunAsync_WhenWingetNotInPath_UsesLocalAppDataFallback()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var tempDir = Directory.CreateTempSubdirectory();
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        var originalLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        Environment.SetEnvironmentVariable("PATH", tempDir.FullName);
        Environment.SetEnvironmentVariable("LOCALAPPDATA", tempDir.FullName);

        var expectedWingetPath = Path.Combine(tempDir.FullName, "Microsoft", "WindowsApps", "winget.exe");

        var processRunner = new Mock<IProcessRunner>();
        ProcessStartInfo? captured = null;
        processRunner
            .Setup(x => x.RunAsync(It.IsAny<ProcessStartInfo>()))
            .Callback<ProcessStartInfo>(info => captured = info)
            .ReturnsAsync(0);

        var target = new WinGetPassthroughRunner(processRunner.Object);

        try
        {
            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await target.RunAsync([]);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            captured.ShouldNotBeNull();
            // cmd.exe is used to execute winget for proper shell environment
            captured!.FileName.ShouldBe("cmd.exe");
            captured.Arguments.ShouldContain(expectedWingetPath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
            Environment.SetEnvironmentVariable("LOCALAPPDATA", originalLocalAppData);
            tempDir.Delete(true);
        }
    }
}
