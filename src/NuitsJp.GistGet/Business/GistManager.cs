using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Infrastructure.Storage;
using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Business;

public class GistManager(
    IGitHubGistClient gistClient,
    IGistConfigurationStorage storage,
    IPackageYamlConverter yamlConverter,
    ILogger<GistManager> logger) : IGistManager
{
    private readonly IGitHubGistClient _gistClient = gistClient ?? throw new ArgumentNullException(nameof(gistClient));
    private readonly ILogger<GistManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IGistConfigurationStorage _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    private readonly IPackageYamlConverter _yamlConverter =
        yamlConverter ?? throw new ArgumentNullException(nameof(yamlConverter));

    public async Task<bool> IsConfiguredAsync()
    {
        try
        {
            return await _storage.IsConfiguredAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Gist configuration");
            return false;
        }
    }

    public async Task<PackageCollection> GetGistPackagesAsync()
    {
        var config = await LoadConfigurationAsync();

        try
        {
            _logger.LogInformation("Retrieving packages from Gist {GistId}, file '{FileName}'",
                config.GistId, config.FileName);

            var yamlContent = await _gistClient.GetFileContentAsync(config.GistId, config.FileName);
            var packages = _yamlConverter.FromYaml(yamlContent);

            config.UpdateLastAccessed();
            await _storage.SaveGistConfigurationAsync(config);

            _logger.LogInformation("Successfully retrieved {PackageCount} packages from Gist", packages.Count);
            return packages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve packages from Gist {GistId}", config.GistId);
            throw;
        }
    }

    public async Task UpdateGistPackagesAsync(PackageCollection packages)
    {
        if (packages == null)
            throw new ArgumentNullException(nameof(packages));

        var config = await LoadConfigurationAsync();

        try
        {
            _logger.LogInformation("Updating Gist {GistId}, file '{FileName}' with {PackageCount} packages",
                config.GistId, config.FileName, packages.Count);

            var yamlContent = _yamlConverter.ToYaml(packages);
            await _gistClient.UpdateFileContentAsync(config.GistId, config.FileName, yamlContent);

            config.UpdateLastAccessed();
            await _storage.SaveGistConfigurationAsync(config);

            _logger.LogInformation("Successfully updated Gist with {PackageCount} packages", packages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Gist {GistId} with packages", config.GistId);
            throw;
        }
    }

    public async Task<GistConfiguration> GetConfigurationAsync()
    {
        return await LoadConfigurationAsync();
    }

    public async Task ValidateGistAccessAsync(string gistId)
    {
        if (string.IsNullOrWhiteSpace(gistId))
            throw new ArgumentException("Gist ID cannot be null or empty", nameof(gistId));

        try
        {
            _logger.LogInformation("Validating access to Gist {GistId}", gistId);

            var exists = await _gistClient.ExistsAsync(gistId);
            if (!exists) throw new InvalidOperationException($"Gist {gistId} does not exist or is not accessible");

            _logger.LogInformation("Gist {GistId} is accessible", gistId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate access to Gist {GistId}", gistId);
            throw;
        }
    }

    private async Task<GistConfiguration> LoadConfigurationAsync()
    {
        var config = await _storage.LoadGistConfigurationAsync();
        if (config == null)
            throw new InvalidOperationException(
                "Gist configuration not found. Please configure Gist settings first using 'gistget gist set' command.");

        return config;
    }
}