using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business;

namespace NuitsJp.GistGet.Presentation.Sync;

/// <summary>
/// syncコマンドの実装
/// Gist → ローカルの一方向同期を実行
/// </summary>
public class SyncCommand(IGistSyncService gistSyncService, ISyncConsole console, ILogger<SyncCommand> logger)
{
    private readonly ISyncConsole _console = console ?? throw new ArgumentNullException(nameof(console));
    private readonly IGistSyncService _gistSyncService = gistSyncService ?? throw new ArgumentNullException(nameof(gistSyncService));
    private readonly ILogger<SyncCommand> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// syncコマンドを実行
    /// </summary>
    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            _logger.LogInformation("Starting sync command");

            // オプション解析（将来的な拡張用）
            var options = ParseOptions(args);

            if (options.DryRun)
            {
                _console.NotifyUnimplementedFeature("ドライランモード");
                return 1;
            }

            // UIへの通知は高レベルで
            _console.NotifySyncStarting();

            // ビジネスロジック実行
            var result = await _gistSyncService.SyncAsync();

            // 結果表示とユーザーアクション取得を一体化
            var userAction = _console.ShowSyncResultAndGetAction(result);

            // 再起動処理（UIの詳細はConsoleに委譲）
            if (result.RebootRequired && !options.SkipReboot && userAction != SyncUserAction.SkipReboot)
            {
                var shouldReboot = options.ForceReboot || userAction == SyncUserAction.ForceReboot ||
                                   _console.ConfirmRebootWithPackageList(result.InstalledPackages);

                if (shouldReboot)
                {
                    _console.NotifyRebootExecuting();
                    await _gistSyncService.ExecuteRebootAsync();
                }
            }

            return result.ExitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "syncコマンドの実行中にエラーが発生しました");
            _console.ShowError(ex, "同期処理でエラーが発生しました");
            return 1;
        }
    }

    /// <summary>
    /// コマンドラインオプションを解析
    /// </summary>
    private static SyncOptions ParseOptions(string[] args)
    {
        return new SyncOptions
        {
            DryRun = args.Contains("--dry-run"),
            ForceReboot = args.Contains("--force-reboot"),
            SkipReboot = args.Contains("--skip-reboot")
        };
    }

    /// <summary>
    /// Syncコマンドのオプション
    /// </summary>
    private class SyncOptions
    {
        public bool DryRun { get; init; }
        public bool ForceReboot { get; init; }
        public bool SkipReboot { get; init; }
    }
}