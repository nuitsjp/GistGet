using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Business
{
    public interface IGistManager
    {
        Task<bool> IsConfiguredAsync();
        Task<PackageCollection> GetGistPackagesAsync();
        Task UpdateGistPackagesAsync(PackageCollection packages);
        Task<GistConfiguration> GetConfigurationAsync();
        Task ValidateGistAccessAsync(string gistId);
    }
}