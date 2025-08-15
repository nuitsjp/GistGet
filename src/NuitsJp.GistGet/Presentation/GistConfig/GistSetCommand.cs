using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business.Services;
using NuitsJp.GistGet.Business.Models;

namespace NuitsJp.GistGet.Presentation.GistConfig;

/// <summary>
/// Gist設定コマンドの実装（Command-Console分離版）
/// </summary>
public class GistSetCommand
{
    private readonly IGistConfigService _gistConfigService;
    private readonly IGistConfigConsole _console;
    private readonly ILogger<GistSetCommand> _logger;

    public GistSetCommand(
        IGistConfigService gistConfigService,
        IGistConfigConsole console,
        ILogger<GistSetCommand> logger)
    {
        _gistConfigService = gistConfigService ?? throw new ArgumentNullException(nameof(gistConfigService));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync(string? gistId, string? fileName)
    {
        try
        {
            // GitHub でのGist作成手順を表示（引数でGist IDが未指定の場合）
            if (string.IsNullOrWhiteSpace(gistId))
            {
                _console.ShowGistCreationInstructions();
                System.Console.WriteLine(); // 空行
            }

            // Gist ID とファイル名の入力または検証
            var configuration = _console.RequestGistConfiguration(gistId, fileName);
            if (configuration == null)
            {
                return 1;
            }

            // ビジネスロジックはサービス層に委譲
            var request = new GistConfigRequest
            {
                GistId = configuration.Value.gistId,
                FileName = configuration.Value.fileName
            };

            using var progress = _console.BeginProgress("Gist設定保存");

            var result = await _gistConfigService.ConfigureGistAsync(request);

            // 結果の表示
            if (result.IsSuccess)
            {
                _console.NotifyConfigurationSaved(result.GistId!, result.FileName!);
                return 0;
            }
            else
            {
                _console.NotifyConfigurationError(result.ErrorMessage!);
                return 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GistSetCommand");
            _console.ShowError(ex, "予期しないエラーが発生しました");
            return 1;
        }
    }
}
