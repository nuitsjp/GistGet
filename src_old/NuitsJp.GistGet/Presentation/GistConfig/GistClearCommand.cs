using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business;

namespace NuitsJp.GistGet.Presentation.GistConfig;

/// <summary>
/// gist clearコマンドの実装
/// Gist設定ファイルをクリアする
/// </summary>
public class GistClearCommand(
    IGistManager gistManager,
    IGistConfigConsole console,
    ILogger<GistClearCommand> logger)
{

    /// <summary>
    /// gist clearコマンドを実行
    /// </summary>
    public async Task<int> ExecuteAsync()
    {
        try
        {
            logger.LogInformation("gist clearコマンドを開始します");

            // 現在の設定確認
            if (!await gistManager.IsConfiguredAsync())
            {
                console.NotifyNotConfigured();
                logger.LogInformation("Gist設定がありません");
                return 0; // 既にクリア済みは正常終了
            }

            // クリア確認
            if (!console.ConfirmClearConfiguration())
            {
                console.NotifyOperationCanceled();
                logger.LogInformation("ユーザーがGist設定クリアをキャンセルしました");
                return 0; // キャンセルは正常終了扱い
            }

            // 設定をクリア
            await gistManager.ClearConfigurationAsync();

            console.NotifyConfigurationCleared();
            logger.LogInformation("Gist設定をクリアしました");

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "gist clearコマンドの実行中にエラーが発生しました");
            console.ShowError(ex, "Gist設定のクリア処理でエラーが発生しました");
            return 1;
        }
    }
}