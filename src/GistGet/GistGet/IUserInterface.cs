namespace GistGet;

/// <summary>
/// ユーザーインターフェースの抽象化レイヤー。
/// テスト時にモック可能な出力インターフェースを提供します。
/// </summary>
public interface IUserInterface
{
    /// <summary>
    /// メッセージを出力して改行します。
    /// </summary>
    /// <param name="message">出力するメッセージ</param>
    void WriteLine(string message);
}