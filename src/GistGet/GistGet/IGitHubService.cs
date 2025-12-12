namespace GistGet;

public interface IGitHubService
{
    Task<Credential> LoginAsync();



    Task<IReadOnlyList<GistGetPackage>> GetPackagesAsync(string token, string gistUrl, string gistFileName, string gistDescription);
    Task SavePackagesAsync(string token, string gistUrl, string gistFileName, string gistDescription, IReadOnlyList<GistGetPackage> packages);
    Task<TokenStatus> GetTokenStatusAsync(string token);
}

public record TokenStatus(string Username, IReadOnlyList<string> Scopes);
