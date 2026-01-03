// Display model for Gist package list table.

namespace NuitsJp.GistGet;

/// <summary>
/// Represents a row in the Gist package list table.
/// </summary>
// ReSharper disable UnusedAutoPropertyAccessor.Global
// Note: Properties are accessed via reflection by FluentTextTable
public class GistPackageRow
{
    /// <summary>
    /// Gets or sets the package identifier.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// Gets or sets the package display name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the pinned version (empty if not pinned).
    /// </summary>
    public string Pin { get; set; } = "";
}
// ReSharper restore UnusedAutoPropertyAccessor.Global




