namespace GistGet;

/// <summary>
/// GitHub APIとの通信を担当するサービスインターフェース。
/// Device Flow認証とGist操作を提供します。
/// </summary>
/// <remarks>
/// <para><b>Gist特定ルール:</b></para>
/// <list type="bullet">
///   <item>
///     <term>gistUrl指定時</term>
///     <description>URLからGist IDを抽出し、そのGistを直接取得します。</description>
///   </item>
///   <item>
///     <term>gistUrl未指定時</term>
///     <description>
///       認証ユーザーの全Gistを検索し、以下のいずれかの条件に一致するGistを特定します:
///       <list type="number">
///         <item>指定されたファイル名（gistFileName）を含むGist</item>
///         <item>指定された説明（gistDescription）と一致するGist</item>
///       </list>
///     </description>
///   </item>
///   <item>
///     <term>複数Gist一致時</term>
///     <description>
///       条件に一致するGistが複数存在する場合、<see cref="InvalidOperationException"/>をスローし、
///       明示的なgistURL指定を要求します。
///     </description>
///   </item>
/// </list>
/// </remarks>
public interface IGitHubService
{
    /// <summary>
    /// GitHub Device Flowを使用してログインします。
    /// ユーザーはブラウザでコードを入力して認証を完了します。
    /// </summary>
    /// <returns>認証成功時の資格情報（ユーザー名とアクセストークン）</returns>
    Task<Credential> LoginAsync();

    /// <summary>
    /// 指定された URL から packages.yaml を取得します。
    /// Gist の Raw URL やその他の HTTP/HTTPS URL を指定可能です。
    /// </summary>
    /// <param name="url">YAML ファイルの URL</param>
    /// <returns>パッケージ一覧</returns>
    Task<IReadOnlyList<GistGetPackage>> GetPackagesFromUrlAsync(string url);

    /// <summary>
    /// 認証ユーザーの Gist からパッケージ一覧を取得します。
    /// </summary>
    /// <param name="token">GitHub アクセストークン</param>
    /// <param name="gistFileName">Gist内のファイル名（通常は "packages.yaml"）</param>
    /// <param name="gistDescription">Gistの説明（検索・作成時に使用）</param>
    /// <returns>パッケージ一覧</returns>
    Task<IReadOnlyList<GistGetPackage>> GetPackagesAsync(string token, string gistFileName, string gistDescription);

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
