using GistGet;

public class GistService(IWinGetPassthroughRunner passthroughRunner) : IGistService
{
    public Task<Dictionary<string, GistGetPackage>> GetPackagesAsync(string? gistUrl = null, string? gistFileName = null, string? gistDescription = null)
    {
        throw new NotImplementedException();
    }

    public Task SavePackagesAsync(Dictionary<string, GistGetPackage> packages, string? gistFileName = null, string? gistDescription = null)
    {
        throw new NotImplementedException();
    }

    public Task<int> RunPassthroughAsync(string command, string[] args)
    {
        var fullArgs = new List<string> { command };
        fullArgs.AddRange(args);
        return passthroughRunner.RunAsync(fullArgs.ToArray());
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