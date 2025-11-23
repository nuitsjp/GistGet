using GistGet.Models;
using System.Threading.Tasks;

namespace GistGet.Infrastructure.WinGet;

public interface IWinGetExecutor
{
    Task<bool> InstallPackageAsync(GistGetPackage package);
    Task<bool> UninstallPackageAsync(string packageId);
    Task<bool> UpgradePackageAsync(string packageId, string? version = null);
    Task<bool> PinPackageAsync(string packageId, string version);
    Task<bool> UnpinPackageAsync(string packageId);
    Task RunPassthroughAsync(string command, string[] args);
}
