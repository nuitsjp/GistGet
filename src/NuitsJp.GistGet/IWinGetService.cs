// Abstraction for reading WinGet package information.

namespace NuitsJp.GistGet;

/// <summary>
/// Defines operations for discovering and querying WinGet packages.
/// </summary>
public interface IWinGetService
{
    /// <summary>
    /// Finds an installed package by package ID.
    /// </summary>
    WinGetPackage? FindById(PackageId id);

    /// <summary>
    /// Returns all installed packages.
    /// </summary>
    IReadOnlyList<WinGetPackage> GetAllInstalledPackages();

    /// <summary>
    /// Returns all pinned packages.
    /// </summary>
    IReadOnlyList<WinGetPin> GetPinnedPackages();

    /// <summary>
    /// Returns all packages that have updates available.
    /// </summary>
    IReadOnlyList<WinGetPackage> GetPackagesWithUpdates();
}





