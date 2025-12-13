namespace GistGet;

/// <summary>
/// コンソール入出力を抽象化するサービスインターフェース。
/// ユーザーへのメッセージ表示やクリップボード操作を提供します。
/// </summary>
public interface IConsoleService
{
    /// <summary>
    /// 情報メッセージをコンソールに出力します。
    /// </summary>
    /// <param name="message">出力するメッセージ</param>
    void WriteInfo(string message);

    /// <summary>
    /// 警告メッセージをコンソールに出力します。
    /// </summary>
    /// <param name="message">出力する警告メッセージ</param>
    void WriteWarning(string message);

    /// <summary>
    /// コンソールから1行読み取ります。
    /// </summary>
    /// <returns>読み取った文字列。入力がない場合はnull。</returns>
    string? ReadLine();

    /// <summary>
    /// テキストをクリップボードに設定します。
    /// Device Flow認証時のユーザーコードコピーなどに使用されます。
    /// </summary>
    /// <param name="text">クリップボードに設定するテキスト</param>
    void SetClipboard(string text);
}