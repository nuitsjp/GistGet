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
    void AfterInstall(string packageId);

    /// <summary>
    /// アンインストール後にGist更新を通知する
    /// </summary>
    /// <param name="packageId">アンインストールされたパッケージID</param>
    void AfterUninstall(string packageId);

    /// <summary>
    /// Gistから同期を実行する
    /// </summary>
    /// <returns>実行結果コード</returns>
    Task<int> SyncAsync();

}