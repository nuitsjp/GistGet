using Microsoft.Extensions.Logging;
using Octokit;

namespace NuitsJp.GistGet.Services;

public class GitHubGistClient
{
    private readonly GitHubAuthService _authService;
    private readonly ILogger<GitHubGistClient> _logger;

    public GitHubGistClient(GitHubAuthService authService, ILogger<GitHubGistClient> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetFileContentAsync(string gistId, string fileName)
    {
        ValidateGistId(gistId);
        ValidateFileName(fileName);

        try
        {
            var client = await _authService.GetAuthenticatedClientAsync();
            var gist = await client.Gist.Get(gistId);

            if (gist?.Files == null || !gist.Files.ContainsKey(fileName))
            {
                throw new InvalidOperationException($"File '{fileName}' not found in Gist {gistId}");
            }

            var file = gist.Files[fileName];
            if (string.IsNullOrEmpty(file.Content))
            {
                _logger.LogWarning("File '{FileName}' in Gist {GistId} has no content", fileName, gistId);
                return string.Empty;
            }

            _logger.LogInformation("Successfully retrieved file '{FileName}' from Gist {GistId}", fileName, gistId);
            return file.Content;
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, "Gist {GistId} not found", gistId);
            throw new InvalidOperationException($"Gist {gistId} not found", ex);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "GitHub API error when retrieving Gist {GistId}: {Message}", gistId, ex.Message);
            throw new InvalidOperationException($"Failed to retrieve Gist {gistId}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when retrieving Gist {GistId}", gistId);
            throw new InvalidOperationException($"Failed to retrieve Gist {gistId}: {ex.Message}", ex);
        }
    }

    public async Task UpdateFileContentAsync(string gistId, string fileName, string content)
    {
        ValidateGistId(gistId);
        ValidateFileName(fileName);

        if (content == null)
            throw new ArgumentException("Content cannot be null", nameof(content));

        try
        {
            var client = await _authService.GetAuthenticatedClientAsync();

            var updateRequest = new GistUpdate();
            updateRequest.Files.Add(fileName, new GistFileUpdate
            {
                Content = content
            });

            await client.Gist.Edit(gistId, updateRequest);
            _logger.LogInformation("Successfully updated file '{FileName}' in Gist {GistId}", fileName, gistId);
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, "Gist {GistId} not found", gistId);
            throw new InvalidOperationException($"Gist {gistId} not found", ex);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "GitHub API error when updating Gist {GistId}: {Message}", gistId, ex.Message);
            throw new InvalidOperationException($"Failed to update Gist {gistId}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when updating Gist {GistId}", gistId);
            throw new InvalidOperationException($"Failed to update Gist {gistId}: {ex.Message}", ex);
        }
    }

    public async Task<bool> ExistsAsync(string gistId)
    {
        ValidateGistId(gistId);

        try
        {
            var client = await _authService.GetAuthenticatedClientAsync();
            await client.Gist.Get(gistId);
            return true;
        }
        catch (NotFoundException)
        {
            _logger.LogDebug("Gist {GistId} not found", gistId);
            return false;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "GitHub API error when checking Gist {GistId} existence: {Message}", gistId, ex.Message);
            throw new InvalidOperationException($"Failed to check Gist {gistId} existence: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when checking Gist {GistId} existence", gistId);
            throw new InvalidOperationException($"Failed to check Gist {gistId} existence: {ex.Message}", ex);
        }
    }

    private static void ValidateGistId(string gistId)
    {
        if (string.IsNullOrWhiteSpace(gistId))
            throw new ArgumentException("Gist ID cannot be null or empty", nameof(gistId));
    }

    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
    }
}