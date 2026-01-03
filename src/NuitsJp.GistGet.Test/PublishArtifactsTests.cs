using System.Diagnostics;
using Shouldly;

namespace NuitsJp.GistGet.Test;

[Collection("Sequential")]
public class PublishArtifactsTests
{
    // Find repository root by searching for .git directory
    protected static readonly string RepositoryRoot = FindRepositoryRoot();

    protected static readonly string LauncherProjectPath = Path.Combine(RepositoryRoot, "src", "GistGet", "GistGet.csproj");
    protected static readonly string CoreProjectPath = Path.Combine(RepositoryRoot, "src", "NuitsJp.GistGet", "NuitsJp.GistGet.csproj");
    protected static readonly string PublishRoot = Path.Combine(Path.GetTempPath(), "GistGetPublishTests");

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not find repository root (no .git directory found)");
    }

    public class PortablePackage : PublishArtifactsTests
    {
        [Fact]
        public void MissingLocalDll_ShouldStillRunExecutableAndKeepWinGetInteropBeside()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var publishDirectory = Path.Combine(PublishRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(publishDirectory);

            try
            {
                // Publish NuitsJp.GistGet.exe (core) first
                var corePublishResult = RunProcess(
                    "dotnet",
                    new[]
                    {
                        "publish",
                        CoreProjectPath,
                        "-c",
                        "Release",
                        "-r",
                        "win-x64",
                        "-o",
                        publishDirectory
                    });

                corePublishResult.ExitCode.ShouldBe(0, corePublishResult.ToString());

                // Publish GistGet.exe (launcher) to the same directory
                var launcherPublishResult = RunProcess(
                    "dotnet",
                    new[]
                    {
                        "publish",
                        LauncherProjectPath,
                        "-c",
                        "Release",
                        "-r",
                        "win-x64",
                        "-o",
                        publishDirectory
                    });

                launcherPublishResult.ExitCode.ShouldBe(0, launcherPublishResult.ToString());

                // Verify both executables exist
                var launcherPath = Path.Combine(publishDirectory, "GistGet.exe");
                File.Exists(launcherPath).ShouldBeTrue("GistGet.exe (launcher) should exist");

                var corePath = Path.Combine(publishDirectory, "NuitsJp.GistGet.exe");
                File.Exists(corePath).ShouldBeTrue("NuitsJp.GistGet.exe (core) should exist");

                // Verify WinGet COM interop DLLs are present
                var winGetInteropFiles = new[]
                {
                    "Microsoft.Management.Deployment.CsWinRTProjection.dll",
                    "Microsoft.Management.Deployment.dll",
                    "Microsoft.Management.Deployment.winmd",
                    "WinRT.Runtime.dll"
                };

                foreach (var fileName in winGetInteropFiles)
                {
                    var fullPath = Path.Combine(publishDirectory, fileName);
                    File.Exists(fullPath).ShouldBeTrue($"{fileName} should be present beside the executable");
                }

                // -------------------------------------------------------------------
                // Act
                // -------------------------------------------------------------------
                var runResult = RunProcess(launcherPath, new[] { "--version" }, publishDirectory);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                runResult.ExitCode.ShouldBe(0, runResult.ToString());
                runResult.StandardOutput.ShouldNotBeNullOrWhiteSpace();
            }
            finally
            {
                if (Directory.Exists(publishDirectory))
                {
                    Directory.Delete(publishDirectory, true);
                }
            }
        }
    }

    protected static ProcessResult RunProcess(string fileName, IEnumerable<string> arguments, string? workingDirectory = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process();
        process.StartInfo = startInfo;

        process.Start();

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();

        process.WaitForExit();

        return new ProcessResult(process.ExitCode, standardOutput.Trim(), standardError.Trim());
    }

    protected readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError)
    {
        public override string ToString()
        {
            return $"ExitCode: {ExitCode}, StdOut: {StandardOutput}, StdErr: {StandardError}";
        }
    }
}




