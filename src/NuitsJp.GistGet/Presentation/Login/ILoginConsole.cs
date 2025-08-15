using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.Login;

/// <summary>
/// ログインコンソール表示のインターフェース
/// GitHub OAuth Device Flow用の高レベル抽象化を提供
/// </summary>
public interface ILoginConsole : IConsoleBase
{
    /// <summary>
    /// ログイン手順を表示（Device Flow用）
    /// </summary>
    /// <param name="deviceCode">デバイスコード</param>
    /// <param name="userCode">ユーザーコード</param>
    /// <param name="verificationUri">認証URL</param>
    void ShowAuthInstructions(string deviceCode, string userCode, string verificationUri);

    /// <summary>
    /// トークン保存の確認を取る
    /// </summary>
    /// <returns>true: 保存許可, false: 保存拒否</returns>
    bool ConfirmTokenStorage();

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