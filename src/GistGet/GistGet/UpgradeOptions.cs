// Command options for upgrading packages via GistGet.

namespace GistGet;

/// <summary>
/// Represents CLI options for the upgrade workflow.
/// </summary>
public record UpgradeOptions
{
    public required string Id { get; init; }
    public string? Version { get; init; }
    public string? Scope { get; init; }
    public string? Architecture { get; init; }
    public string? Location { get; init; }
    public string? Locale { get; init; }
    public string? Log { get; init; }
    public string? Custom { get; init; }
    public string? Override { get; init; }
    public string? InstallerType { get; init; }
    public string? Header { get; init; }
    public bool Interactive { get; init; }
    public bool Silent { get; init; }
    public bool Force { get; init; }
    public bool AcceptPackageAgreements { get; init; }
    public bool AcceptSourceAgreements { get; init; }
    public bool AllowHashMismatch { get; init; }
    public bool SkipDependencies { get; init; }
}
