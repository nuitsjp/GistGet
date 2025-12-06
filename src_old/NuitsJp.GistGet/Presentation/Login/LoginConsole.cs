using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.Login;

/// <summary>
/// ログインコマンド固有のコンソール入出力実装
/// GitHub OAuth Device Flow用の高レベル操作を提供
/// </summary>
public class LoginConsole : ConsoleBase, ILoginConsole
{
    /// <summary>
    /// ログイン成功を通知
    /// </summary>
    public void NotifyAuthSuccess()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("✅ GitHubログインに成功しました");
    }

    /// <summary>
    /// ログイン失敗を通知
    /// </summary>
    public void NotifyAuthFailure(string reason)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"❌ GitHubログインに失敗しました: {reason}");
    }

    /// <summary>
    /// ログイン状態を表示
    /// </summary>
    public void ShowAuthStatus(bool isAuthenticated, string? tokenInfo = null)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("=== GitHubログイン状態 ===");

        if (isAuthenticated)
        {
            System.Console.WriteLine("✅ ログイン済み");
            if (!string.IsNullOrEmpty(tokenInfo)) System.Console.WriteLine($"ユーザー情報: {tokenInfo}");
        }
        else
        {
            System.Console.WriteLine("❌ 未ログイン");
            System.Console.WriteLine("'gistget login' を実行してログインしてください");
        }
    }

    /// <summary>
    /// エラーメッセージを出力
    /// </summary>
    protected override void WriteErrorLine(string message)
    {
        System.Console.WriteLine(message);
    }

    /// <summary>
    /// 警告メッセージを出力
    /// </summary>
    protected override void WriteWarningLine(string message)
    {
        System.Console.WriteLine(message);
    }

    /// <summary>
    /// 情報メッセージを出力
    /// </summary>
    protected override void WriteInfoLine(string message)
    {
        System.Console.WriteLine(message);
    }
}