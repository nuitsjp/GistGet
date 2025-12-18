// WinGet COM-based package discovery implementation.

using Microsoft.Management.Deployment;

namespace GistGet.Infrastructure;

/// <summary>
/// Provides access to installed package data via WinGet COM APIs.
/// </summary>
public class WinGetService : IWinGetService
{
    private readonly IPackageCatalogConnector _connector;

    /// <summary>
    /// Initializes a new instance with the default connector.
    /// </summary>
    public WinGetService() : this(new PackageCatalogConnector())
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom connector for testing.
    /// </summary>
    /// <param name="connector">Package catalog connector.</param>
    public WinGetService(IPackageCatalogConnector connector)
    {
        _connector = connector;
    }

    /// <summary>
    /// Finds an installed package by ID.
    /// </summary>
    /// <param name="id">Package ID.</param>
    /// <returns>The installed package, or <see langword="null"/> if not found.</returns>
    public WinGetPackage? FindById(PackageId id)
    {
        var catalog = _connector.Connect(CompositeSearchBehavior.AllCatalogs);
        if (catalog == null)
        {
            return null;
        }

        var findPackagesOptions = new FindPackagesOptions();
        findPackagesOptions.Selectors.Add(new PackageMatchFilter
        {
            Field = PackageMatchField.Id,
            Option = PackageFieldMatchOption.EqualsCaseInsensitive,
            Value = id.AsPrimitive()
        });

        var findResult = _connector.FindPackages(catalog, findPackagesOptions);

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
        var usableVersion = GetUsableVersion(catalogPackage, installedVersion);

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

        var catalog = _connector.Connect(CompositeSearchBehavior.LocalCatalogs);
        if (catalog == null)
        {
            return packages;
        }

        var findPackagesOptions = new FindPackagesOptions();
        var findResult = _connector.FindPackages(catalog, findPackagesOptions);

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
            var usableVersion = GetUsableVersion(catalogPackage, installedVersion);

            packages.Add(new WinGetPackage(
                Name: catalogPackage.Name,
                Id: new PackageId(catalogPackage.Id),
                Version: new Version(installedVersion.Version),
                UsableVersion: usableVersion
            ));
        }

        return packages;
    }

    /// <summary>
    /// Determines usable version from available versions.
    /// </summary>
    private static Version? GetUsableVersion(Microsoft.Management.Deployment.CatalogPackage catalogPackage, Microsoft.Management.Deployment.PackageVersionInfo installedVersion)
    {
        // Check for available updates by comparing versions.
        // Note: IsUpdateAvailable performs applicability checks (architecture, requirements, pinning)
        // and may return false even when AvailableVersions contains newer versions (e.g., arm64-only on x64).
        // We use AvailableVersions[0] for simple version comparison without applicability constraints.
        if (catalogPackage.AvailableVersions.Count > 0)
        {
            var latestAvailableVersion = catalogPackage.AvailableVersions[0].Version;
            if (latestAvailableVersion != installedVersion.Version)
            {
                return new Version(latestAvailableVersion);
            }
        }
        return null;
    }
}
