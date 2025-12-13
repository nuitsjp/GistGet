namespace GistGet;

/// <summary>
/// GitHub APIとの通信を担当するサービスインターフェース。
/// Device Flow認証とGist操作を提供します。
/// </summary>
public interface IGitHubService
{
    /// <summary>
    /// GitHub Device Flowを使用してログインします。
    /// ユーザーはブラウザでコードを入力して認証を完了します。
    /// </summary>
    /// <returns>認証成功時の資格情報（ユーザー名とアクセストークン）</returns>
    Task<Credential> LoginAsync();

    /// <summary>
    /// Gistからパッケージ一覧を取得します。
    /// </summary>
    /// <param name="token">GitHub アクセストークン</param>
    /// <param name="gistUrl">Gist URL（空文字列の場合は認証ユーザーのGistを検索）</param>
    /// <param name="gistFileName">Gist内のファイル名（通常は "packages.yaml"）</param>
    /// <param name="gistDescription">Gistの説明（検索・作成時に使用）</param>
    /// <returns>パッケージ一覧</returns>
    Task<IReadOnlyList<GistGetPackage>> GetPackagesAsync(string token, string gistUrl, string gistFileName, string gistDescription);

    /// <summary>
    /// パッケージ一覧をGistに保存します。
    /// Gistが存在しない場合は新規作成します。
    /// </summary>
    /// <param name="token">GitHub アクセストークン</param>
    /// <param name="gistUrl">Gist URL（空文字列の場合は認証ユーザーのGistを検索または作成）</param>
    /// <param name="gistFileName">Gist内のファイル名（通常は "packages.yaml"）</param>
    /// <param name="gistDescription">Gistの説明</param>
    /// <param name="packages">保存するパッケージ一覧</param>
    Task SavePackagesAsync(string token, string gistUrl, string gistFileName, string gistDescription, IReadOnlyList<GistGetPackage> packages);

    /// <summary>
    /// GitHubトークンの状態を取得します。
    /// ユーザー名とスコープ情報を返します。
    /// </summary>
    /// <param name="token">確認するアクセストークン</param>
    /// <returns>トークンのステータス（ユーザー名とスコープ）</returns>
    Task<TokenStatus> GetTokenStatusAsync(string token);
}

/// <summary>
/// GitHubトークンのステータス情報。
/// </summary>
/// <param name="Username">トークンに関連付けられたユーザー名</param>
/// <param name="Scopes">トークンに付与されたスコープ一覧</param>
public record TokenStatus(string Username, IReadOnlyList<string> Scopes);
