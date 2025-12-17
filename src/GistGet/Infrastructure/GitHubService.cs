// GitHub API implementation for authentication and Gist operations.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Octokit;

namespace GistGet.Infrastructure;

/// <summary>
/// Implements GitHub authentication and Gist persistence.
/// </summary>
public class GitHubService(
    ICredentialService credentialService,
    IConsoleService consoleService,
    IGitHubClientFactory gitHubClientFactory,
    HttpClient? httpClient = null) : IGitHubService
{
    private readonly HttpClient _httpClient = httpClient ?? new HttpClient();

    /// <summary>
    /// Performs GitHub device flow login.
    /// </summary>
    public async Task<Credential> LoginAsync()
    {
        var client = gitHubClientFactory.Create(null);
        var request = CreateDeviceFlowRequest();

        var deviceFlowResponse = await client.InitiateDeviceFlowAsync(request);

        SafeSetClipboard(deviceFlowResponse.UserCode);

        consoleService.WriteWarning($"First copy your one-time code: {deviceFlowResponse.UserCode}");
        consoleService.WriteInfo($"Press Enter to open {deviceFlowResponse.VerificationUri} in your browser...");
        consoleService.ReadLine();
        OpenBrowser(deviceFlowResponse.VerificationUri);

        var token = await client.CreateAccessTokenForDeviceFlowAsync(Constants.ClientId, deviceFlowResponse);

        var authenticatedClient = gitHubClientFactory.Create(token.AccessToken);
        var user = await authenticatedClient.GetCurrentUserAsync();

        return new Credential(user.Login, token.AccessToken);
    }

    protected virtual OauthDeviceFlowRequest CreateDeviceFlowRequest()
    {
        var request = new OauthDeviceFlowRequest(Constants.ClientId);
        request.Scopes.Add("gist");
        return request;
    }

    /// <summary>
    /// Opens browser to verification URI.
    /// Defensive: handles browser launch failures gracefully.
    /// </summary>
    [ExcludeFromCodeCoverage]
    protected virtual bool OpenBrowser(string verificationUri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(verificationUri) { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns token owner and scope information.
    /// </summary>
    public async Task<TokenStatus> GetTokenStatusAsync(string token)
    {
        var client = gitHubClientFactory.Create(token);

        var user = await client.GetCurrentUserAsync();

        var apiInfo = client.GetLastApiInfo();
        var scopes = apiInfo?.OauthScopes ?? new List<string>();

        return new TokenStatus(user.Login, scopes.ToList());
    }

    /// <summary>
    /// Loads packages from a YAML URL.
    /// </summary>
    public async Task<IReadOnlyList<GistGetPackage>> GetPackagesFromUrlAsync(string url)
    {
        var httpClient = CreateHttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.ProductHeader);

        var yaml = await httpClient.GetStringAsync(url);

        if (string.IsNullOrWhiteSpace(yaml))
        {
            return Array.Empty<GistGetPackage>();
        }

        return GistGetPackageSerializer.Deserialize(yaml);
    }

    /// <summary>
    /// Loads packages from a user-owned Gist.
    /// </summary>
    public async Task<IReadOnlyList<GistGetPackage>> GetPackagesAsync(string token, string gistFileName, string gistDescription)
    {
        var resolvedToken = ResolveToken(token);
        if (string.IsNullOrWhiteSpace(resolvedToken))
        {
            throw new InvalidOperationException("Authentication required to fetch default Gist.");
        }

        var client = gitHubClientFactory.Create(resolvedToken);
        var (targetFileName, targetDescription) = ResolveGistMetadata(gistFileName, gistDescription);

        var gists = await client.GetAllGistsAsync();
        var matchingGists = gists
            .Where(g => g.Files.ContainsKey(targetFileName) || g.Description == targetDescription)
            .ToList();

        if (matchingGists.Count > 1)
        {
            throw new InvalidOperationException(
                $"Multiple Gists match the criteria ({matchingGists.Count} found). Please specify the target Gist URL explicitly via the `--url` argument.");
        }

        var targetGist = matchingGists.SingleOrDefault();
        if (targetGist == null)
        {
            return Array.Empty<GistGetPackage>();
        }

        var gist = await client.GetGistAsync(targetGist.Id);
        var content = ExtractYamlContent(gist, targetFileName);

        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<GistGetPackage>();
        }

        return GistGetPackageSerializer.Deserialize(content);
    }

    /// <summary>
    /// Saves packages to a Gist, creating it when needed.
    /// </summary>
    public async Task SavePackagesAsync(string token, string gistUrl, string gistFileName, string gistDescription,
        IReadOnlyList<GistGetPackage> packages)
    {
        var resolvedToken = ResolveToken(token);
        if (string.IsNullOrWhiteSpace(resolvedToken))
        {
            throw new InvalidOperationException("Authentication required to save packages.");
        }

        var client = gitHubClientFactory.Create(resolvedToken);
        var (targetFileName, targetDescription) = ResolveGistMetadata(gistFileName, gistDescription);
        var yaml = GistGetPackageSerializer.Serialize(packages);

        if (!string.IsNullOrWhiteSpace(gistUrl))
        {
            var gistId = ParseGistId(gistUrl);
            await client.EditGistAsync(gistId, new GistUpdate
            {
                Files = { { targetFileName, new GistFileUpdate { Content = yaml } } }
            });
            return;
        }

        var gists = await client.GetAllGistsAsync();
        var matchingGists = gists
            .Where(g => g.Files.ContainsKey(targetFileName) || g.Description == targetDescription)
            .ToList();

        if (matchingGists.Count > 1)
        {
            throw new InvalidOperationException(
                $"Multiple Gists match the criteria ({matchingGists.Count} found). Please specify the target Gist URL explicitly via the `gistUrl` argument.");
        }

        var targetGist = matchingGists.SingleOrDefault();

        if (targetGist != null)
        {
            await client.EditGistAsync(targetGist.Id, new GistUpdate
            {
                Files = { { targetFileName, new GistFileUpdate { Content = yaml } } }
            });
        }
        else
        {
            var newGist = new NewGist
            {
                Description = targetDescription,
                Public = false,
                Files = { { targetFileName, yaml } }
            };
            await client.CreateGistAsync(newGist);
        }
    }

    protected virtual HttpClient CreateHttpClient()
    {
        return _httpClient;
    }

    /// <summary>
    /// Safely sets clipboard content, ignoring errors.
    /// Defensive: clipboard operations may fail in non-interactive environments.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private void SafeSetClipboard(string text)
    {
        try
        {
            consoleService.SetClipboard(text);
        }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception
        catch (Exception)
        {
            // Ignore clipboard errors - not critical for the flow
        }
#pragma warning restore RCS1075
    }

    private string? ResolveToken(string token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        if (credentialService.TryGetCredential(out var credential) &&
            !string.IsNullOrWhiteSpace(credential.Token))
        {
            return credential.Token;
        }

        return null;
    }

    private static string ParseGistId(string gistUrl)
    {
        if (gistUrl.Contains("gist.github.com", StringComparison.OrdinalIgnoreCase))
        {
            return gistUrl.TrimEnd('/').Split('/').Last();
        }
        return gistUrl;
    }

    /// <summary>
    /// Extracts YAML content from a Gist.
    /// Handles missing target files and falls back to first YAML file.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static string? ExtractYamlContent(Gist gist, string targetFileName)
    {
        if (gist.Files.TryGetValue(targetFileName, out var file))
        {
            return file.Content;
        }

        var firstYaml = gist.Files.Values
            .FirstOrDefault(f => f.Filename.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                                 f.Filename.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));
        return firstYaml?.Content;
    }

    /// <summary>
    /// Resolves Gist metadata with default fallbacks.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static (string FileName, string Description) ResolveGistMetadata(string? gistFileName, string? gistDescription)
    {
        var fileName = string.IsNullOrWhiteSpace(gistFileName) ? Constants.DefaultGistFileName : gistFileName;
        var description = string.IsNullOrWhiteSpace(gistDescription) ? Constants.DefaultGistDescription : gistDescription;
        return (fileName, description);
    }
}
