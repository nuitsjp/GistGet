// Default implementation of IPackageCatalogConnector using WinGet COM APIs.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Management.Deployment;

namespace NuitsJp.GistGet.Infrastructure.WinGet;

/// <summary>
/// Production implementation that connects to real WinGet COM APIs.
/// </summary>
[ExcludeFromCodeCoverage]
public class PackageCatalogConnector : IPackageCatalogConnector
{
    /// <inheritdoc />
    public PackageCatalog? Connect(CompositeSearchBehavior searchBehavior)
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

        createCompositePackageCatalogOptions.CompositeSearchBehavior = searchBehavior;

        var compositeRef = packageManager.CreateCompositePackageCatalog(createCompositePackageCatalogOptions);
        var connectResult = compositeRef.Connect();

        if (connectResult.Status != ConnectResultStatus.Ok)
        {
            return null;
        }

        return connectResult.PackageCatalog;
    }

    /// <inheritdoc />
    public FindPackagesResult FindPackages(PackageCatalog catalog, FindPackagesOptions options)
    {
        return catalog.FindPackages(options);
    }
}




