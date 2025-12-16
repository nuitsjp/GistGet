// Factory for creating configured GitHub client wrappers.

using Octokit;

namespace GistGet.Infrastructure;

/// <summary>
/// Creates GitHub client wrappers configured for optional authentication.
/// </summary>
public class GitHubClientFactory : IGitHubClientFactory
{
    /// <summary>
    /// Creates a client wrapper configured with an optional token.
    /// </summary>
    public IGitHubClientWrapper Create(string? token)
    {
        var client = new GitHubClient(new ProductHeaderValue(Constants.ProductHeader));
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.Credentials = new Credentials(token);
        }

        return new OctokitGitHubClientWrapper(client);
    }

    private sealed class OctokitGitHubClientWrapper : IGitHubClientWrapper
    {
        private readonly GitHubClient _client;

        public OctokitGitHubClientWrapper(GitHubClient client)
        {
            _client = client;
        }

        public Task<OauthDeviceFlowResponse> InitiateDeviceFlowAsync(OauthDeviceFlowRequest request)
        {
            return _client.Oauth.InitiateDeviceFlow(request);
        }

        public Task<OauthToken> CreateAccessTokenForDeviceFlowAsync(string clientId, OauthDeviceFlowResponse response)
        {
            return _client.Oauth.CreateAccessTokenForDeviceFlow(clientId, response);
        }

        public Task<User> GetCurrentUserAsync()
        {
            return _client.User.Current();
        }

        public ApiInfo? GetLastApiInfo()
        {
            return _client.GetLastApiInfo();
        }

        public Task<IReadOnlyList<Gist>> GetAllGistsAsync()
        {
            return _client.Gist.GetAll();
        }

        public Task<Gist> GetGistAsync(string id)
        {
            return _client.Gist.Get(id);
        }

        public Task<Gist> EditGistAsync(string id, GistUpdate update)
        {
            return _client.Gist.Edit(id, update);
        }

        public Task<Gist> CreateGistAsync(NewGist gist)
        {
            return _client.Gist.Create(gist);
        }
    }
}
