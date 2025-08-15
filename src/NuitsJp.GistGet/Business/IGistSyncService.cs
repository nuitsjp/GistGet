using NuitsJp.GistGet.Business.Models;

namespace NuitsJp.GistGet.Business;

/// <summary>
/// Gist同期サービスのインターフェース
/// </summary>
public interface IGistSyncService
{
    /// <summary>
    /// インストール後にGist更新を通知する
    /// </summary>
    /// <param name="packageId">インストールされたパッケージID</param>
    Task AfterInstallAsync(string packageId);

    /// <summary>
    /// アンインストール後にGist更新を通知する
    /// </summary>
    /// <param name="packageId">アンインストールされたパッケージID</param>
    Task AfterUninstallAsync(string packageId);

    /// <summary>
    /// Gistから同期を実行する
    /// </summary>
    /// <returns>同期結果</returns>
    Task<SyncResult> SyncAsync();

    /// <summary>
    /// システム再起動を実行する
    /// </summary>
    Task ExecuteRebootAsync();
}