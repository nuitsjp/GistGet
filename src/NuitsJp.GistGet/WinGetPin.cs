// Pin information returned by winget pin list.

using System.Diagnostics.CodeAnalysis;

namespace NuitsJp.GistGet;

/// <summary>
/// Represents a pinned package from WinGet.
/// </summary>
/// <param name="Id">Package identifier.</param>
/// <param name="PinType">Type of pin: Gating, Blocking, or Pinning.</param>
/// <param name="PinnedVersion">The version to which the package is pinned.</param>
[ExcludeFromCodeCoverage]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "PinType is kept for API consistency with winget pin list output")]
public record WinGetPin(
    PackageId Id,
    string PinType,
    Version? PinnedVersion
);





