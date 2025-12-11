namespace GistGet;

public class PackageService : IPackageService
{
    public Task<Dictionary<string, GistGetPackage>> GetInstalledPackagesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> InstallPackageAsync(GistGetPackage package)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UninstallPackageAsync(string packageId)
    {
        throw new NotImplementedException();
    }

    public Task<int> RunPassthroughAsync(string command, string[] args)
    {
        throw new NotImplementedException();
    }

    public Task<SyncResult> SyncAsync(Dictionary<string, GistGetPackage> gistPackages, Dictionary<string, GistGetPackage> localPackages)
    {
        throw new NotImplementedException();
    }

    public Task<bool> InstallAndSaveAsync(GistGetPackage package)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UninstallAndSaveAsync(string packageId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpgradeAndSaveAsync(string packageId, string? version = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> PinAddAndSaveAsync(string packageId, string version)
    {
        throw new NotImplementedException();
    }

    public Task<bool> PinRemoveAndSaveAsync(string packageId)
    {
        throw new NotImplementedException();
    }
}