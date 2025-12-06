using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Infrastructure.WinGet;

/// <summary>
/// WinGetクライアントのインターフェース
/// </summary>
public interface IWinGetClient
{
    /// <summary>
    /// COM APIを初期化する
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// パッケージをインストールする
    /// </summary>
    /// <param name="args">インストール引数</param>
    /// <returns>実行結果コード</returns>
    Task<int> InstallPackageAsync(string[] args);

    /// <summary>
    /// パッケージをアンインストールする
    /// </summary>
    /// <param name="args">アンインストール引数</param>
    /// <returns>実行結果コード</returns>
    Task<int> UninstallPackageAsync(string[] args);

    /// <summary>
    /// パッケージをアップグレードする
    /// </summary>
    /// <param name="args">アップグレード引数</param>
    /// <returns>実行結果コード</returns>
    Task<int> UpgradePackageAsync(string[] args);

    /// <summary>
    /// インストール済みパッケージ一覧を取得する
    /// </summary>
    /// <returns>パッケージ情報のリスト</returns>
    Task<List<PackageDefinition>> GetInstalledPackagesAsync();

    /// <summary>
    /// winget.exeに引数をそのまま渡して実行する（パススルー）
    /// </summary>
    /// <param name="args">wingetコマンド引数</param>
    /// <returns>実行結果コード</returns>
    Task<int> ExecutePassthroughAsync(string[] args);
}