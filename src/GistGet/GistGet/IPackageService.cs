using GistGet.Model;

namespace GistGet;

public interface IPackageService
{
    Task<Dictionary<string, GistGetPackage>> GetInstalledPackagesAsync();
    Task<bool> InstallPackageAsync(GistGetPackage package);
    Task<bool> UninstallPackageAsync(string packageId);
    Task<int> RunPassthroughAsync(string command, string[] args);
    Task<SyncResult> SyncAsync(Dictionary<string, GistGetPackage> gistPackages, Dictionary<string, GistGetPackage> localPackages);

    Task<bool> InstallAndSaveAsync(GistGetPackage package);
    Task<bool> UninstallAndSaveAsync(string packageId);
    Task<bool> UpgradeAndSaveAsync(string packageId, string? version = null);
    Task<bool> PinAddAndSaveAsync(string packageId, string version);
    Task<bool> PinRemoveAndSaveAsync(string packageId);
}
