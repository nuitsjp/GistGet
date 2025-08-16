namespace NuitsJp.GistGet.Business;

/// <summary>
/// パッケージ管理操作とGist同期を統合するサービス
/// </summary>
public interface IPackageManagementService
{
    /// <summary>
    /// パッケージをインストールし、成功時にGistを更新する
    /// </summary>
    /// <param name="args">wingetコマンド引数</param>
    /// <returns>終了コード</returns>
    Task<int> InstallPackageAsync(string[] args);

    /// <summary>
    /// パッケージをアンインストールし、成功時にGistを更新する
    /// </summary>
    /// <param name="args">wingetコマンド引数</param>
    /// <returns>終了コード</returns>
    Task<int> UninstallPackageAsync(string[] args);

    /// <summary>
    /// パッケージをアップグレードし、成功時にGistを更新する
    /// </summary>
    /// <param name="args">wingetコマンド引数</param>
    /// <returns>終了コード</returns>
    Task<int> UpgradePackageAsync(string[] args);

    /// <summary>
    /// 引数からパッケージIDを抽出する
    /// </summary>
    /// <param name="args">wingetコマンド引数</param>
    /// <returns>パッケージID（見つからない場合はnull）</returns>
    string? ExtractPackageId(string[] args);
}