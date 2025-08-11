using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Abstractions;

namespace NuitsJp.GistGet.Services;

/// <summary>
/// 認証コマンドの実装
/// </summary>
public class AuthCommand
{
    private readonly IGitHubAuthService _authService;
    private readonly ILogger<AuthCommand> _logger;

    public AuthCommand(IGitHubAuthService authService, ILogger<AuthCommand> logger)
    {
        _authService = authService;
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
                    await _authService.ShowAuthStatusAsync();
                    return 0;
                    
                case "":
                default:
                    // デフォルトは認証フロー実行
                    var success = await _authService.AuthenticateAsync();
                    return success ? 0 : 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "認証コマンドの実行中にエラーが発生しました");
            Console.WriteLine($"エラー: {ex.Message}");
            return 1;
        }
    }
}
