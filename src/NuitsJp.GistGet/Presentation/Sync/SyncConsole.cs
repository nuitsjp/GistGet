using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business.Models;
using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.Sync;

/// <summary>
/// Syncコマンド用コンソール実装（標準版）
/// </summary>
public class SyncConsole(ILogger<SyncConsole>? logger = null) : ConsoleBase(logger), ISyncConsole
{

    /// <summary>
    /// 同期開始を通知
    /// </summary>
    public void NotifySyncStarting()
    {
        WriteInfoLine("Gistからパッケージ同期を開始します...");
    }

    /// <summary>
    /// 同期結果を表示し、ユーザーアクションを取得
    /// </summary>
    public SyncUserAction ShowSyncResultAndGetAction(SyncResult result)
    {
        DisplaySyncResult(result);

        // 標準実装では常に継続
        return SyncUserAction.Continue;
    }

    /// <summary>
    /// 再起動確認（必要なパッケージリストを含む）
    /// </summary>
    public bool ConfirmRebootWithPackageList(List<string> packagesRequiringReboot)
    {
        WriteInfoLine("同期が完了しました。");
        WriteInfoLine("再起動が必要なパッケージがインストールされました：");

        foreach (var package in packagesRequiringReboot) System.Console.WriteLine($"  - {package}");

        return PromptForConfirmation("今すぐ再起動しますか？", false);
    }

    /// <summary>
    /// 再起動実行を通知
    /// </summary>
    public void NotifyRebootExecuting()
    {
        WriteWarningLine("システムを再起動しています...");
    }

    /// <summary>
    /// 未実装機能の通知
    /// </summary>
    public void NotifyUnimplementedFeature(string featureName)
    {
        WriteErrorLine($"{featureName}は現在未実装です。");
    }

    /// <summary>
    /// 同期結果を表示
    /// </summary>
    private void DisplaySyncResult(SyncResult result)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("=== 同期結果 ===");

        if (result.InstalledPackages.Count > 0)
        {
            WriteSuccessLine($"インストール済み ({result.InstalledPackages.Count}件):");
            foreach (var package in result.InstalledPackages) System.Console.WriteLine($"  + {package}");
        }

        if (result.UninstalledPackages.Count > 0)
        {
            WriteWarningLine($"アンインストール済み ({result.UninstalledPackages.Count}件):");
            foreach (var package in result.UninstalledPackages) System.Console.WriteLine($"  - {package}");
        }

        if (result.FailedPackages.Count > 0)
        {
            WriteErrorLine($"失敗 ({result.FailedPackages.Count}件):");
            foreach (var package in result.FailedPackages) System.Console.WriteLine($"  ✗ {package}");
        }

        if (!result.HasChanges) WriteInfoLine("変更はありませんでした。システムは既に同期されています。");

        System.Console.WriteLine();

        if (result.IsSuccess)
            WriteSuccessLine("✓ 同期が正常に完了しました。");
        else
            WriteErrorLine("⚠ 同期中にエラーが発生しました。");

        if (result.RebootRequired) WriteWarningLine("⚠ 再起動が必要です。");
    }

    /// <summary>
    /// 確認プロンプトを表示
    /// </summary>
    private static bool PromptForConfirmation(string message, bool defaultValue = false)
    {
        var defaultText = defaultValue ? "[Y/n]" : "[y/N]";
        System.Console.Write($"{message} {defaultText}: ");

        var input = System.Console.ReadLine()?.Trim().ToLowerInvariant();

        return input switch
        {
            "y" or "yes" => true,
            "n" or "no" => false,
            "" => defaultValue,
            _ => defaultValue
        };
    }

    protected override void WriteErrorLine(string message)
    {
        var originalColor = System.Console.ForegroundColor;
        try
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine(message);
        }
        finally
        {
            System.Console.ForegroundColor = originalColor;
        }
    }

    protected override void WriteWarningLine(string message)
    {
        var originalColor = System.Console.ForegroundColor;
        try
        {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(message);
        }
        finally
        {
            System.Console.ForegroundColor = originalColor;
        }
    }

    protected override void WriteInfoLine(string message)
    {
        var originalColor = System.Console.ForegroundColor;
        try
        {
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine(message);
        }
        finally
        {
            System.Console.ForegroundColor = originalColor;
        }
    }

    private static void WriteSuccessLine(string message)
    {
        var originalColor = System.Console.ForegroundColor;
        try
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine(message);
        }
        finally
        {
            System.Console.ForegroundColor = originalColor;
        }
    }
}