using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Infrastructure.GitHub;

namespace NuitsJp.GistGet.Presentation.Auth;

/// <summary>
/// 認証コマンドの実装（Command-Console分離版）
/// </summary>
public class AuthCommand
{
    private readonly IGitHubAuthService _authService;
    private readonly IAuthConsole _console;
    private readonly ILogger<AuthCommand> _logger;

    public AuthCommand(
        IGitHubAuthService authService,
        IAuthConsole console,
        ILogger<AuthCommand> logger)
    {
        _authService = authService;
        _console = console;
        _logger = logger;
    }

    /// <summary>
    /// 認証コマンドを実行
    /// </summary>
    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            // サブコマンドの判定
            var subCommand = args.Length > 1 ? args[1].ToLower() : "";

            switch (subCommand)
            {
                case "status":
                    await ShowAuthStatusAsync();
                    return 0;

                case "":
                default:
                    // デフォルトは認証フロー実行
                    return await ExecuteAuthenticationAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "認証コマンドの実行中にエラーが発生しました");
            _console.ShowError(ex, "認証コマンドの実行中にエラーが発生しました");
            return 1;
        }
    }

    /// <summary>
    /// 認証フローを実行
    /// </summary>
    private async Task<int> ExecuteAuthenticationAsync()
    {
        try
        {
            using var progress = _console.BeginProgress("GitHub認証");

            // 現在のGitHubAuthServiceがDevice Flow処理と表示を内包しているため、
            // 将来的にはGitHubAuthServiceも分離が必要
            // TODO: GitHubAuthServiceからDevice Code取得とトークン取得を分離し、
            //       表示ロジックはIAuthConsoleに移動
            var success = await _authService.AuthenticateAsync();

            if (success)
            {
                _console.NotifyAuthSuccess();
                return 0;
            }
            else
            {
                _console.NotifyAuthFailure("認証に失敗しました");
                return 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "認証フロー実行中にエラーが発生しました");
            _console.NotifyAuthFailure($"認証エラー: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// 認証状態を表示
    /// </summary>
    private async Task ShowAuthStatusAsync()
    {
        try
        {
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            string? tokenInfo = null;

            if (isAuthenticated)
            {
                try
                {
                    var client = await _authService.GetAuthenticatedClientAsync();
                    if (client != null)
                    {
                        var user = await client.User.Current();
                        tokenInfo = $"{user.Login} ({user.Name})";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ユーザー情報の取得に失敗しました");
                    tokenInfo = "ユーザー情報取得失敗";
                }
            }

            _console.ShowAuthStatus(isAuthenticated, tokenInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "認証状態確認中にエラーが発生しました");
            _console.ShowError(ex, "認証状態の確認に失敗しました");
        }
    }
}
