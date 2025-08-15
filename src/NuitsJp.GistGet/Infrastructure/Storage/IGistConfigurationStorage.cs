using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Infrastructure.Storage;

public interface IGistConfigurationStorage
{
    string FilePath { get; }
    Task SaveGistConfigurationAsync(GistConfiguration configuration);
    Task<GistConfiguration?> LoadGistConfigurationAsync();
    Task<bool> IsConfiguredAsync();
    Task DeleteConfigurationAsync();
}