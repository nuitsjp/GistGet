using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.WinGet;

/// <summary>
/// WinGetコマンド用のコンソールインターフェース
/// </summary>
public interface IWinGetConsole : IConsoleBase
{
    /// <summary>
    /// パッケージインストール開始を通知
    /// </summary>
    void NotifyInstallStarting(string packageId);

    /// <summary>
    /// パッケージアンインストール開始を通知
    /// </summary>
    void NotifyUninstallStarting(string packageId);

    /// <summary>
    /// パッケージアップグレード開始を通知
    /// </summary>
    void NotifyUpgradeStarting(string packageId);

    /// <summary>
    /// 操作成功を通知
    /// </summary>
    void NotifyOperationSuccess(string operation, string packageId);

    /// <summary>
    /// Gist更新開始を通知
    /// </summary>
    void NotifyGistUpdateStarting();

    /// <summary>
    /// Gist更新成功を通知
    /// </summary>
    void NotifyGistUpdateSuccess();

    /// <summary>
    /// 再起動確認（必要なパッケージリストを含む）
    /// </summary>
    bool ConfirmRebootWithPackageList(List<string> packagesRequiringReboot);

    /// <summary>
    /// 再起動実行を通知
    /// </summary>
    void NotifyRebootExecuting();
}