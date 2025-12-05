using GistGet.Models;
using GistGet.Utils;
using Octokit;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GistGet.Application.Services;

public class GistService : IGistService
{
    private readonly IAuthService _authService;
    private const string GistFileName = "gistget-packages.yaml";
    private const string GistDescription = "GistGet Packages";

    public GistService(IAuthService authService)
    {
        _authService = authService;
    }

    private async Task<GitHubClient> GetClientAsync()
    {
        var token = await _authService.GetAccessTokenAsync();
        var client = new GitHubClient(new ProductHeaderValue("GistGet"));
        if (!string.IsNullOrEmpty(token))
        {
            client.Credentials = new Credentials(token);
        }
        return client;
    }

    public async Task<Dictionary<string, GistGetPackage>> GetPackagesAsync(string? gistUrl = null, string? gistFileName = null, string? gistDescription = null)
    {
        var client = await GetClientAsync();
        string? content = null;
        var (targetFileName, targetDescription) = ResolveGistMetadata(gistFileName, gistDescription);

        if (!string.IsNullOrEmpty(gistUrl))
        {
            try
            {
                var gistId = gistUrl;
                if (gistUrl.Contains("gist.github.com"))
                {
                    gistId = gistUrl.Split('/').Last();
                }

                var gist = await client.Gist.Get(gistId);
                if (gist.Files.TryGetValue(targetFileName, out var file))
                {
                    content = file.Content;
                }
                else
                {
                    var firstYaml = gist.Files.Values.FirstOrDefault(f => f.Filename.EndsWith(".yaml") || f.Filename.EndsWith(".yml"));
                    if (firstYaml != null)
                    {
                        content = firstYaml.Content;
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to fetch Gist from URL/ID: {ex.Message}[/]");
                throw;
            }
        }
        else
        {
            if (!await _authService.IsAuthenticatedAsync())
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
                if (gist.Files.TryGetValue(targetFileName, out var file))
                {
                    content = file.Content;
                }
                else
                {
                    var firstYaml = gist.Files.Values.FirstOrDefault(f => f.Filename.EndsWith(".yaml") || f.Filename.EndsWith(".yml"));
                    if (firstYaml != null)
                    {
                        content = firstYaml.Content;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(content))
        {
            return new Dictionary<string, GistGetPackage>();
        }

        return YamlHelper.Deserialize(content);
    }

    public async Task SavePackagesAsync(Dictionary<string, GistGetPackage> packages, string? gistFileName = null, string? gistDescription = null)
    {
        if (!await _authService.IsAuthenticatedAsync())
        {
            throw new InvalidOperationException("Authentication required to save packages.");
        }

        var client = await GetClientAsync();
        var yaml = YamlHelper.Serialize(packages);
        var (targetFileName, targetDescription) = ResolveGistMetadata(gistFileName, gistDescription);

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
            AnsiConsole.MarkupLine($"[green]Updated existing Gist: {targetGist.HtmlUrl}[/]");
        }
        else
        {
            var newGist = new NewGist
            {
                Description = targetDescription,
                Public = false,
                Files = { { targetFileName, yaml } }
            };
            var createdGist = await client.Gist.Create(newGist);
            AnsiConsole.MarkupLine($"[green]Created new Gist: {createdGist.HtmlUrl}[/]");
        }
    }

    private (string FileName, string Description) ResolveGistMetadata(string? gistFileName, string? gistDescription)
    {
        var fileName = string.IsNullOrWhiteSpace(gistFileName) ? GistFileName : gistFileName;
        var description = string.IsNullOrWhiteSpace(gistDescription) ? GistDescription : gistDescription;
        return (fileName, description);
    }
}
