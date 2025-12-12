namespace GistGet;

public class GistService() : IGistService
{
    public Task<Dictionary<string, GistGetPackage>> GetPackagesAsync(string? gistUrl = null, string? gistFileName = null, string? gistDescription = null)
    {
        throw new NotImplementedException();
    }

    public Task SavePackagesAsync(Dictionary<string, GistGetPackage> packages, string? gistFileName = null, string? gistDescription = null)
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