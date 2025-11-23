using GistGet.Infrastructure.WinGet;
using GistGet.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GistGet.Application.Services;

public class PackageService : IPackageService
{
    private readonly IWinGetRepository _repository;
    private readonly IWinGetExecutor _executor;

    public PackageService(IWinGetRepository repository, IWinGetExecutor executor)
    {
        _repository = repository;
        _executor = executor;
    }

    public async Task<Dictionary<string, GistGetPackage>> GetInstalledPackagesAsync()
    {
        return await _repository.GetInstalledPackagesAsync();
    }

    public async Task<bool> InstallPackageAsync(GistGetPackage package)
    {
        return await _executor.InstallPackageAsync(package);
    }

    public async Task<bool> UninstallPackageAsync(string packageId)
    {
        return await _executor.UninstallPackageAsync(packageId);
    }

    public async Task RunPassthroughAsync(string command, string[] args)
    {
        await _executor.RunPassthroughAsync(command, args);
    }

    public async Task<Models.SyncResult> SyncAsync(Dictionary<string, GistGetPackage> gistPackages, Dictionary<string, GistGetPackage> localPackages)
    {
        var result = new Models.SyncResult();
        var toInstall = new List<GistGetPackage>();
        var toUninstall = new List<GistGetPackage>();

        foreach (var package in gistPackages.Values)
        {
            if (package.Uninstall)
            {
                if (localPackages.ContainsKey(package.Id))
                {
                    toUninstall.Add(package);
                }
            }
            else
            {
                if (!localPackages.ContainsKey(package.Id))
                {
                    toInstall.Add(package);
                }
                // TODO: Version check / upgrade check
            }
        }

        foreach (var pkg in toUninstall)
        {
            if (await _executor.UninstallPackageAsync(pkg.Id))
            {
                result.Uninstalled.Add(pkg);
            }
            else
            {
                result.Failed.Add(pkg);
                result.Errors.Add($"Failed to uninstall {pkg.Id}");
            }
        }

        foreach (var pkg in toInstall)
        {
            if (await _executor.InstallPackageAsync(pkg))
            {
                result.Installed.Add(pkg);
            }
            else
            {
                result.Failed.Add(pkg);
                result.Errors.Add($"Failed to install {pkg.Id}");
            }
        }

        return result;
    }
}
