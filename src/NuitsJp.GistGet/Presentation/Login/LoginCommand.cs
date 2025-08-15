using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Infrastructure.GitHub;

namespace NuitsJp.GistGet.Presentation.Login;

/// <summary>
/// ログインコマンドの実装（Command-Console分離版）
/// </summary>
public class LoginCommand
{
    private readonly IGitHubAuthService _authService;
    private readonly ILoginConsole _console;
    private readonly ILogger<LoginCommand> _logger;

    public LoginCommand(
        IGitHubAuthService authService,
        ILoginConsole console,
        ILogger<LoginCommand> logger)
    {
        _authService = authService;
        _console = console;
        _logger = logger;
    }

    /// <summary>
    /// ログインコマンドを実行
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
                    // デフォルトはログインフロー実行
                    return await ExecuteAuthenticationAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ログインコマンドの実行中にエラーが発生しました");
            _console.ShowError(ex, "ログインコマンドの実行中にエラーが発生しました");
            return 1;
        }
    }

    /// <summary>
    /// ログインフローを実行
    /// </summary>
    private async Task<int> ExecuteAuthenticationAsync()
    {
        try
        {
            using var progress = _console.BeginProgress("GitHubログイン");

            var success = await _authService.AuthenticateAsync();

            if (success)
            {
                _console.NotifyAuthSuccess();
                return 0;
            }

            _console.NotifyAuthFailure("ログインに失敗しました");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ログインフロー実行中にエラーが発生しました");
            _console.NotifyAuthFailure($"ログインエラー: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// ログイン状態を表示
    /// </summary>
    private async Task ShowAuthStatusAsync()
    {
        try
        {
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            string? tokenInfo = null;

            if (isAuthenticated)
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

            _console.ShowAuthStatus(isAuthenticated, tokenInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ログイン状態確認中にエラーが発生しました");
            _console.ShowError(ex, "ログイン状態の確認に失敗しました");
        }
    }
}