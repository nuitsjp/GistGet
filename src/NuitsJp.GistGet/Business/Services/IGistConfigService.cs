using NuitsJp.GistGet.Business.Models;

namespace NuitsJp.GistGet.Business.Services
{
    public interface IGistConfigService
    {
        Task<GistConfigResult> ConfigureGistAsync(GistConfigRequest request);
    }
}