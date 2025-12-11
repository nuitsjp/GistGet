namespace GistGet;

public interface IGitHubService
{
    Task<Credential> LoginAsync();

    Task StatusAsync();

    Task<IReadOnlyList<GistGetPackage>> GetPackagesAsync(string token, string gistUrl, string gistFileName, string gistDescription);
    Task SavePackagesAsync(string token, string gistUrl, string gistFileName, string gistDescription, IReadOnlyList<GistGetPackage> packages);
}
