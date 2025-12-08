namespace GistGet.Model;

public interface IGistService
{
    Task<Dictionary<string, GistGetPackage>> GetPackagesAsync(string? gistUrl = null, string? gistFileName = null, string? gistDescription = null);
    Task SavePackagesAsync(Dictionary<string, GistGetPackage> packages, string? gistFileName = null, string? gistDescription = null);
}
