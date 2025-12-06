using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.Login;

/// <summary>
/// ログアウトコマンド固有のコンソール入出力実装
/// GitHubログアウト用の高レベル操作を提供
/// </summary>
public class LogoutConsole : ConsoleBase, ILogoutConsole
{
    /// <summary>
    /// ログアウト確認を取る
    /// </summary>
    public bool ConfirmLogout()
    {
        System.Console.WriteLine();
        System.Console.Write("GitHubからログアウトしますか？ (y/N): ");
        var input = System.Console.ReadLine()?.Trim().ToLower();
        return input == "y" || input == "yes";
    }

    /// <summary>
    /// ログアウト成功を通知
    /// </summary>
    public void NotifyLogoutSuccess()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("✅ GitHubからログアウトしました");
        System.Console.WriteLine("認証トークンが削除されました。");
    }

    /// <summary>
    /// ログアウト失敗を通知
    /// </summary>
    public void NotifyLogoutFailure(string message)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"❌ ログアウトに失敗しました: {message}");
    }

    /// <summary>
    /// エラーメッセージを出力
    /// </summary>
    protected override void WriteErrorLine(string message)
    {
        System.Console.WriteLine($"❌ {message}");
    }

    /// <summary>
    /// 警告メッセージを出力
    /// </summary>
    protected override void WriteWarningLine(string message)
    {
        System.Console.WriteLine($"⚠️  {message}");
    }

    /// <summary>
    /// 情報メッセージを出力
    /// </summary>
    protected override void WriteInfoLine(string message)
    {
        System.Console.WriteLine($"ℹ️  {message}");
    }
}