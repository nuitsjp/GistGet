using GistGet.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GistGet.Infrastructure.WinGet;

public class WinGetRepository : IWinGetRepository
{
    private static readonly Guid CLSID_PackageManager = new Guid("C53A4F16-787E-42A4-B304-29EFFB4BF597");

    public async Task<Dictionary<string, GistGetPackage>> GetInstalledPackagesAsync()
    {
        return await Task.Run(() =>
        {
            var packages = new Dictionary<string, GistGetPackage>();

            Type? type = Type.GetTypeFromCLSID(CLSID_PackageManager);
            if (type == null) throw new InvalidOperationException("Winget COM not found. Make sure App Installer is installed.");

            dynamic packageManager = Activator.CreateInstance(type)!;
            dynamic catalogRef = packageManager.GetLocalPackageCatalog();
            dynamic connectResult = catalogRef.Connect();
            dynamic catalog = connectResult.PackageCatalog;
            dynamic options = packageManager.CreateFindPackagesOptions();
            dynamic findResult = catalog.FindPackages(options);

            foreach (dynamic match in findResult.Matches)
            {
                dynamic package = match.Package;
                string id = package.Id;

                dynamic installedVersion = package.InstalledVersion;
                string version = installedVersion?.Version ?? "Unknown";

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
        });
    }
}
