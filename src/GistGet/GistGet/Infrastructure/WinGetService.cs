// WinGet COM-based package discovery implementation.

using Microsoft.Management.Deployment;

namespace GistGet.Infrastructure;

/// <summary>
/// Provides access to installed package data via WinGet COM APIs.
/// </summary>
public class WinGetService : IWinGetService
{
    /// <summary>
    /// Finds an installed package by ID.
    /// </summary>
    /// <param name="id">Package ID.</param>
    /// <returns>The installed package, or <see langword="null"/> if not found.</returns>
    public WinGetPackage? FindById(PackageId id)
    {
        var packageManager = new PackageManager();

        var createCompositePackageCatalogOptions = new CreateCompositePackageCatalogOptions();

        var catalogs = packageManager.GetPackageCatalogs();
        for (var i = 0; i < catalogs.Count; i++)
        {
            var catalogRef = catalogs[i];
            if (!catalogRef.Info.Explicit)
            {
                createCompositePackageCatalogOptions.Catalogs.Add(catalogRef);
            }
        }
        createCompositePackageCatalogOptions.CompositeSearchBehavior = CompositeSearchBehavior.AllCatalogs;

        var compositeRef = packageManager.CreateCompositePackageCatalog(createCompositePackageCatalogOptions);
        var connectResult = compositeRef.Connect();

        if (connectResult.Status != ConnectResultStatus.Ok)
        {
            return null;
        }

        var catalog = connectResult.PackageCatalog;

        var findPackagesOptions = new FindPackagesOptions();
        findPackagesOptions.Selectors.Add(new PackageMatchFilter
        {
            Field = PackageMatchField.Id,
            Option = PackageFieldMatchOption.EqualsCaseInsensitive,
            Value = id.AsPrimitive()
        });

        var findResult = catalog.FindPackages(findPackagesOptions);

        if (findResult.Status != FindPackagesResultStatus.Ok || findResult.Matches.Count == 0)
        {
            return null;
        }

        var catalogPackage = findResult.Matches[0].CatalogPackage;

        if (catalogPackage.InstalledVersion == null)
        {
            return null;
        }

        var installedVersion = catalogPackage.InstalledVersion;

        Version? usableVersion = null;
        if (catalogPackage.AvailableVersions.Count > 0)
        {
            var latestAvailableVersion = catalogPackage.AvailableVersions[0].Version;
            if (latestAvailableVersion != installedVersion.Version)
            {
                usableVersion = new Version(latestAvailableVersion);
            }
        }

        return new WinGetPackage(
            Name: catalogPackage.Name,
            Id: new PackageId(catalogPackage.Id),
            Version: new Version(installedVersion.Version),
            UsableVersion: usableVersion
        );
    }

    /// <summary>
    /// Gets all locally installed packages.
    /// </summary>
    /// <returns>Installed packages.</returns>
    public IReadOnlyList<WinGetPackage> GetAllInstalledPackages()
    {
        var packages = new List<WinGetPackage>();
        var packageManager = new PackageManager();

        var createCompositePackageCatalogOptions = new CreateCompositePackageCatalogOptions();

        var catalogs = packageManager.GetPackageCatalogs();
        for (var i = 0; i < catalogs.Count; i++)
        {
            var catalogRef = catalogs[i];
            if (!catalogRef.Info.Explicit)
            {
                createCompositePackageCatalogOptions.Catalogs.Add(catalogRef);
            }
        }
        createCompositePackageCatalogOptions.CompositeSearchBehavior = CompositeSearchBehavior.LocalCatalogs;

        var compositeRef = packageManager.CreateCompositePackageCatalog(createCompositePackageCatalogOptions);
        var connectResult = compositeRef.Connect();

        if (connectResult.Status != ConnectResultStatus.Ok)
        {
            return packages;
        }

        var catalog = connectResult.PackageCatalog;

        var findPackagesOptions = new FindPackagesOptions();
        var findResult = catalog.FindPackages(findPackagesOptions);

        if (findResult.Status != FindPackagesResultStatus.Ok)
        {
            return packages;
        }

        for (var i = 0; i < findResult.Matches.Count; i++)
        {
            var match = findResult.Matches[i];
            var catalogPackage = match.CatalogPackage;

            if (catalogPackage.InstalledVersion == null)
            {
                continue;
            }

            var installedVersion = catalogPackage.InstalledVersion;

            Version? usableVersion = null;
            if (catalogPackage.AvailableVersions.Count > 0)
            {
                var latestAvailableVersion = catalogPackage.AvailableVersions[0].Version;
                if (latestAvailableVersion != installedVersion.Version)
                {
                    usableVersion = new Version(latestAvailableVersion);
                }
            }

            packages.Add(new WinGetPackage(
                Name: catalogPackage.Name,
                Id: new PackageId(catalogPackage.Id),
                Version: new Version(installedVersion.Version),
                UsableVersion: usableVersion
            ));
        }

        return packages;
    }
}
