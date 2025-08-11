using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Services;

namespace NuitsJp.GistGet.Commands;

public class GistStatusCommand
{
    private readonly GitHubAuthService _authService;
    private readonly GistInputService _inputService;
    private readonly GistManager _gistManager;
    private readonly ILogger<GistStatusCommand> _logger;

    public GistStatusCommand(
        GitHubAuthService authService,
        GistInputService inputService,
        GistManager gistManager,
        ILogger<GistStatusCommand> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _gistManager = gistManager ?? throw new ArgumentNullException(nameof(gistManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            Console.WriteLine("=== Gist設定状態 ===");
            Console.WriteLine();

            // 認証状態確認
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            Console.WriteLine($"GitHub認証状態: {(isAuthenticated ? "✓ 認証済み" : "✗ 未認証")}");

            if (!isAuthenticated)
            {
                Console.WriteLine("Warning: 認証が必要です。'gistget auth' コマンドを実行してください。");
                Console.WriteLine();
                return 1;
            }

            // Gist設定状態確認
            var isConfigured = await _gistManager.IsConfiguredAsync();
            Console.WriteLine($"Gist設定状態: {(isConfigured ? "✓ 設定済み" : "✗ 未設定")}");

            if (!isConfigured)
            {
                Console.WriteLine("Gist設定が見つかりません。'gistget gist set' コマンドを実行してください。");
                Console.WriteLine();
                return 0;
            }

            // 設定詳細表示
            try
            {
                var config = await _gistManager.GetConfigurationAsync();
                Console.WriteLine();
                Console.WriteLine("=== 設定詳細 ===");
                Console.WriteLine($"Gist ID: {config.GistId}");
                Console.WriteLine($"ファイル名: {config.FileName}");
                Console.WriteLine($"Gist URL: {_inputService.FormatGistUrl(config.GistId)}");
                Console.WriteLine($"設定作成日時: {config.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"最終アクセス日時: {config.LastAccessedAt:yyyy-MM-dd HH:mm:ss} UTC");

                // Gist存在確認
                Console.WriteLine();
                Console.WriteLine("Gist アクセス確認中...");
                await _gistManager.ValidateGistAccessAsync(config.GistId);
                Console.WriteLine("✓ Gistにアクセス可能です");

                // パッケージ数取得
                try
                {
                    var packages = await _gistManager.GetGistPackagesAsync();
                    Console.WriteLine($"登録パッケージ数: {packages.Count}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: パッケージ情報の取得に失敗: {ex.Message}");
                }

                _logger.LogInformation("Gist status checked successfully for {GistId}", config.GistId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: 設定情報の取得に失敗: {ex.Message}");
                _logger.LogError(ex, "Failed to retrieve Gist configuration details");
                return 1;
            }

            Console.WriteLine();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            _logger.LogError(ex, "Unexpected error while checking Gist status");
            return 1;
        }
    }
}