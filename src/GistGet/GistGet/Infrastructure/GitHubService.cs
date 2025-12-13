using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using GistGet.Presentation;
using Octokit;


namespace GistGet.Infrastructure;

public class GitHubService(
    ICredentialService credentialService,
    IConsoleService consoleService) : IGitHubService
{

    public async Task<Credential> LoginAsync()
    {
        var client = new GitHubClient(new ProductHeaderValue(Constants.ProductHeader));
        var request = CreateDeviceFlowRequest();

        var deviceFlowResponse = await client.Oauth.InitiateDeviceFlow(request);

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
        try
        {
            Process.Start(new ProcessStartInfo(deviceFlowResponse.VerificationUri) { UseShellExecute = true });
        }
        catch { /* Ignore if browser cannot be opened */ }

        var token = await client.Oauth.CreateAccessTokenForDeviceFlow(Constants.ClientId, deviceFlowResponse);

        client.Credentials = new Credentials(token.AccessToken);
        var user = await client.User.Current();

        return new Credential(user.Login, token.AccessToken);
    }

    protected virtual OauthDeviceFlowRequest CreateDeviceFlowRequest()
    {
        var request = new OauthDeviceFlowRequest(Constants.ClientId);
        request.Scopes.Add("gist");
        return request;
    }

    public async Task<TokenStatus> GetTokenStatusAsync(string token)
    {
        var client = CreateClient(token);
        
        // This call will verify the token and populate LastApiInfo with headers (including scopes)
        var user = await client.User.Current();
        
        var apiInfo = client.GetLastApiInfo();
        var scopes = apiInfo?.OauthScopes ?? new List<string>();

        return new TokenStatus(user.Login, scopes.ToList());
    }

    public async Task<IReadOnlyList<GistGetPackage>> GetPackagesFromUrlAsync(string url)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.ProductHeader);
        
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

        var client = CreateClient(resolvedToken);
        var (targetFileName, targetDescription) = ResolveGistMetadata(gistFileName, gistDescription);

        var gists = await client.Gist.GetAll();
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

        var gist = await client.Gist.Get(targetGist.Id);
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

        var client = CreateClient(resolvedToken);
        var (targetFileName, targetDescription) = ResolveGistMetadata(gistFileName, gistDescription);
        var yaml = GistGetPackageSerializer.Serialize(packages);

        if (!string.IsNullOrWhiteSpace(gistUrl))
        {
            var gistId = ParseGistId(gistUrl);
            await client.Gist.Edit(gistId, new GistUpdate
            {
                Files = { { targetFileName, new GistFileUpdate { Content = yaml } } }
            });
            return;
        }

        var gists = await client.Gist.GetAll();
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
            await client.Gist.Edit(targetGist.Id, new GistUpdate
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
            await client.Gist.Create(newGist);
        }
    }

    private static GitHubClient CreateClient(string? token)
    {
        var client = new GitHubClient(new ProductHeaderValue(Constants.ProductHeader));
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.Credentials = new Credentials(token);
        }
        return client;
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
