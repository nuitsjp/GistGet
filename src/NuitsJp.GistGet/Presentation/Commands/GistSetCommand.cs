using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Infrastructure.Storage;
using NuitsJp.GistGet.Business;

namespace NuitsJp.GistGet.Presentation.Commands;

public class GistSetCommand
{
    private readonly GitHubAuthService _authService;
    private readonly GistInputService _inputService;
    private readonly IGistConfigurationStorage _storage;
    private readonly GistManager _gistManager;
    private readonly ILogger<GistSetCommand> _logger;

    public GistSetCommand(
        GitHubAuthService authService,
        GistInputService inputService,
        IGistConfigurationStorage storage,
        GistManager gistManager,
        ILogger<GistSetCommand> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _gistManager = gistManager ?? throw new ArgumentNullException(nameof(gistManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync(string? gistId, string? fileName)
    {
        try
        {
            // 認証確認
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            if (!isAuthenticated)
            {
                Console.WriteLine("Error: GitHub認証が必要です。");
                Console.WriteLine("まず 'gistget auth' コマンドを実行して認証を完了してください。");
                return 1;
            }

            // GitHubでのGist作成手順を表示
            if (string.IsNullOrWhiteSpace(gistId))
            {
                Console.WriteLine(_inputService.GetGistCreationInstructions());
                Console.WriteLine();
            }

            // Gist IDの入力または検証
            var validatedGistId = await GetValidatedGistIdAsync(gistId);
            if (validatedGistId == null)
            {
                return 1;
            }

            // ファイル名の入力または検証
            var validatedFileName = GetValidatedFileName(fileName);

            // Gist存在確認
            Console.WriteLine($"Gist {validatedGistId} の存在を確認中...");
            await _gistManager.ValidateGistAccessAsync(validatedGistId);

            // 設定保存
            var config = _inputService.CreateConfiguration(validatedGistId, validatedFileName);
            await _storage.SaveGistConfigurationAsync(config);

            Console.WriteLine("Gist設定を保存しました。");
            Console.WriteLine($"  Gist ID: {validatedGistId}");
            Console.WriteLine($"  ファイル名: {validatedFileName}");
            Console.WriteLine($"  Gist URL: {_inputService.FormatGistUrl(validatedGistId)}");

            _logger.LogInformation("Gist configuration saved: {GistId}, {FileName}",
                validatedGistId, validatedFileName);

            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            _logger.LogError(ex, "Failed to set Gist configuration");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            _logger.LogError(ex, "Unexpected error while setting Gist configuration");
            return 1;
        }
    }

    private async Task<string?> GetValidatedGistIdAsync(string? providedGistId)
    {
        if (!string.IsNullOrWhiteSpace(providedGistId))
        {
            try
            {
                // URL形式からIDを抽出する場合
                var extractedId = _inputService.ExtractGistIdFromUrl(providedGistId);
                _inputService.ValidateGistId(extractedId);
                return extractedId;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: 無効なGist ID: {ex.Message}");
                return null;
            }
        }

        // 対話的にGist IDを入力
        Console.Write("Gist IDまたはURLを入力してください: ");
        var userInput = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userInput))
        {
            Console.WriteLine("Error: Gist IDが入力されませんでした。");
            return null;
        }

        try
        {
            var extractedId = _inputService.ExtractGistIdFromUrl(userInput);
            _inputService.ValidateGistId(extractedId);
            return extractedId;
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Error: 無効なGist ID: {ex.Message}");
            return null;
        }
    }

    private string GetValidatedFileName(string? providedFileName)
    {
        if (!string.IsNullOrWhiteSpace(providedFileName))
        {
            try
            {
                _inputService.ValidateFileName(providedFileName);
                return providedFileName;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Warning: 無効なファイル名: {ex.Message}");
                Console.WriteLine($"デフォルトのファイル名 '{_inputService.GetDefaultFileName()}' を使用します。");
                return _inputService.GetDefaultFileName();
            }
        }

        // 対話的にファイル名を入力（任意）
        var defaultFileName = _inputService.GetDefaultFileName();
        Console.Write($"ファイル名を入力してください（デフォルト: {defaultFileName}）: ");
        var userInput = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userInput))
        {
            return defaultFileName;
        }

        try
        {
            _inputService.ValidateFileName(userInput);
            return userInput;
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Warning: 無効なファイル名: {ex.Message}");
            Console.WriteLine($"デフォルトのファイル名 '{defaultFileName}' を使用します。");
            return defaultFileName;
        }
    }
}