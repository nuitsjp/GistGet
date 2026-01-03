// Command options for uninstalling a package via GistGet.

namespace NuitsJp.GistGet;

/// <summary>
/// Represents CLI options for the uninstall workflow.
/// </summary>
public record UninstallOptions
{
    public required string Id { get; init; }
    public string? Scope { get; init; }
    public bool Interactive { get; init; }
    public bool Silent { get; init; }
    public bool Force { get; init; }
}




