using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Infrastructure.GitHub;

namespace NuitsJp.GistGet.Presentation.Login;

/// <summary>
/// ログアウトコマンドの実装（Command-Console分離版）
/// </summary>
public class LogoutCommand
{
    private readonly IGitHubAuthService _authService;
    private readonly ILogoutConsole _console;
    private readonly ILogger<LogoutCommand> _logger;

    public LogoutCommand(
        IGitHubAuthService authService,
        ILogoutConsole console,
        ILogger<LogoutCommand> logger)
    {
        _authService = authService;
        _console = console;
        _logger = logger;
    }

    /// <summary>
    /// ログアウトコマンドを実行
    /// </summary>
    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            _logger.LogInformation("ログアウトコマンドを開始します");

            // --silentオプションをチェック
            var isSilent = args.Contains("--silent");

            // サイレントモードでない場合は確認を取る
            if (!isSilent && !_console.ConfirmLogout())
            {
                _logger.LogInformation("ユーザーがログアウトをキャンセルしました");
                return 0; // キャンセルは正常終了扱い
            }

            // ログアウト実行
            var success = await _authService.LogoutAsync();

            if (success)
            {
                _console.NotifyLogoutSuccess();
                _logger.LogInformation("ログアウトが完了しました");
                return 0;
            }
            else
            {
                _console.NotifyLogoutFailure("ログアウト処理に失敗しました");
                return 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ログアウトコマンドの実行中にエラーが発生しました");
            _console.ShowError(ex, "ログアウトコマンドの実行中にエラーが発生しました");
            return 1;
        }
    }
}