using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business;

namespace NuitsJp.GistGet.Presentation.GistConfig;

/// <summary>
/// gist clearコマンドの実装
/// Gist設定ファイルをクリアする
/// </summary>
public class GistClearCommand
{
    private readonly IGistManager _gistManager;
    private readonly IGistConfigConsole _console;
    private readonly ILogger<GistClearCommand> _logger;

    public GistClearCommand(
        IGistManager gistManager,
        IGistConfigConsole console,
        ILogger<GistClearCommand> logger)
    {
        _gistManager = gistManager;
        _console = console;
        _logger = logger;
    }

    /// <summary>
    /// gist clearコマンドを実行
    /// </summary>
    public async Task<int> ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("gist clearコマンドを開始します");

            // 現在の設定確認
            if (!await _gistManager.IsConfiguredAsync())
            {
                _console.NotifyNotConfigured();
                _logger.LogInformation("Gist設定がありません");
                return 0; // 既にクリア済みは正常終了
            }

            // クリア確認
            if (!_console.ConfirmClearConfiguration())
            {
                _console.NotifyOperationCanceled();
                _logger.LogInformation("ユーザーがGist設定クリアをキャンセルしました");
                return 0; // キャンセルは正常終了扱い
            }

            // 設定をクリア
            await _gistManager.ClearConfigurationAsync();

            _console.NotifyConfigurationCleared();
            _logger.LogInformation("Gist設定をクリアしました");

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gist clearコマンドの実行中にエラーが発生しました");
            _console.ShowError(ex, "Gist設定のクリア処理でエラーが発生しました");
            return 1;
        }
    }
}