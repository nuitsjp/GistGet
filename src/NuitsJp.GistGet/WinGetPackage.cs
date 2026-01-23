// Local WinGet package model used for listing and comparisons.

namespace NuitsJp.GistGet;

/// <summary>
/// Represents a package as reported by WinGet on the local machine.
/// </summary>
public record WinGetPackage(
    string Name,
    PackageId Id,
    Version Version,
    Version? UsableVersion,
    string? Source)
{
    /// <summary>
    /// Gets a value indicating whether the installed version is unknown.
    /// WinGet reports "Unknown" when it cannot determine the installed version.
    /// </summary>
    public bool IsVersionUnknown =>
        string.IsNullOrEmpty(Version.AsPrimitive()) ||
        Version.AsPrimitive().Equals("Unknown", StringComparison.OrdinalIgnoreCase);
}




