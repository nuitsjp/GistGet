using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.Auth;

/// <summary>
/// 認証コマンド固有のコンソール入出力インターフェース
/// GitHub OAuth Device Flow用の高レベル抽象化を提供
/// </summary>
public interface IAuthConsole : IConsoleBase
{
    /// <summary>
    /// 認証手順を表示（Device Flow用）
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
    /// 認証成功を通知
    /// </summary>
    void NotifyAuthSuccess();

    /// <summary>
    /// 認証失敗を通知
    /// </summary>
    /// <param name="reason">失敗理由</param>
    void NotifyAuthFailure(string reason);

    /// <summary>
    /// 認証状態を表示
    /// </summary>
    /// <param name="isAuthenticated">認証済みかどうか</param>
    /// <param name="tokenInfo">トークン情報（認証済みの場合）</param>
    void ShowAuthStatus(bool isAuthenticated, string? tokenInfo = null);
}
