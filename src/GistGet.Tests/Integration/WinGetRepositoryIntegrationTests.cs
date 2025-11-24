using GistGet.Infrastructure.WinGet;
using GistGet.Infrastructure.OS;
using Microsoft.Management.Deployment;
using System;
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
}
