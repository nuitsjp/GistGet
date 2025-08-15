namespace NuitsJp.GistGet.Presentation.Console;

/// <summary>
/// コンソール入出力サービスの共通基盤インターフェース
/// 全コマンドで共通の機能を提供
/// </summary>
public interface IConsoleBase
{
    /// <summary>
    /// エラーを表示（詳細度は実装に委ねる）
    /// </summary>
    /// <param name="exception">発生した例外</param>
    /// <param name="userFriendlyMessage">ユーザー向けメッセージ（省略時は例外メッセージを使用）</param>
    void ShowError(Exception exception, string? userFriendlyMessage = null);

    /// <summary>
    /// 警告を表示
    /// </summary>
    /// <param name="message">警告メッセージ</param>
    void ShowWarning(string message);

    /// <summary>
    /// 進捗状況を表示（長時間処理用）
    /// </summary>
    /// <param name="operation">実行中の操作名</param>
    /// <returns>Disposeで進捗を終了</returns>
    IDisposable BeginProgress(string operation);
}

/// <summary>
/// 進捗表示インターフェース
/// </summary>
public interface IProgressIndicator : IDisposable
{
    /// <summary>
    /// 進捗メッセージを更新
    /// </summary>
    /// <param name="message">進捗メッセージ</param>
    void UpdateMessage(string message);

    /// <summary>
    /// 進捗を完了状態にする
    /// </summary>
    void Complete();
}