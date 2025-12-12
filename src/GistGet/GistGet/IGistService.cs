namespace GistGet;

public interface IGistService
{
    Task<Dictionary<string, GistGetPackage>> GetPackagesAsync(string? gistUrl = null, string? gistFileName = null, string? gistDescription = null);
    Task SavePackagesAsync(Dictionary<string, GistGetPackage> packages, string? gistFileName = null, string? gistDescription = null);
    
    Task<int> RunPassthroughAsync(string command, string[] args);
    Task<bool> InstallAndSaveAsync(GistGetPackage package);
    Task<bool> UninstallAndSaveAsync(string packageId);
    Task<bool> UpgradeAndSaveAsync(string packageId, string? version = null);
    Task<bool> PinAddAndSaveAsync(string packageId, string version);
    Task<bool> PinRemoveAndSaveAsync(string packageId);
}