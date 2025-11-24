using GistGet.Infrastructure.WinGet;
using GistGet.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GistGet.Application.Services;

public class PackageService : IPackageService
{
    private readonly IWinGetRepository _repository;
    private readonly IWinGetExecutor _executor;
    private readonly IGistService _gistService;

    public PackageService(IWinGetRepository repository, IWinGetExecutor executor, IGistService gistService)
    {
        _repository = repository;
        _executor = executor;
        _gistService = gistService;
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

    public async Task<int> RunPassthroughAsync(string command, string[] args)
    {
        return await _executor.RunPassthroughAsync(command, args);
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

        // Enforce Pinning State
        foreach (var package in gistPackages.Values)
        {
            if (!package.Uninstall)
            {
                if (!string.IsNullOrEmpty(package.Version))
                {
                    await _executor.PinPackageAsync(package.Id, package.Version);
                }
                else
                {
                    await _executor.UnpinPackageAsync(package.Id);
                }
            }
        }

        return result;
    }

    public async Task<bool> InstallAndSaveAsync(GistGetPackage package)
    {
        if (await _executor.InstallPackageAsync(package))
        {
            var packages = await _gistService.GetPackagesAsync();
            packages[package.Id] = package;
            await _gistService.SavePackagesAsync(packages);

            if (!string.IsNullOrEmpty(package.Version))
            {
                await _executor.PinPackageAsync(package.Id, package.Version);
            }
            else
            {
                await _executor.UnpinPackageAsync(package.Id);
            }

            return true;
        }
        return false;
    }

    public async Task<bool> UninstallAndSaveAsync(string packageId)
    {
        if (await _executor.UninstallPackageAsync(packageId))
        {
            var packages = await _gistService.GetPackagesAsync();
            if (packages.ContainsKey(packageId))
            {
                packages[packageId].Uninstall = true;
                await _gistService.SavePackagesAsync(packages);
            }
            // If not in Gist, maybe add it with uninstall: true?
            // For now, only update if exists.
            return true;
        }
        return false;
    }

    public async Task<bool> UpgradeAndSaveAsync(string packageId, string? version = null)
    {
        if (await _executor.UpgradePackageAsync(packageId, version))
        {
            var packages = await _gistService.GetPackagesAsync();

            // If package exists, update version. If not, add it.
            if (packages.TryGetValue(packageId, out var existingPackage))
            {
                existingPackage.Version = version; // If version is null, it means latest, but we might want to resolve it?
                // If version is null, we don't know the installed version unless we check.
                // But for now, let's just save what user asked.
                // If user ran `gistget upgrade --id Foo`, version is null.
                // We should probably fetch the installed version to save it accurately?
                // Or just leave version null in YAML (implies latest).
                // If user specified version, save it.
                if (version != null)
                {
                    existingPackage.Version = version;
                }
                existingPackage.Uninstall = false; // Ensure it's not marked as uninstall
            }
            else
            {
                packages[packageId] = new GistGetPackage
                {
                    Id = packageId,
                    Version = version
                };
            }

            await _gistService.SavePackagesAsync(packages);

            if (!string.IsNullOrEmpty(version))
            {
                await _executor.PinPackageAsync(packageId, version);
            }
            else
            {
                await _executor.UnpinPackageAsync(packageId);
            }

            return true;
        }
        return false;
    }
    public async Task<bool> PinAddAndSaveAsync(string packageId, string version)
    {
        if (await _executor.PinPackageAsync(packageId, version))
        {
            var packages = await _gistService.GetPackagesAsync();
            if (packages.TryGetValue(packageId, out var existingPackage))
            {
                existingPackage.Version = version;
            }
            else
            {
                packages[packageId] = new GistGetPackage
                {
                    Id = packageId,
                    Version = version
                };
            }
            await _gistService.SavePackagesAsync(packages);
            return true;
        }
        return false;
    }

    public async Task<bool> PinRemoveAndSaveAsync(string packageId)
    {
        if (await _executor.UnpinPackageAsync(packageId))
        {
            var packages = await _gistService.GetPackagesAsync();
            if (packages.TryGetValue(packageId, out var existingPackage))
            {
                existingPackage.Version = null; // Remove version constraint
                await _gistService.SavePackagesAsync(packages);
            }
            return true;
        }
        return false;
    }
}
