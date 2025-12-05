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
                pkg.Uninstall = false;
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
        var pinnedPackages = await _repository.GetPinnedPackagesAsync();

        foreach (var package in gistPackages.Values)
        {
            if (!package.Uninstall)
            {
                if (!string.IsNullOrEmpty(package.Version))
                {
                    // Check if already pinned to the correct version
                    if (pinnedPackages.TryGetValue(package.Id, out var pinnedVersion))
                    {
                        // If pinned version is different, update pin
                        // Note: pinnedVersion string format might differ slightly, but usually it's exact.
                        // We might want to normalize or just re-pin if string doesn't match.
                        if (!string.Equals(pinnedVersion, package.Version, StringComparison.OrdinalIgnoreCase))
                        {
                             await _executor.PinPackageAsync(package.Id, package.Version);
                        }
                    }
                    else
                    {
                        // Not pinned, so pin it
                        await _executor.PinPackageAsync(package.Id, package.Version);
                    }
                }
                else
                {
                    // Should not be pinned
                    if (pinnedPackages.ContainsKey(package.Id))
                    {
                        await _executor.UnpinPackageAsync(package.Id);
                    }
                }
            }
        }

        return result;
    }

    public async Task<bool> InstallAndSaveAsync(GistGetPackage package)
    {
        var packages = await _gistService.GetPackagesAsync();
        if (!await _executor.InstallPackageAsync(package))
        {
            return false;
        }

        package.Uninstall = false;
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

    public async Task<bool> UninstallAndSaveAsync(string packageId)
    {
        var packages = await _gistService.GetPackagesAsync();
        if (!await _executor.UninstallPackageAsync(packageId))
        {
            return false;
        }

        if (!packages.TryGetValue(packageId, out var package))
        {
            package = new GistGetPackage { Id = packageId };
            packages[packageId] = package;
        }

        package.Uninstall = true;
        await _gistService.SavePackagesAsync(packages);
        return true;
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
        var packages = await _gistService.GetPackagesAsync();
        if (!await _executor.PinPackageAsync(packageId, version))
        {
            return false;
        }

        if (!packages.TryGetValue(packageId, out var existingPackage))
        {
            existingPackage = new GistGetPackage { Id = packageId };
            packages[packageId] = existingPackage;
        }
        existingPackage.Version = version;
        existingPackage.Uninstall = false;
        await _gistService.SavePackagesAsync(packages);
        return true;
    }

    public async Task<bool> PinRemoveAndSaveAsync(string packageId)
    {
        var packages = await _gistService.GetPackagesAsync();
        if (!await _executor.UnpinPackageAsync(packageId))
        {
            return false;
        }

        if (packages.TryGetValue(packageId, out var existingPackage))
        {
            existingPackage.Version = null; // Remove version constraint
            existingPackage.Uninstall = false;
        }
        await _gistService.SavePackagesAsync(packages);
        return true;
    }
}
