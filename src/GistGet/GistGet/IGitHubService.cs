// Abstraction over GitHub API interactions used by GistGet.

namespace GistGet;

/// <summary>
/// Defines GitHub operations for authentication, Gist access, and package persistence.
/// </summary>
public interface IGitHubService
{
    /// <summary>
    /// Performs GitHub device flow login.
    /// </summary>
    Task<Credential> LoginAsync();

    /// <summary>
    /// Loads packages from a YAML URL.
    /// </summary>
    Task<IReadOnlyList<GistGetPackage>> GetPackagesFromUrlAsync(string url);

    /// <summary>
    /// Loads packages from a user-owned Gist.
    /// </summary>
    Task<IReadOnlyList<GistGetPackage>> GetPackagesAsync(string token, string gistFileName, string gistDescription);

    /// <summary>
    /// Saves packages to a Gist, creating it when needed.
    /// </summary>
    Task SavePackagesAsync(string token, string gistUrl, string gistFileName, string gistDescription, IReadOnlyList<GistGetPackage> packages);

    /// <summary>
    /// Returns token owner and scope information.
    /// </summary>
    Task<TokenStatus> GetTokenStatusAsync(string token);
}

/// <summary>
/// Token owner and granted scopes.
/// </summary>
public record TokenStatus(string Username, IReadOnlyList<string> Scopes);
