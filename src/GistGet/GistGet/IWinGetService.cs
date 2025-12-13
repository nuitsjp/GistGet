namespace GistGet;

/// <summary>
/// WinGet COM APIを使用したパッケージ情報取得サービスインターフェース。
/// ローカルにインストールされているパッケージの情報を取得します。
/// </summary>
public interface IWinGetService
{
    /// <summary>
    /// パッケージIDでインストール済みパッケージを検索します。
    /// </summary>
    /// <param name="id">検索するパッケージID</param>
    /// <returns>該当するパッケージ情報。見つからない場合はnull。</returns>
    WinGetPackage? FindById(PackageId id);

    /// <summary>
    /// ローカルにインストールされている全パッケージを取得します。
    /// </summary>
    /// <returns>インストール済みパッケージの一覧</returns>
    IReadOnlyList<WinGetPackage> GetAllInstalledPackages();
}
