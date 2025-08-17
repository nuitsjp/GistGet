using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using System.Collections.Generic;
using System.Linq;
using System;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    AbsolutePath BuildScriptsDir => RootDirectory / "build-scripts";
    AbsolutePath SolutionFile => RootDirectory / "GistGet.slnx";
    AbsolutePath TestResultsDirectory => RootDirectory / ".reports" / "test-results" / "trx";

    Target Clean => _ => _
        .Description("Clean build artifacts")
        .Executes(() =>
        {
            var cleanedItems = new List<string>();

            // Remove bin and obj directories
            Log.Information("Removing bin and obj directories...");
            var binDirectories = RootDirectory.GlobDirectories("**/bin").Where(d => d.DirectoryExists());
            var objDirectories = RootDirectory.GlobDirectories("**/obj").Where(d => d.DirectoryExists());
            
            foreach (var dir in binDirectories)
            {
                try
                {
                    dir.DeleteDirectory();
                    cleanedItems.Add($"bin: {dir}");
                }
                catch (Exception ex)
                {
                    Log.Warning($"Could not delete {dir}: {ex.Message}");
                }
            }
            
            foreach (var dir in objDirectories)
            {
                try
                {
                    dir.DeleteDirectory();
                    cleanedItems.Add($"obj: {dir}");
                }
                catch (Exception ex)
                {
                    Log.Warning($"Could not delete {dir}: {ex.Message}");
                }
            }

            // Remove coverage files and reports
            Log.Information("Removing coverage artifacts...");
            
            var coverageReportDir = RootDirectory / "coverage-report";
            if (coverageReportDir.DirectoryExists())
            {
                try
                {
                    coverageReportDir.DeleteDirectory();
                    cleanedItems.Add("Coverage report directory");
                }
                catch (Exception ex)
                {
                    Log.Warning($"Could not delete coverage report directory: {ex.Message}");
                }
            }

            var coverageFiles = RootDirectory.GlobFiles("**/coverage.*.xml").Where(f => f.FileExists());
            foreach (var file in coverageFiles)
            {
                try
                {
                    file.DeleteFile();
                    cleanedItems.Add($"Coverage file: {file.Name}");
                }
                catch (Exception ex)
                {
                    Log.Warning($"Could not delete {file}: {ex.Message}");
                }
            }

            var testResultsDirs = RootDirectory.GlobDirectories("**/TestResults").Where(d => d.DirectoryExists());
            foreach (var dir in testResultsDirs)
            {
                try
                {
                    dir.DeleteDirectory();
                    cleanedItems.Add($"TestResults: {dir}");
                }
                catch (Exception ex)
                {
                    Log.Warning($"Could not delete {dir}: {ex.Message}");
                }
            }

            // Remove inspection files
            Log.Information("Removing inspection artifacts...");
            var inspectionFile = RootDirectory / "inspection-results.xml";
            if (inspectionFile.FileExists())
            {
                try
                {
                    inspectionFile.DeleteFile();
                    cleanedItems.Add("Inspection results file");
                }
                catch (Exception ex)
                {
                    Log.Warning($"Could not delete inspection file: {ex.Message}");
                }
            }

            // Remove .reports directory
            Log.Information("Removing .reports directory...");
            var reportsDir = RootDirectory / ".reports";
            if (reportsDir.DirectoryExists())
            {
                try
                {
                    reportsDir.DeleteDirectory();
                    cleanedItems.Add(".reports directory");
                }
                catch (Exception ex)
                {
                    Log.Warning($"Could not delete .reports directory: {ex.Message}");
                }
            }

            // Summary
            Log.Information($"Clean completed! Cleaned {cleanedItems.Count} items.");
        });

    Target FormatCheck => _ => _
        .Description("Check code formatting without making changes")
        .Executes(() =>
        {
            Log.Information("Checking code formatting...");
            
            try
            {
                // Check if dotnet-format is installed
                var toolListResult = ProcessTasks.StartProcess("dotnet", "tool list -g").AssertZeroExitCode();
                if (!toolListResult.Output.Any(line => line.Text.Contains("dotnet-format")))
                {
                    Log.Information("Installing dotnet-format...");
                    ProcessTasks.StartProcess("dotnet", "tool install -g dotnet-format").AssertZeroExitCode();
                }

                // Set environment variable for Windows targeting  
                EnvironmentInfo.SetVariable("EnableWindowsTargeting", "true");

                // Restore dependencies
                Log.Information("Restoring dependencies...");
                DotNetRestore(s => s
                    .SetProjectFile(SolutionFile)
                    .SetProperty("EnableWindowsTargeting", "true")
                    .SetVerbosity(DotNetVerbosity.quiet));

                // Run dotnet format with verify-no-changes
                Log.Information("Running format verification...");
                var formatArgs = $"format \"{SolutionFile}\" --verbosity diagnostic --verify-no-changes --no-restore";
                ProcessTasks.StartProcess("dotnet", formatArgs).AssertZeroExitCode();

                // Run code analysis
                Log.Information("Running code analysis...");
                DotNetBuild(s => s
                    .SetProjectFile(SolutionFile)
                    .SetNoIncremental(true)
                    .SetProperty("EnforceCodeStyleInBuild", "true")
                    .SetProperty("EnableWindowsTargeting", "true")
                    .SetVerbosity(DotNetVerbosity.quiet));

                Log.Information("✅ All formatting checks passed! Your code is ready for commit.");
            }
            catch (Exception)
            {
                Log.Error("❌ Code formatting issues detected! Run FormatFix to apply formatting fixes.");
                throw;
            }
        });

    Target FormatFix => _ => _
        .Description("Apply code formatting fixes")
        .Executes(() =>
        {
            Log.Information("Applying code formatting fixes...");
            
            try
            {
                // Check if dotnet-format is installed
                var toolListResult = ProcessTasks.StartProcess("dotnet", "tool list -g").AssertZeroExitCode();
                if (!toolListResult.Output.Any(line => line.Text.Contains("dotnet-format")))
                {
                    Log.Information("Installing dotnet-format...");
                    ProcessTasks.StartProcess("dotnet", "tool install -g dotnet-format").AssertZeroExitCode();
                }

                // Set environment variable for Windows targeting
                EnvironmentInfo.SetVariable("EnableWindowsTargeting", "true");

                // Restore dependencies
                Log.Information("Restoring dependencies...");
                DotNetRestore(s => s
                    .SetProjectFile(SolutionFile)
                    .SetProperty("EnableWindowsTargeting", "true")
                    .SetVerbosity(DotNetVerbosity.quiet));

                // Run dotnet format
                Log.Information("Applying code formatting...");
                var formatArgs = $"format \"{SolutionFile}\" --verbosity diagnostic --no-restore";
                ProcessTasks.StartProcess("dotnet", formatArgs).AssertZeroExitCode();

                Log.Information("✅ Code formatting applied successfully! Review the changes and commit when ready.");
            }
            catch (Exception)
            {
                Log.Error("❌ Failed to apply formatting!");
                throw;
            }
        });

    Target Setup => _ => _
        .Description("Install required tools (ReSharper CLI, ReportGenerator, dotnet-format)")
        .Executes(() =>
        {
            var arguments = $@"-File ""{BuildScriptsDir / "Setup.ps1"}""";
            ProcessTasks.StartProcess("powershell", arguments).AssertZeroExitCode();
        });

    Target Compile => _ => _
        .Description("Build the solution using dotnet build")
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(SolutionFile)
                .SetConfiguration("Release")
                .SetVerbosity(DotNetVerbosity.minimal));
        });

    Target Test => _ => _
        .Description("Run tests without coverage collection")
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(SolutionFile)
                .SetConfiguration("Release")
                .SetVerbosity(DotNetVerbosity.normal)
                .SetNoBuild(true));
        });

    Target Coverage => _ => _
        .Description("Run tests with coverage collection and generate report")
        .DependsOn(Compile)
        .Executes(() =>
        {
            TestResultsDirectory.CreateOrCleanDirectory();
            
            DotNetTest(s => s
                .SetProjectFile(SolutionFile)
                .SetConfiguration("Release")
                .SetVerbosity(DotNetVerbosity.normal)
                .SetResultsDirectory(TestResultsDirectory)
                .SetLoggers("trx")
                .SetNoBuild(true)
                .SetDataCollector("XPlat Code Coverage"));

            var arguments = $@"-File ""{BuildScriptsDir / "Coverage.ps1"}"" -ShowSummary";
            ProcessTasks.StartProcess("powershell", arguments).AssertZeroExitCode();
        });

    Target CodeInspection => _ => _
        .Description("Run ReSharper code inspection")
        .DependsOn(Compile)
        .Executes(() =>
        {
            var arguments = $@"-File ""{BuildScriptsDir / "CodeInspection.ps1"}"" -ShowSummary";
            ProcessTasks.StartProcess("powershell", arguments).AssertZeroExitCode();
        });

    Target Full => _ => _
        .Description("Setup + Build + Test + Coverage + CodeInspection")
        .DependsOn(Setup, Coverage, CodeInspection);
}