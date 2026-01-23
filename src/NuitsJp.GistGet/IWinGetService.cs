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
    /// <remarks>
    /// By default, packages with unknown installed versions are excluded.
    /// Use <see cref="GetPackagesWithUpdates(bool)"/> to include them.
    /// </remarks>
    IReadOnlyList<WinGetPackage> GetPackagesWithUpdates();

    /// <summary>
    /// Returns all packages that have updates available.
    /// </summary>
    /// <param name="includeUnknown">
    /// When <c>true</c>, includes packages whose installed version is unknown.
    /// When <c>false</c>, excludes such packages.
    /// </param>
    IReadOnlyList<WinGetPackage> GetPackagesWithUpdates(bool includeUnknown);
}





