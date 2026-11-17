// Abstraction for WinGet COM catalog operations to enable unit testing.

using Microsoft.Management.Deployment;

namespace NuitsJp.GistGet.Infrastructure;

/// <summary>
/// Abstracts WinGet PackageManager operations for testability.
/// </summary>
public interface IPackageCatalogConnector
{
    /// <summary>
    /// Connects to the composite package catalog.
    /// </summary>
    /// <param name="searchBehavior">Composite search behavior.</param>
    /// <returns>Connected catalog. Connection failures are reported as exceptions.</returns>
    PackageCatalog? Connect(CompositeSearchBehavior searchBehavior);

    /// <summary>
    /// Finds packages matching the given options.
    /// </summary>
    /// <param name="catalog">Package catalog to search.</param>
    /// <param name="options">Search options.</param>
    /// <returns>Find packages result.</returns>
    FindPackagesResult FindPackages(PackageCatalog catalog, FindPackagesOptions options);
}




