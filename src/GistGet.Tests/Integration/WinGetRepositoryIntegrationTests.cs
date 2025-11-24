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
    private static readonly Guid PackageManagerClsid = new("C53A4F16-787E-42A4-B304-29EFFB4BF597");

    [Fact]
    public async Task GetInstalledPackagesAsync_WhenWingetComAvailable_ReturnsPackages()
    {
        if (!IsWingetComAvailable())
        {
            return;
        }

        var repository = new WinGetRepository(new ProcessRunner());

        var packages = await repository.GetInstalledPackagesAsync();

        Assert.NotNull(packages);
        Assert.NotEmpty(packages);
    }

    [Fact]
    public async Task GetPinnedPackagesAsync_ShouldReturnPinnedPackages_WhenPinsExist()
    {
        if (!IsWingetComAvailable())
        {
            return;
        }

        // 0. Setup: Reset pins
        RunWingetCommand("pin reset --force");

        var repository = new WinGetRepository(new ProcessRunner());

        // 1. Verify empty
        var initialPins = await repository.GetPinnedPackagesAsync();
        Assert.Empty(initialPins);

        // 2. Get installed packages to find candidates to pin
        var installedPackages = await repository.GetInstalledPackagesAsync();
        Assert.NotEmpty(installedPackages);

        // Pick 2 packages to pin. 
        // We prefer packages with simple IDs if possible, but taking any 2 should work.
        var packagesToPin = installedPackages.Values.Take(2).ToList();
        Assert.True(packagesToPin.Count >= 2, "Need at least 2 installed packages to run this test.");

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

    private static bool IsWingetComAvailable()
    {
        try
        {
            _ = new PackageManager();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RunWingetCommand(string args)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "winget",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        process!.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new Exception($"Winget command failed: winget {args}. Error: {error}");
        }
    }
}
