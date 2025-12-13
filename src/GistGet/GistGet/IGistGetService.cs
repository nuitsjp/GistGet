namespace GistGet;

/// <summary>
/// GistGetの中核サービスインターフェース。
/// GitHub Gistとの同期、WinGetコマンドの実行、認証管理を統合的に提供します。
/// すべてのパッケージ操作はGistの<c>packages.yaml</c>と同期されます。
/// </summary>
public interface IGistGetService
{
    /// <summary>
    /// GitHubへDevice Flowでログインし、資格情報を保存します。
    /// ブラウザでの認証が必要です。
    /// </summary>
    Task AuthLoginAsync();

    /// <summary>
    /// GitHubからログアウトし、保存されている資格情報を削除します。
    /// </summary>
    void AuthLogout();

    /// <summary>
    /// 現在の認証状態を表示します。
    /// ログイン中の場合はユーザー名、トークン情報、スコープを表示します。
    /// </summary>
    Task AuthStatusAsync();

    /// <summary>
    /// パッケージをインストールし、Gistの<c>packages.yaml</c>に保存します。
    /// 既存のPinがある場合はそのバージョンでインストールし、Pinを設定します。
    /// </summary>
    /// <param name="options">インストールオプション（ID、バージョン、各種フラグ）</param>
    Task InstallAndSaveAsync(InstallOptions options);

    /// <summary>
    /// パッケージをアンインストールし、Gistの<c>packages.yaml</c>を更新します。
    /// エントリに<c>uninstall: true</c>が設定され、他デバイスでのsync時にアンインストールされます。
    /// </summary>
    /// <param name="packageId">アンインストールするパッケージのID</param>
    Task UninstallAndSaveAsync(string packageId);

    /// <summary>
    /// パッケージをアップグレードし、Gistの<c>packages.yaml</c>を更新します。
    /// Pinがある場合は新しいバージョンに更新されます。
    /// </summary>
    /// <param name="packageId">アップグレードするパッケージのID</param>
    /// <param name="version">アップグレード先のバージョン（省略時は最新版）</param>
    Task UpgradeAndSaveAsync(string packageId, string? version = null);

    /// <summary>
    /// パッケージをピン留めし、Gistの<c>packages.yaml</c>に保存します。
    /// Pinにより<c>upgrade --all</c>から除外されます。
    /// </summary>
    /// <param name="packageId">Pinするパッケージの ID</param>
    /// <param name="version">Pinするバージョン（ワイルドカード使用可、例: "1.7.*"）</param>
    Task PinAddAndSaveAsync(string packageId, string version);

    /// <summary>
    /// パッケージのピン留めを解除し、Gistの<c>packages.yaml</c>から<c>pin</c>を削除します。
    /// </summary>
    /// <param name="packageId">Pin解除するパッケージのID</param>
    Task PinRemoveAndSaveAsync(string packageId);

    /// <summary>
    /// packages.yamlとローカル状態を同期します。
    /// 差分を検出し、インストール/アンインストール/pin設定を実行します。
    /// </summary>
    /// <param name="url">同期元の YAML URL（省略時は認証ユーザーの Gist）</param>
    /// <returns>同期結果（インストール/アンインストール/失敗したパッケージ一覧）</returns>
    Task<SyncResult> SyncAsync(string? url = null);

    /// <summary>
    /// 指定されたコマンドをWinGetにそのままパススルーで実行します。
    /// list、search、showなどGist同期が不要なコマンドに使用されます。
    /// </summary>
    /// <param name="command">WinGetコマンド（例: list, search, show）</param>
    /// <param name="args">コマンドに渡す引数</param>
    /// <returns>WinGetプロセスの終了コード</returns>
    Task<int> RunPassthroughAsync(string command, string[] args);
}

