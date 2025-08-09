namespace NuitsJp.GistGet.WinGetClient.Models;

/// <summary>
/// Represents a WinGet package with metadata
/// </summary>
public record WinGetPackage
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string? AvailableVersion { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? Publisher { get; init; }
    public string? Description { get; init; }
    public string? Homepage { get; init; }
    public string? License { get; init; }
    public string[]? Tags { get; init; }
    public string? Moniker { get; init; }
    public string? Architecture { get; init; }
    public string? InstallerType { get; init; }
    public string? Scope { get; init; }
    public bool IsInstalled { get; init; }
    public bool HasUpgrade => !string.IsNullOrEmpty(AvailableVersion) && AvailableVersion != Version;
}

/// <summary>
/// Represents a package source configuration
/// </summary>
public record PackageSource
{
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? TrustLevel { get; init; }
    public bool IsEnabled { get; init; } = true;
    public DateTime? LastUpdated { get; init; }
}

/// <summary>
/// Progress information for long-running operations
/// </summary>
public record OperationProgress
{
    public string Phase { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int? ProgressPercentage { get; init; }
    public string? CurrentPackage { get; init; }
    public int? CompletedItems { get; init; }
    public int? TotalItems { get; init; }
    public TimeSpan? ElapsedTime { get; init; }
    public TimeSpan? EstimatedRemainingTime { get; init; }
}

/// <summary>
/// Result of a WinGet operation
/// </summary>
public record OperationResult
{
    public bool IsSuccess { get; init; }
    public int ExitCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ErrorDetails { get; init; }
    public string[]? OutputLines { get; init; }
    public string[]? ErrorLines { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public bool UsedComApi { get; init; }
    public Exception? Exception { get; init; }

    public static OperationResult Success(string message = "Operation completed successfully", bool usedComApi = true)
        => new() { IsSuccess = true, Message = message, UsedComApi = usedComApi };

    public static OperationResult Failure(string message, int exitCode = 1, Exception? exception = null, bool usedComApi = false)
        => new() { IsSuccess = false, Message = message, ExitCode = exitCode, Exception = exception, UsedComApi = usedComApi };
}

/// <summary>
/// Search options for package operations
/// </summary>
public record SearchOptions
{
    public string? Query { get; init; }
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Moniker { get; init; }
    public string? Tag { get; init; }
    public string? Command { get; init; }
    public string? Source { get; init; }
    public int? Count { get; init; }
    public bool Exact { get; init; }
}

/// <summary>
/// List options for installed packages
/// </summary>
public record ListOptions
{
    public string? Query { get; init; }
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Moniker { get; init; }
    public string? Source { get; init; }
    public string? Tag { get; init; }
    public bool Exact { get; init; }
    public bool UpgradeAvailable { get; init; }
    public bool IncludeUnknown { get; init; }
    public int? Count { get; init; }
    public bool AcceptSourceAgreements { get; init; }
}

/// <summary>
/// Installation options
/// </summary>
public record InstallOptions
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Moniker { get; init; }
    public string? Query { get; init; }
    public string? Version { get; init; }
    public string? Source { get; init; }
    public string? Architecture { get; init; }
    public string? InstallerType { get; init; }
    public string? Scope { get; init; }
    public string? Location { get; init; }
    public string? Locale { get; init; }
    public bool Interactive { get; init; }
    public bool Silent { get; init; }
    public bool Exact { get; init; }
    public bool Force { get; init; }
    public string[]? OverrideArgs { get; init; }
    public bool AcceptPackageAgreements { get; init; }
    public bool AcceptSourceAgreements { get; init; }
}

/// <summary>
/// Upgrade options
/// </summary>
public record UpgradeOptions
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Moniker { get; init; }
    public string? Query { get; init; }
    public string? Version { get; init; }
    public string? Source { get; init; }
    public string? Architecture { get; init; }
    public string? InstallerType { get; init; }
    public string? Locale { get; init; }
    public bool All { get; init; }
    public bool IncludeUnknown { get; init; }
    public bool Interactive { get; init; }
    public bool Silent { get; init; }
    public bool Exact { get; init; }
    public bool Force { get; init; }
    public bool AcceptPackageAgreements { get; init; }
    public bool AcceptSourceAgreements { get; init; }
}

/// <summary>
/// Uninstall options
/// </summary>
public record UninstallOptions
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Moniker { get; init; }
    public string? Query { get; init; }
    public string? Version { get; init; }
    public string? Source { get; init; }
    public bool Interactive { get; init; }
    public bool Silent { get; init; }
    public bool Exact { get; init; }
    public bool Force { get; init; }
    public bool Purge { get; init; }
}

/// <summary>
/// Source operation types
/// </summary>
public enum SourceOperation
{
    Add,
    List,
    Update,
    Remove,
    Reset,
    Export
}

/// <summary>
/// Source management options
/// </summary>
public record SourceOptions
{
    public string? Name { get; init; }
    public string? Url { get; init; }
    public string? Type { get; init; }
    public string? TrustLevel { get; init; }
    public bool Force { get; init; }
}

/// <summary>
/// Export options
/// </summary>
public record ExportOptions
{
    public string? Source { get; init; }
    public bool IncludeVersions { get; init; }
    public bool AcceptSourceAgreements { get; init; }
}

/// <summary>
/// Import options
/// </summary>
public record ImportOptions
{
    public bool IgnoreUnavailable { get; init; }
    public bool IgnoreVersions { get; init; }
    public bool AcceptPackageAgreements { get; init; }
    public bool AcceptSourceAgreements { get; init; }
}

/// <summary>
/// Client information
/// </summary>
public record ClientInfo
{
    public bool ComApiAvailable { get; init; }
    public string? ComApiVersion { get; init; }
    public bool CliAvailable { get; init; }
    public string? CliVersion { get; init; }
    public string? CliPath { get; init; }
    public ClientMode ActiveMode { get; init; }
    public string[]? AvailableSources { get; init; }
    public Dictionary<string, object>? AdditionalInfo { get; init; }
}

/// <summary>
/// Client operation mode
/// </summary>
public enum ClientMode
{
    ComApi,
    CliFallback,
    Unavailable
}