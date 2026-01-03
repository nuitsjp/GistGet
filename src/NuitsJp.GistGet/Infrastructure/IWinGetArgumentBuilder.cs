// Abstraction for building WinGet command-line arguments.

namespace NuitsJp.GistGet.Infrastructure;

/// <summary>
/// Defines argument construction for WinGet operations invoked by the app.
/// </summary>
public interface IWinGetArgumentBuilder
{
    /// <summary>
    /// Builds arguments for an install operation.
    /// </summary>
    string[] BuildInstallArgs(InstallOptions options);

    /// <summary>
    /// Builds install arguments from a package entry.
    /// </summary>
    string[] BuildInstallArgs(GistGetPackage package);

    /// <summary>
    /// Builds arguments for an upgrade operation.
    /// </summary>
    string[] BuildUpgradeArgs(UpgradeOptions options);

    /// <summary>
    /// Builds arguments for an uninstall operation.
    /// </summary>
    string[] BuildUninstallArgs(UninstallOptions options);

    /// <summary>
    /// Builds arguments for a pin add operation.
    /// </summary>
    string[] BuildPinAddArgs(string id, string version, string? pinType = null, bool force = false);
}




