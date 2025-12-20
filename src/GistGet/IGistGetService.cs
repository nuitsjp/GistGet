// Public contract for GistGet application workflows.

namespace GistGet;

/// <summary>
/// Defines the main application operations exposed to the CLI layer.
/// </summary>
public interface IGistGetService
{
    /// <summary>
    /// Authenticates via GitHub device flow and stores the credential.
    /// </summary>
    Task AuthLoginAsync();

    /// <summary>
    /// Logs out and removes any stored credential.
    /// </summary>
    void AuthLogout();

    /// <summary>
    /// Displays the current authentication status.
    /// </summary>
    Task AuthStatusAsync();

    /// <summary>
    /// Installs a package and persists it to the manifest.
    /// </summary>
    Task<int> InstallAndSaveAsync(InstallOptions options);

    /// <summary>
    /// Uninstalls a package and updates the manifest.
    /// </summary>
    Task<int> UninstallAndSaveAsync(UninstallOptions options);

    /// <summary>
    /// Upgrades a package and updates the manifest.
    /// </summary>
    Task<int> UpgradeAndSaveAsync(UpgradeOptions options);

    /// <summary>
    /// Adds a pin and persists it to the manifest.
    /// </summary>
    Task PinAddAndSaveAsync(string packageId, string version, string? pinType = null, bool force = false);

    /// <summary>
    /// Removes a pin and updates the manifest.
    /// </summary>
    Task PinRemoveAndSaveAsync(string packageId);

    /// <summary>
    /// Synchronizes the manifest with local state.
    /// </summary>
    Task<SyncResult> SyncAsync(string? url = null, string? filePath = null);

    /// <summary>
    /// Runs a WinGet command without syncing.
    /// </summary>
    Task<int> RunPassthroughAsync(string command, string[] args);

    /// <summary>
    /// Exports installed packages to YAML.
    /// </summary>
    Task<string> ExportAsync(string? outputPath = null);

    /// <summary>
    /// Imports package definitions from a YAML file.
    /// </summary>
    Task ImportAsync(string filePath);

    /// <summary>
    /// Initializes the Gist by interactively selecting installed packages.
    /// </summary>
    Task InitAsync();
}
