using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GistGet.Models;
using Microsoft.Management.Deployment;

namespace GistGet.Infrastructure.WinGet;

public class WinGetRepository : IWinGetRepository
{
    public async Task<Dictionary<string, GistGetPackage>> GetInstalledPackagesAsync()
    {
        var packages = new Dictionary<string, GistGetPackage>(StringComparer.OrdinalIgnoreCase);

        var packageManager = new PackageManager();
        var catalogRef = packageManager.GetLocalPackageCatalog(LocalPackageCatalog.InstalledPackages);
        var connectResult = await catalogRef.ConnectAsync();
        if (connectResult.Status != ConnectResultStatus.Ok || connectResult.PackageCatalog == null)
        {
            return packages;
        }

        var findOptions = new FindPackagesOptions();
        var findResult = await connectResult.PackageCatalog.FindPackagesAsync(findOptions);
        for (var i = 0; i < findResult.Matches.Count; i++)
        {
            var match = findResult.Matches[i];
            var package = match.CatalogPackage;
            var installedVersion = package.InstalledVersion;
            if (installedVersion == null)
            {
                continue;
            }

            var id = package.Id;
            var version = installedVersion.Version ?? "Unknown";

            if (!packages.ContainsKey(id))
            {
                packages.Add(id, new GistGetPackage
                {
                    Id = id,
                    Version = version
                });
            }
        }

        return packages;
    }
}
