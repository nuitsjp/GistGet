using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.GistConfig;

/// <summary>
/// Gist状態確認コマンドの実装（Command-Console分離版）
/// </summary>
public class GistStatusCommand
{
    private readonly IGitHubAuthService _authService;
    private readonly IGistConfigConsole _console;
    private readonly IGistManager _gistManager;
    private readonly ILogger<GistStatusCommand> _logger;

    public GistStatusCommand(
        IGitHubAuthService authService,
        IGistManager gistManager,
        IGistConfigConsole console,
        ILogger<GistStatusCommand> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _gistManager = gistManager ?? throw new ArgumentNullException(nameof(gistManager));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            // 認証状態確認
            var isAuthenticated = await _authService.IsAuthenticatedAsync();

            if (!isAuthenticated)
            {
                _console.ShowGistStatus(false, false);
                return 0; // 認証されていない状態の表示は正常終了
            }

            // Gist設定状態確認
            var isConfigured = await _gistManager.IsConfiguredAsync();

            if (!isConfigured)
            {
                _console.ShowGistStatus(true, false);
                return 0;
            }

            // 設定詳細表示
            try
            {
                var config = await _gistManager.GetConfigurationAsync();

                _console.ShowGistStatus(true, true, config.GistId, config.FileName);

                using var progress = _console.BeginProgress("Gist アクセス確認");

                // Gist存在確認
                await _gistManager.ValidateGistAccessAsync(config.GistId);

                ((IProgressIndicator)progress).UpdateMessage("パッケージ情報を取得中");

                // パッケージ数取得
                try
                {
                    var packages = await _gistManager.GetGistPackagesAsync();
                    System.Console.WriteLine($"登録パッケージ数: {packages.Count}");
                    System.Console.WriteLine($"設定作成日時: {config.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
                    System.Console.WriteLine($"最終アクセス日時: {config.LastAccessedAt:yyyy-MM-dd HH:mm:ss} UTC");
                }
                catch (Exception ex)
                {
                    _console.ShowWarning($"パッケージ情報の取得に失敗: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to retrieve package information");
                }

                _logger.LogInformation("Gist status checked successfully for {GistId}", config.GistId);
                return 0;
            }
            catch (Exception ex)
            {
                _console.ShowError(ex, "設定情報の取得に失敗しました");
                _logger.LogError(ex, "Failed to retrieve Gist configuration details");
                return 1;
            }
        }
        catch (Exception ex)
        {
            _console.ShowError(ex, "Gist状態確認中に予期しないエラーが発生しました");
            _logger.LogError(ex, "Unexpected error while checking Gist status");
            return 1;
        }
    }
}