using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.Auth;

/// <summary>
/// 認証コマンド固有のコンソール入出力実装
/// GitHub OAuth Device Flow用の高レベル操作を提供
/// </summary>
public class AuthConsole : ConsoleBase, IAuthConsole
{
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
    /// <summary>
    /// 認証手順を表示（Device Flow用）
    /// </summary>
    public void ShowAuthInstructions(string deviceCode, string userCode, string verificationUri)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("=== GitHub認証 ===");
        System.Console.WriteLine($"1. ブラウザで以下のURLを開いてください:");
        System.Console.WriteLine($"   {verificationUri}");
        System.Console.WriteLine();
        System.Console.WriteLine($"2. 以下のコードを入力してください:");
        System.Console.WriteLine($"   {userCode}");
        System.Console.WriteLine();
        System.Console.WriteLine("3. ブラウザでの認証完了後、Enterキーを押してください...");
        System.Console.ReadLine();
    }

    /// <summary>
    /// トークン保存の確認を取る
    /// </summary>
    public bool ConfirmTokenStorage()
    {
        System.Console.WriteLine();
        System.Console.Write("認証トークンをローカルに保存しますか？ (y/N): ");
        var input = System.Console.ReadLine()?.Trim().ToLower();
        return input == "y" || input == "yes";
    }

    /// <summary>
    /// 認証成功を通知
    /// </summary>
    public void NotifyAuthSuccess()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("✅ GitHub認証に成功しました");
    }

    /// <summary>
    /// 認証失敗を通知
    /// </summary>
    public void NotifyAuthFailure(string reason)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"❌ GitHub認証に失敗しました: {reason}");
    }

    /// <summary>
    /// 認証状態を表示
    /// </summary>
    public void ShowAuthStatus(bool isAuthenticated, string? tokenInfo = null)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("=== GitHub認証状態 ===");

        if (isAuthenticated)
        {
            System.Console.WriteLine("✅ 認証済み");
            if (!string.IsNullOrEmpty(tokenInfo))
            {
                System.Console.WriteLine($"トークン情報: {tokenInfo}");
            }
        }
        else
        {
            System.Console.WriteLine("❌ 未認証");
            System.Console.WriteLine("'gistget auth' を実行して認証してください");
        }
    }
}
