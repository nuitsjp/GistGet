using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using GistGet.Presentation;
using Octokit;


namespace GistGet.Infrastructure;

public class GitHubService(
    ICredentialService credentialService,
    IConsoleService consoleService,
    IGitHubClientFactory gitHubClientFactory,
    HttpClient? httpClient = null) : IGitHubService
{
    private readonly IGitHubClientFactory _gitHubClientFactory = gitHubClientFactory;
    private readonly HttpClient _httpClient = httpClient ?? new HttpClient();

    public async Task<Credential> LoginAsync()
    {
        var client = _gitHubClientFactory.Create(null);
        var request = CreateDeviceFlowRequest();

        var deviceFlowResponse = await client.InitiateDeviceFlowAsync(request);

        // Copy user code to clipboard
        try
        {
            consoleService.SetClipboard(deviceFlowResponse.UserCode);
        }
        catch { /* Ignore if clipboard operation fails */ }

        // Display message in gh CLI style
        consoleService.WriteWarning($"First copy your one-time code: {deviceFlowResponse.UserCode}");
        consoleService.WriteInfo($"Press Enter to open {deviceFlowResponse.VerificationUri} in your browser...");

        // Wait for user to press Enter
        consoleService.ReadLine();

        // Open browser after user presses Enter
        OpenBrowser(deviceFlowResponse.VerificationUri);

        var token = await client.CreateAccessTokenForDeviceFlowAsync(Constants.ClientId, deviceFlowResponse);

        var authenticatedClient = _gitHubClientFactory.Create(token.AccessToken);
        var user = await authenticatedClient.GetCurrentUserAsync();

        return new Credential(user.Login, token.AccessToken);
    }

    protected virtual OauthDeviceFlowRequest CreateDeviceFlowRequest()
    {
        var request = new OauthDeviceFlowRequest(Constants.ClientId);
        request.Scopes.Add("gist");
        return request;
    }

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

    public async Task<TokenStatus> GetTokenStatusAsync(string token)
    {
        var client = _gitHubClientFactory.Create(token);
        
        // This call will verify the token and populate LastApiInfo with headers (including scopes)
        var user = await client.GetCurrentUserAsync();
        
        var apiInfo = client.GetLastApiInfo();
        var scopes = apiInfo?.OauthScopes ?? new List<string>();

        return new TokenStatus(user.Login, scopes.ToList());
    }

    public async Task<IReadOnlyList<GistGetPackage>> GetPackagesFromUrlAsync(string url)
    {
        var httpClient = CreateHttpClient();
        var hasHeader = httpClient.DefaultRequestHeaders.UserAgent.Any(h => h.Product?.Name == Constants.ProductHeader);
        if (!hasHeader)
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.ProductHeader);
        }
        
        var yaml = await httpClient.GetStringAsync(url);
        
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return Array.Empty<GistGetPackage>();
        }

        return GistGetPackageSerializer.Deserialize(yaml);
    }

    public async Task<IReadOnlyList<GistGetPackage>> GetPackagesAsync(string token, string gistFileName, string gistDescription)
    {
        var resolvedToken = ResolveToken(token);
        if (string.IsNullOrWhiteSpace(resolvedToken))
        {
            throw new InvalidOperationException("Authentication required to fetch default Gist.");
        }

        var client = _gitHubClientFactory.Create(resolvedToken);
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

    public async Task SavePackagesAsync(string token, string gistUrl, string gistFileName, string gistDescription,
        IReadOnlyList<GistGetPackage> packages)
    {
        var resolvedToken = ResolveToken(token);
        if (string.IsNullOrWhiteSpace(resolvedToken))
        {
            throw new InvalidOperationException("Authentication required to save packages.");
        }

        var client = _gitHubClientFactory.Create(resolvedToken);
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

    private static (string FileName, string Description) ResolveGistMetadata(string? gistFileName, string? gistDescription)
    {
        var fileName = string.IsNullOrWhiteSpace(gistFileName) ? Constants.DefaultGistFileName : gistFileName;
        var description = string.IsNullOrWhiteSpace(gistDescription) ? Constants.DefaultGistDescription : gistDescription;
        return (fileName, description);
    }
}
