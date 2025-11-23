using GistGet.Models;
using System.Threading.Tasks;

namespace GistGet.Infrastructure.WinGet;

public interface IWinGetExecutor
{
    Task<bool> InstallPackageAsync(GistGetPackage package);
    Task<bool> UninstallPackageAsync(string packageId);
    Task RunPassthroughAsync(string command, string[] args);
}
