using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Business.Models;

namespace NuitsJp.GistGet.Presentation.Commands;

/// <summary>
/// syncコマンドの実装
/// Gist → ローカルの一方向同期を実行
/// </summary>
public class SyncCommand
{
    private readonly IGistSyncService _gistSyncService;
    private readonly ILogger<SyncCommand> _logger;

    public SyncCommand(IGistSyncService gistSyncService, ILogger<SyncCommand> logger)
    {
        _gistSyncService = gistSyncService;
        _logger = logger;
    }

    /// <summary>
    /// syncコマンドを実行
    /// </summary>
    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            _logger.LogInformation("Starting sync command");

            // オプション解析（将来的な拡張用）
            var dryRun = args.Contains("--dry-run");
            var forceReboot = args.Contains("--force-reboot");
            var skipReboot = args.Contains("--skip-reboot");

            if (dryRun)
            {
                Console.WriteLine("ドライランモードは現在未実装です。");
                return 1;
            }

            // 同期実行
            Console.WriteLine("Gistからパッケージ同期を開始します...");
            var result = await _gistSyncService.SyncAsync();

            // 結果表示
            DisplayResults(result);

            // 再起動処理
            if (result.RebootRequired && !skipReboot)
            {
                var shouldReboot = forceReboot || await PromptForReboot(result.InstalledPackages);
                if (shouldReboot)
                {
                    Console.WriteLine("システムを再起動しています...");
                    await _gistSyncService.ExecuteRebootAsync();
                }
            }

            return result.ExitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "syncコマンドの実行中にエラーが発生しました");
            Console.WriteLine($"エラー: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// 同期結果を表示
    /// </summary>
    private void DisplayResults(SyncResult result)
    {
        Console.WriteLine();
        Console.WriteLine("=== 同期結果 ===");

        if (result.InstalledPackages.Count > 0)
        {
            Console.WriteLine($"インストール済み ({result.InstalledPackages.Count}件):");
            foreach (var package in result.InstalledPackages)
            {
                Console.WriteLine($"  + {package}");
            }
        }

        if (result.UninstalledPackages.Count > 0)
        {
            Console.WriteLine($"アンインストール済み ({result.UninstalledPackages.Count}件):");
            foreach (var package in result.UninstalledPackages)
            {
                Console.WriteLine($"  - {package}");
            }
        }

        if (result.FailedPackages.Count > 0)
        {
            Console.WriteLine($"失敗 ({result.FailedPackages.Count}件):");
            foreach (var package in result.FailedPackages)
            {
                Console.WriteLine($"  ✗ {package}");
            }
        }

        if (!result.HasChanges)
        {
            Console.WriteLine("変更はありませんでした。システムは既に同期されています。");
        }

        Console.WriteLine();

        if (result.IsSuccess)
        {
            Console.WriteLine("✓ 同期が正常に完了しました。");
        }
        else
        {
            Console.WriteLine("⚠ 同期中にエラーが発生しました。");
        }

        if (result.RebootRequired)
        {
            Console.WriteLine("⚠ 再起動が必要です。");
        }
    }

    /// <summary>
    /// ユーザーに再起動の確認を行う
    /// </summary>
    private Task<bool> PromptForReboot(List<string> installedPackages)
    {
        Console.WriteLine();
        Console.WriteLine("同期が完了しました。");
        Console.WriteLine("再起動が必要なパッケージがインストールされました：");

        foreach (var package in installedPackages)
        {
            Console.WriteLine($"  - {package}");
        }

        Console.WriteLine();
        Console.Write("今すぐ再起動しますか？ (Y/N): ");

        while (true)
        {
            var key = Console.ReadKey(true);
            Console.WriteLine(key.KeyChar);

            switch (key.KeyChar.ToString().ToUpper())
            {
                case "Y":
                    return Task.FromResult(true);
                case "N":
                    Console.WriteLine("再起動をスキップしました。後で手動で再起動してください。");
                    return Task.FromResult(false);
                default:
                    Console.Write("Y または N を入力してください: ");
                    break;
            }
        }
    }
}