using GistGet.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GistGet.Application.Services;

public interface IGistService
{
    Task<Dictionary<string, GistGetPackage>> GetPackagesAsync(string? gistUrl = null);
    Task SavePackagesAsync(Dictionary<string, GistGetPackage> packages);
}
