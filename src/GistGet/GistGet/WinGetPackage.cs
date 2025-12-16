// Local WinGet package model used for listing and comparisons.

namespace GistGet;

/// <summary>
/// Represents a package as reported by WinGet on the local machine.
/// </summary>
public record WinGetPackage(
    string Name,
    PackageId Id,
    Version Version,
    Version? UsableVersion);
