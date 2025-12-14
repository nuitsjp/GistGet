using Octokit;

namespace GistGet.Infrastructure;

public interface IGitHubClientFactory
{
    IGitHubClientWrapper Create(string? token);
}

public interface IGitHubClientWrapper
{
    Task<OauthDeviceFlowResponse> InitiateDeviceFlowAsync(OauthDeviceFlowRequest request);
    Task<OauthToken> CreateAccessTokenForDeviceFlowAsync(string clientId, OauthDeviceFlowResponse response);
    Task<User> GetCurrentUserAsync();
    ApiInfo? GetLastApiInfo();
    Task<IReadOnlyList<Gist>> GetAllGistsAsync();
    Task<Gist> GetGistAsync(string id);
    Task<Gist> EditGistAsync(string id, GistUpdate update);
    Task<Gist> CreateGistAsync(NewGist gist);
}
