// Abstraction for reading WinGet package information.

namespace GistGet;

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
}
