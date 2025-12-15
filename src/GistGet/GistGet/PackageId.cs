// Strongly-typed package identifier value object.

using UnitGenerator;

namespace GistGet;

/// <summary>
/// Represents a WinGet package identifier as a strongly typed value.
/// </summary>
[UnitOf(typeof(string))]
public partial struct PackageId;