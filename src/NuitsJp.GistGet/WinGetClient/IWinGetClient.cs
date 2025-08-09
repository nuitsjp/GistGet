using NuitsJp.GistGet.WinGetClient.Models;

namespace NuitsJp.GistGet.WinGetClient;

/// <summary>
/// Interface for WinGet client operations
/// Provides abstraction layer over COM API and CLI fallback
/// </summary>
public interface IWinGetClient
{
    /// <summary>
    /// Initializes the WinGet client (COM API or CLI fallback)
    /// </summary>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for packages matching the specified criteria
    /// </summary>
    /// <param name="options">Search options</param>
    /// <param name="progress">Progress reporting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching packages</returns>
    Task<IReadOnlyList<WinGetPackage>> SearchPackagesAsync(
        SearchOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists installed packages
    /// </summary>
    /// <param name="options">List options</param>
    /// <param name="progress">Progress reporting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of installed packages</returns>
    Task<IReadOnlyList<WinGetPackage>> ListInstalledPackagesAsync(
        ListOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific package
    /// </summary>
    /// <param name="packageId">Package identifier</param>
    /// <param name="source">Optional source name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Package details</returns>
    Task<WinGetPackage?> GetPackageDetailsAsync(
        string packageId,
        string? source = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs a package
    /// </summary>
    /// <param name="options">Installation options</param>
    /// <param name="progress">Progress reporting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Installation result</returns>
    Task<OperationResult> InstallPackageAsync(
        InstallOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates/upgrades packages
    /// </summary>
    /// <param name="options">Upgrade options</param>
    /// <param name="progress">Progress reporting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upgrade result</returns>
    Task<OperationResult> UpgradePackageAsync(
        UpgradeOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls a package
    /// </summary>
    /// <param name="options">Uninstall options</param>
    /// <param name="progress">Progress reporting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Uninstall result</returns>
    Task<OperationResult> UninstallPackageAsync(
        UninstallOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists available package upgrades
    /// </summary>
    /// <param name="options">Upgrade list options</param>
    /// <param name="progress">Progress reporting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of packages with available upgrades</returns>
    Task<IReadOnlyList<WinGetPackage>> ListUpgradablePackagesAsync(
        ListOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manages package sources
    /// </summary>
    /// <param name="operation">Source operation type</param>
    /// <param name="options">Source operation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> ManageSourceAsync(
        SourceOperation operation,
        SourceOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists configured package sources
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of package sources</returns>
    Task<IReadOnlyList<PackageSource>> ListSourcesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports package list
    /// </summary>
    /// <param name="outputPath">Output file path</param>
    /// <param name="options">Export options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result</returns>
    Task<OperationResult> ExportPackagesAsync(
        string outputPath,
        ExportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports package list
    /// </summary>
    /// <param name="inputPath">Input file path</param>
    /// <param name="options">Import options</param>
    /// <param name="progress">Progress reporting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result</returns>
    Task<OperationResult> ImportPackagesAsync(
        string inputPath,
        ImportOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets client information (COM API availability, version, etc.)
    /// </summary>
    ClientInfo GetClientInfo();

    /// <summary>
    /// Disposes the client and releases resources
    /// </summary>
    void Dispose();
}