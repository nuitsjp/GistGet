using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using GistGet.Presentation;
using Octokit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GistGet.Infrastructure;

public class GitHubService(
    ICredentialService credentialService,
    IConsoleService consoleService) : IGitHubService
{
    private const string ClientId = "Ov23lihQJhLB6hCnEIvS"; // GistGet Client ID
    private const string ProductHeader = "GistGet";
    private const string DefaultGistFileName = "gistget-packages.yaml";
    private const string DefaultGistDescription = "GistGet Packages";

    public async Task<Credential> LoginAsync()
    {
        var client = new GitHubClient(new ProductHeaderValue(ProductHeader));
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

        var token = await client.Oauth.CreateAccessTokenForDeviceFlow(ClientId, deviceFlowResponse);

        client.Credentials = new Credentials(token.AccessToken);
        var user = await client.User.Current();

        return new Credential(user.Login, token.AccessToken);
    }

    protected virtual OauthDeviceFlowRequest CreateDeviceFlowRequest()
    {
        var request = new OauthDeviceFlowRequest(ClientId);
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

    public async Task<IReadOnlyList<GistGetPackage>> GetPackagesAsync(string token, string gistUrl, string gistFileName, string gistDescription)
    {
        var resolvedToken = ResolveToken(token);
        var client = CreateClient(resolvedToken);
        var (targetFileName, targetDescription) = ResolveGistMetadata(gistFileName, gistDescription);

        string? content = null;

        if (!string.IsNullOrWhiteSpace(gistUrl))
        {
            var gistId = ParseGistId(gistUrl);
            var gist = await client.Gist.Get(gistId);
            content = ExtractYamlContent(gist, targetFileName);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(resolvedToken))
            {
                throw new InvalidOperationException("Authentication required to fetch default Gist.");
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
                var gist = await client.Gist.Get(targetGist.Id);
                content = ExtractYamlContent(gist, targetFileName);
            }
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<GistGetPackage>();
        }

        var packages = DeserializePackages(content);
        return packages.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase).ToList();
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
        var yaml = SerializePackages(packages);

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
        var client = new GitHubClient(new ProductHeaderValue(ProductHeader));
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
        var fileName = string.IsNullOrWhiteSpace(gistFileName) ? DefaultGistFileName : gistFileName;
        var description = string.IsNullOrWhiteSpace(gistDescription) ? DefaultGistDescription : gistDescription;
        return (fileName, description);
    }

    private static string SerializePackages(IReadOnlyList<GistGetPackage> packages)
    {
        var dict = new Dictionary<string, GistGetPackage>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in packages)
        {
            if (string.IsNullOrWhiteSpace(package.Id))
            {
                throw new ArgumentException("Package Id is required.", nameof(packages));
            }

            var copy = new GistGetPackage
            {
                Version = package.Version,
                Custom = package.Custom,
                Uninstall = package.Uninstall,
                Scope = package.Scope,
                Architecture = package.Architecture,
                Location = package.Location,
                Locale = package.Locale,
                AllowHashMismatch = package.AllowHashMismatch,
                Force = package.Force,
                SkipDependencies = package.SkipDependencies,
                Header = package.Header,
                InstallerType = package.InstallerType,
                Log = package.Log,
                Mode = package.Mode,
                Override = package.Override,
                Confirm = package.Confirm,
                WhatIf = package.WhatIf,
                Interactive = package.Interactive,
                Silent = package.Silent
            };

            dict[package.Id] = copy;
        }

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitNull)
            .Build();

        return serializer.Serialize(dict);
    }

    private static IReadOnlyList<GistGetPackage> DeserializePackages(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var dict = deserializer.Deserialize<Dictionary<string, GistGetPackage>>(yaml)
                   ?? new Dictionary<string, GistGetPackage>();

        var list = new List<GistGetPackage>();
        foreach (var (id, package) in dict)
        {
            var item = package ?? new GistGetPackage();
            item.Id = id;
            list.Add(item);
        }

        return list;
    }
}
