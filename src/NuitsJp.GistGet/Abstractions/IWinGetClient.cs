namespace NuitsJp.GistGet.Abstractions;

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
    Task<List<(string Id, string Name, string Version)>> GetInstalledPackagesAsync();
}

/// <summary>
/// WinGetパススルークライアントのインターフェース
/// </summary>
public interface IWinGetPassthroughClient
{
    /// <summary>
    /// winget.exeに引数をそのまま渡して実行する
    /// </summary>
    /// <param name="args">wingetコマンド引数</param>
    /// <returns>実行結果コード</returns>
    Task<int> ExecuteAsync(string[] args);
}

/// <summary>
/// プロセス実行の抽象化インターフェース
/// </summary>
public interface IProcessWrapper
{
    /// <summary>
    /// プロセスを開始する
    /// </summary>
    /// <param name="startInfo">プロセス開始情報</param>
    /// <returns>開始されたプロセス</returns>
    IProcessResult? Start(System.Diagnostics.ProcessStartInfo startInfo);
}

/// <summary>
/// プロセス実行結果の抽象化
/// </summary>
public interface IProcessResult : IDisposable
{
    /// <summary>
    /// プロセスの終了を待機する
    /// </summary>
    Task WaitForExitAsync();

    /// <summary>
    /// プロセスの終了コード
    /// </summary>
    int ExitCode { get; }
}