using GistGet.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GistGet.Infrastructure.WinGet;

public interface IWinGetRepository
{
    Task<Dictionary<string, GistGetPackage>> GetInstalledPackagesAsync();
    Task<Dictionary<string, string>> GetPinnedPackagesAsync();
}
