using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Interfaces;

public interface IGistConfigurationStorage
{
    Task SaveGistConfigurationAsync(GistConfiguration configuration);
    Task<GistConfiguration?> LoadGistConfigurationAsync();
    Task<bool> IsConfiguredAsync();
    Task DeleteConfigurationAsync();
    string FilePath { get; }
}