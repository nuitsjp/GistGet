using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.GistConfig;

/// <summary>
/// Gist設定コマンド固有のコンソール入出力インターフェース
/// Gist設定管理用の高レベル抽象化を提供
/// </summary>
public interface IGistConfigConsole : IConsoleBase
{
    /// <summary>
    /// Gist作成手順を表示
    /// </summary>
    void ShowGistCreationInstructions();

    /// <summary>
    /// Gist設定（IDとファイル名）を取得
    /// </summary>
    /// <param name="providedGistId">コマンドライン引数で提供されたGist ID</param>
    /// <param name="providedFileName">コマンドライン引数で提供されたファイル名</param>
    /// <returns>検証済みのGist IDとファイル名のタプル。失敗時はnull</returns>
    (string gistId, string fileName)? RequestGistConfiguration(string? providedGistId, string? providedFileName);

    /// <summary>
    /// Gist設定保存成功を通知
    /// </summary>
    /// <param name="gistId">設定されたGist ID</param>
    /// <param name="fileName">設定されたファイル名</param>
    void NotifyConfigurationSaved(string gistId, string fileName);

    /// <summary>
    /// Gist設定状態を表示
    /// </summary>
    /// <param name="isAuthenticated">GitHub認証済みかどうか</param>
    /// <param name="isConfigured">Gist設定済みかどうか</param>
    /// <param name="gistId">設定されたGist ID（設定済みの場合）</param>
    /// <param name="fileName">設定されたファイル名（設定済みの場合）</param>
    void ShowGistStatus(bool isAuthenticated, bool isConfigured, string? gistId = null, string? fileName = null);

    /// <summary>
    /// Gistコンテンツを表示
    /// </summary>
    /// <param name="gistId">Gist ID</param>
    /// <param name="fileName">ファイル名</param>
    /// <param name="content">ファイル内容</param>
    void ShowGistContent(string gistId, string fileName, string content);

    /// <summary>
    /// Gist設定の設定ミスを通知
    /// </summary>
    /// <param name="reason">設定エラーの理由</param>
    void NotifyConfigurationError(string reason);

    /// <summary>
    /// Gist設定クリアの確認を取る
    /// </summary>
    bool ConfirmClearConfiguration();

    /// <summary>
    /// Gist設定クリア成功を通知
    /// </summary>
    void NotifyConfigurationCleared();

    /// <summary>
    /// Gist設定が既にクリア済みであることを通知
    /// </summary>
    void NotifyNotConfigured();

    /// <summary>
    /// 操作キャンセルを通知
    /// </summary>
    void NotifyOperationCanceled();
}