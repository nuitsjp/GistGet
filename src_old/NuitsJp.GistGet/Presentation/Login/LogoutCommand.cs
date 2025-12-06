using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Infrastructure.GitHub;

namespace NuitsJp.GistGet.Presentation.Login;

/// <summary>
/// ログアウトコマンドの実装（Command-Console分離版）
/// </summary>
public class LogoutCommand(
    IGitHubAuthService authService,
    ILogoutConsole console,
    ILogger<LogoutCommand> logger)
{

    /// <summary>
    /// ログアウトコマンドを実行
    /// </summary>
    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            logger.LogInformation("ログアウトコマンドを開始します");

            // --silentオプションをチェック
            var isSilent = args.Contains("--silent");

            // サイレントモードでない場合は確認を取る
            if (!isSilent && !console.ConfirmLogout())
            {
                logger.LogInformation("ユーザーがログアウトをキャンセルしました");
                return 0; // キャンセルは正常終了扱い
            }

            // ログアウト実行
            var success = await authService.LogoutAsync();

            if (success)
            {
                console.NotifyLogoutSuccess();
                logger.LogInformation("ログアウトが完了しました");
                return 0;
            }
            else
            {
                console.NotifyLogoutFailure("ログアウト処理に失敗しました");
                return 1;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ログアウトコマンドの実行中にエラーが発生しました");
            console.ShowError(ex, "ログアウトコマンドの実行中にエラーが発生しました");
            return 1;
        }
    }
}