using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business.Models;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Infrastructure.Storage;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Business.Services
{
    public class GistConfigService : IGistConfigService
    {
        private readonly IGitHubAuthService _authService;
        private readonly IGistConfigurationStorage _storage;
        private readonly IGistManager _gistManager;
        private readonly ILogger<GistConfigService> _logger;

        public GistConfigService(
            IGitHubAuthService authService,
            IGistConfigurationStorage storage,
            IGistManager gistManager,
            ILogger<GistConfigService> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _gistManager = gistManager ?? throw new ArgumentNullException(nameof(gistManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GistConfigResult> ConfigureGistAsync(GistConfigRequest request)
        {
            try
            {
                // 認証確認
                var isAuthenticated = await _authService.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    return GistConfigResult.Failure("GitHub認証が必要です。まず 'gistget auth' コマンドを実行して認証を完了してください。");
                }

                // 入力値の検証とデフォルト値設定
                var validatedGistId = ValidateAndExtractGistId(request.GistId);
                if (string.IsNullOrEmpty(validatedGistId))
                {
                    return GistConfigResult.Failure("有効なGist IDが必要です。");
                }

                var validatedFileName = string.IsNullOrWhiteSpace(request.FileName) ? "packages.yaml" : request.FileName;

                // Gist存在確認
                await _gistManager.ValidateGistAccessAsync(validatedGistId);

                // 設定の保存
                var config = new GistConfiguration
                {
                    GistId = validatedGistId,
                    FileName = validatedFileName
                };
                await _storage.SaveGistConfigurationAsync(config);

                _logger.LogInformation("Gist configuration saved: {GistId}, {FileName}",
                    validatedGistId, validatedFileName);

                return GistConfigResult.Success(validatedGistId, validatedFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure Gist");
                return GistConfigResult.Failure($"設定エラー: {ex.Message}");
            }
        }

        private static string? ValidateAndExtractGistId(string? gistIdOrUrl)
        {
            if (string.IsNullOrWhiteSpace(gistIdOrUrl))
                return null;

            // URL形式の場合はIDを抽出
            if (gistIdOrUrl.Contains("gist.github.com"))
            {
                var segments = gistIdOrUrl.Split('/');
                return segments.LastOrDefault();
            }

            return gistIdOrUrl;
        }
    }
}