using GistGet;

public class GistService : IGistService
{
    public Task<Dictionary<string, GistGetPackage>> GetPackagesAsync(string? gistUrl = null, string? gistFileName = null, string? gistDescription = null)
    {
        throw new NotImplementedException();
    }

    public Task SavePackagesAsync(Dictionary<string, GistGetPackage> packages, string? gistFileName = null, string? gistDescription = null)
    {
        throw new NotImplementedException();
    }
}