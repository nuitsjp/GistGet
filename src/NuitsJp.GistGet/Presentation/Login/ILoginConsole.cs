using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.Login;

/// <summary>
/// ログインコンソール表示のインターフェース
/// GitHub OAuth Device Flow用の高レベル抽象化を提供
/// </summary>
public interface ILoginConsole : IConsoleBase
{
    /// <summary>
    /// ログイン成功を通知
    /// </summary>
    void NotifyAuthSuccess();

    /// <summary>
    /// ログイン失敗を通知
    /// </summary>
    void NotifyAuthFailure(string message);

    /// <summary>
    /// ログイン状態を表示
    /// </summary>
    void ShowAuthStatus(bool isAuthenticated, string? tokenInfo);
}