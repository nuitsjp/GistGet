using GistGet.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GistGet.Application.Services;

public interface IPackageService
{
    Task<Dictionary<string, GistGetPackage>> GetInstalledPackagesAsync();
    Task<bool> InstallPackageAsync(GistGetPackage package);
    Task<bool> UninstallPackageAsync(string packageId);
    Task RunPassthroughAsync(string command, string[] args);
    Task<SyncResult> SyncAsync(Dictionary<string, GistGetPackage> gistPackages, Dictionary<string, GistGetPackage> localPackages);
}
