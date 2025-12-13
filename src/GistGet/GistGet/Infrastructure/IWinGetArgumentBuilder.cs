using System.Collections.Generic;

namespace GistGet.Infrastructure;

/// <summary>
/// WinGet コマンドの引数を構築するビルダーインターフェース。
/// </summary>
public interface IWinGetArgumentBuilder
{
    /// <summary>
    /// install コマンドの引数を構築します。
    /// </summary>
    string[] BuildInstallArgs(InstallOptions options);

    /// <summary>
    /// GistGetPackage から install コマンドの引数を構築します。
    /// </summary>
    string[] BuildInstallArgs(GistGetPackage package);

    /// <summary>
    /// upgrade コマンドの引数を構築します。
    /// </summary>
    string[] BuildUpgradeArgs(UpgradeOptions options);

    /// <summary>
    /// uninstall コマンドの引数を構築します。
    /// </summary>
    string[] BuildUninstallArgs(UninstallOptions options);

    /// <summary>
    /// pin add コマンドの引数を構築します。
    /// </summary>
    string[] BuildPinAddArgs(string id, string version, string? pinType = null, bool force = false);
}
