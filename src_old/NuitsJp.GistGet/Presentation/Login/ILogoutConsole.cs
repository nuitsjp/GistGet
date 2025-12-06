using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.Login;

/// <summary>
/// ログアウトコンソール表示のインターフェース
/// GitHubログアウト用の高レベル抽象化を提供
/// </summary>
public interface ILogoutConsole : IConsoleBase
{
    /// <summary>
    /// ログアウト確認を取る
    /// </summary>
    /// <returns>true: ログアウト実行, false: キャンセル</returns>
    bool ConfirmLogout();

    /// <summary>
    /// ログアウト成功を通知
    /// </summary>
    void NotifyLogoutSuccess();

    /// <summary>
    /// ログアウト失敗を通知
    /// </summary>
    void NotifyLogoutFailure(string message);
}