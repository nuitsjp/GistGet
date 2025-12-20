// Pin information returned by winget pin list.

using System.Diagnostics.CodeAnalysis;

namespace GistGet;

/// <summary>
/// Represents a pinned package from WinGet.
/// </summary>
/// <param name="Id">Package identifier.</param>
/// <param name="Version">Currently installed version.</param>
/// <param name="PinType">Type of pin: Gating, Blocking, or Pinning.</param>
/// <param name="PinnedVersion">The version to which the package is pinned.</param>
[ExcludeFromCodeCoverage]
public record WinGetPin(
    PackageId Id,
    Version Version,
    string PinType,
    Version? PinnedVersion
);
