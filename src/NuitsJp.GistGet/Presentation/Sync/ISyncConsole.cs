using NuitsJp.GistGet.Business.Models;
using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.Sync;

/// <summary>
/// Syncコマンド固有のコンソールインターフェース
/// </summary>
public interface ISyncConsole : IConsoleBase
{
    /// <summary>
    /// 同期開始を通知
    /// </summary>
    void NotifySyncStarting();

    /// <summary>
    /// 同期結果を表示し、ユーザーアクションを取得
    /// </summary>
    /// <param name="result">同期結果</param>
    /// <returns>ユーザーアクション</returns>
    SyncUserAction ShowSyncResultAndGetAction(SyncResult result);

    /// <summary>
    /// 再起動確認（必要なパッケージリストを含む）
    /// </summary>
    /// <param name="packagesRequiringReboot">再起動が必要なパッケージリスト</param>
    /// <returns>再起動するかどうか</returns>
    bool ConfirmRebootWithPackageList(List<string> packagesRequiringReboot);

    /// <summary>
    /// 再起動実行を通知
    /// </summary>
    void NotifyRebootExecuting();

    /// <summary>
    /// 未実装機能の通知
    /// </summary>
    /// <param name="featureName">機能名</param>
    void NotifyUnimplementedFeature(string featureName);
}

/// <summary>
/// 同期結果表示後のユーザーアクション
/// </summary>
public enum SyncUserAction
{
    /// <summary>継続（通常の処理）</summary>
    Continue,
    /// <summary>再起動をスキップ</summary>
    SkipReboot,
    /// <summary>強制再起動</summary>
    ForceReboot,
    /// <summary>キャンセル</summary>
    Cancel
}