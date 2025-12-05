using GistGet.Infrastructure.WinGet;
using GistGet.Infrastructure.OS;
using Microsoft.Management.Deployment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GistGet.Tests.Integration;

public class WinGetRepositoryIntegrationTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public WinGetRepositoryIntegrationTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly Guid PackageManagerClsid = new("C53A4F16-787E-42A4-B304-29EFFB4BF597");

    [Fact]
    public async Task GetInstalledPackagesAsync_WhenWingetComAvailable_ReturnsPackages()
    {

        var repository = new WinGetRepository(new ProcessRunner());

        var packages = await repository.GetInstalledPackagesAsync();

        Assert.NotNull(packages);
        Assert.NotEmpty(packages);
    }

    [Fact]
    public async Task GetPinnedPackagesAsync_ShouldReturnPinnedPackages_WhenPinsExist()
    {

        // 0. Setup: Reset pins
        RunWingetCommand("pin reset --force");

        var repository = new WinGetRepository(new ProcessRunner());

        // 1. Verify empty
        var initialPins = await repository.GetPinnedPackagesAsync();
        Assert.Empty(initialPins);

        // 2. Get installed packages to find candidates to pin
        var installedPackages = await repository.GetInstalledPackagesAsync();
        Assert.NotEmpty(installedPackages);

        // Debug: Log all installed packages
        _output.WriteLine($"Found {installedPackages.Count} installed packages:");
        foreach (var p in installedPackages.Values)
        {
            _output.WriteLine($" - {p.Id}");
        }

        // Pick 2 packages to pin. 
        // We prefer packages with simple IDs (no backslashes) as they are likely from WinGet source.
        // Also exclude packages starting with "ARP\" or "MSIX\" explicitly to be safe.
        var packagesToPin = installedPackages.Values
            .Where(p => !p.Id.Contains('\\') && !p.Id.StartsWith("ARP") && !p.Id.StartsWith("MSIX"))
            .Take(2)
            .ToList();

        // If not enough simple IDs, skip the test.
        if (packagesToPin.Count < 2)
        {
            _output.WriteLine("Warning: Not enough simple IDs found. Skipping test.");
            return;
        }
        
        // 3. Add pins
        foreach (var package in packagesToPin)
        {
            RunWingetCommand($"pin add --id {package.Id}");
        }

        // 4. Verify pins are present
        var currentPins = await repository.GetPinnedPackagesAsync();
        Assert.Equal(2, currentPins.Count);

        foreach (var package in packagesToPin)
        {
            Assert.Contains(package.Id, currentPins.Keys);
            // Note: Version comparison might be tricky if installed version differs from pinned version logic,
            // but usually pin list shows the pinned version.
            // Let's just verify the ID is present for now.
        }
    }

    private static void RunWingetCommand(string args)
    {
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        var wingetExe = System.IO.Path.Combine(localAppData!, "Microsoft", "WindowsApps", "winget.exe");

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = wingetExe,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        var stdout = process!.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Winget command failed: winget {args}.\nExit Code: {process.ExitCode}\nOutput: {stdout}\nError: {stderr}");
        }
    }
}
