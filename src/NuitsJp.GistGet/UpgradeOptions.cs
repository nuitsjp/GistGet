// Command options for upgrading packages via GistGet.

namespace NuitsJp.GistGet;

/// <summary>
/// Represents CLI options for the upgrade workflow.
/// </summary>
public record UpgradeOptions
{
    public required string Id { get; init; }
    public string? Manifest { get; init; }
    public string? Name { get; init; }
    public string? Moniker { get; init; }
    public string? Version { get; init; }
    public string? Source { get; init; }
    public string? Scope { get; init; }
    public string? Architecture { get; init; }
    public string? Location { get; init; }
    public string? Locale { get; init; }
    public string? Log { get; init; }
    public string? Custom { get; init; }
    public string? Override { get; init; }
    public string? InstallerType { get; init; }
    public string? Header { get; init; }
    public string? AuthenticationMode { get; init; }
    public string? AuthenticationAccount { get; init; }
    public bool Exact { get; init; }
    public bool Interactive { get; init; }
    public bool Silent { get; init; }
    public bool Purge { get; init; }
    public bool Force { get; init; }
    public bool AcceptPackageAgreements { get; init; }
    public bool AcceptSourceAgreements { get; init; }
    public bool AllowHashMismatch { get; init; }
    public bool IgnoreLocalArchiveMalwareScan { get; init; }
    public bool SkipDependencies { get; init; }
    public bool IncludeUnknown { get; init; }
    public bool IncludePinned { get; init; }
    public bool UninstallPrevious { get; init; }
    public bool AllowReboot { get; init; }
    public bool Wait { get; init; }
    public bool OpenLogs { get; init; }
    public bool VerboseLogs { get; init; }
    public bool IgnoreWarnings { get; init; }
    public bool DisableInteractivity { get; init; }
    public string? Proxy { get; init; }
    public bool NoProxy { get; init; }
}



