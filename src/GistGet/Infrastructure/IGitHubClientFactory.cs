// Abstraction for creating GitHub API clients.

using Octokit;

namespace GistGet.Infrastructure;

/// <summary>
/// Defines a factory for producing configured GitHub client instances.
/// </summary>
public interface IGitHubClientFactory
{
    /// <summary>
    /// Creates a client wrapper configured with an optional token.
    /// </summary>
    IGitHubClientWrapper Create(string? token);
}

/// <summary>
/// Minimal GitHub client surface used by the application.
/// </summary>
public interface IGitHubClientWrapper
{
    /// <summary>
    /// Starts a device flow authorization.
    /// </summary>
    Task<OauthDeviceFlowResponse> InitiateDeviceFlowAsync(OauthDeviceFlowRequest request);

    /// <summary>
    /// Exchanges a device flow response for an access token.
    /// </summary>
    Task<OauthToken> CreateAccessTokenForDeviceFlowAsync(string clientId, OauthDeviceFlowResponse response);

    /// <summary>
    /// Returns the current authenticated user.
    /// </summary>
    Task<User> GetCurrentUserAsync();

    /// <summary>
    /// Returns API response metadata from the last call.
    /// </summary>
    ApiInfo? GetLastApiInfo();

    /// <summary>
    /// Returns all gists for the current user.
    /// </summary>
    Task<IReadOnlyList<Gist>> GetAllGistsAsync();

    /// <summary>
    /// Loads a gist by ID.
    /// </summary>
    Task<Gist> GetGistAsync(string id);

    /// <summary>
    /// Updates an existing gist.
    /// </summary>
    Task<Gist> EditGistAsync(string id, GistUpdate update);

    /// <summary>
    /// Creates a new gist.
    /// </summary>
    Task<Gist> CreateGistAsync(NewGist gist);
}
