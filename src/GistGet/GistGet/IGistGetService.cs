namespace GistGet;

public interface IGistGetService
{
    Task AuthLoginAsync();
    void AuthLogout();
    void AuthStatus();

    Task<WinGetPackage?> FindByIdAsync(PackageId id);
    Task InstallAndSaveAsync(GistGetPackage package);
    Task UninstallAndSaveAsync(string packageId);
    Task UpgradeAndSaveAsync(string packageId, string? version = null);
    Task PinAddAndSaveAsync(string packageId, string version);
    Task PinRemoveAndSaveAsync(string packageId);
    Task<int> RunPassthroughAsync(string command, string[] args);
}
