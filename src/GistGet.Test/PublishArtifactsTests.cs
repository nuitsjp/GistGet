using System.Diagnostics;
using Shouldly;

namespace GistGet.Test;

public class PublishArtifactsTests
{
    protected static readonly string ProjectPath = Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "..",
            "GistGet",
            "GistGet.csproj"));

    protected static readonly string PublishRoot = Path.Combine(Path.GetTempPath(), "GistGetPublishTests");

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
                var publishResult = RunProcess(
                    "dotnet",
                    new[]
                    {
                        "publish",
                        ProjectPath,
                        "-c",
                        "Release",
                        "-r",
                        "win-x64",
                        "-o",
                        publishDirectory
                    });

                publishResult.ExitCode.ShouldBe(0, publishResult.ToString());

                var executablePath = Path.Combine(publishDirectory, "GistGet.exe");
                File.Exists(executablePath).ShouldBeTrue();

                var dllPath = Path.Combine(publishDirectory, "GistGet.dll");
                if (File.Exists(dllPath))
                {
                    File.Delete(dllPath);
                }

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
                var runResult = RunProcess(executablePath, new[] { "--version" }, publishDirectory);

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
