using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business.Services;
using NuitsJp.GistGet.Business.Models;
using Sharprompt;

namespace NuitsJp.GistGet.Presentation.Commands
{
    public class GistSetCommand
    {
        private readonly IGistConfigService _gistConfigService;
        private readonly ILogger<GistSetCommand> _logger;

        public GistSetCommand(
            IGistConfigService gistConfigService,
            ILogger<GistSetCommand> logger)
        {
            _gistConfigService = gistConfigService ?? throw new ArgumentNullException(nameof(gistConfigService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> ExecuteAsync(string? gistId, string? fileName)
        {
            try
            {
                // GitHub でのGist作成手順を表示（UI制御）
                if (string.IsNullOrWhiteSpace(gistId))
                {
                    DisplayGistCreationInstructions();
                    Console.WriteLine();
                }

                // Gist ID の入力または検証（UI制御）
                var validatedGistId = CollectGistId(gistId);
                if (validatedGistId == null)
                {
                    return 1;
                }

                // ファイル名の入力または検証（UI制御）
                var validatedFileName = CollectFileName(fileName);

                // ビジネスロジックはサービス層に委譲
                var request = new GistConfigRequest
                {
                    GistId = validatedGistId,
                    FileName = validatedFileName
                };

                var result = await _gistConfigService.ConfigureGistAsync(request);

                // 結果の表示（UI制御）
                if (result.IsSuccess)
                {
                    DisplaySuccessMessage(result.GistId!, result.FileName!);
                    return 0;
                }
                else
                {
                    DisplayErrorMessage(result.ErrorMessage!);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GistSetCommand");
                DisplayErrorMessage($"予期しないエラーが発生しました: {ex.Message}");
                return 1;
            }
        }

        private static void DisplayGistCreationInstructions()
        {
            Console.WriteLine("GitHub Gist の作成手順:");
            Console.WriteLine("1. https://gist.github.com にアクセス");
            Console.WriteLine("2. 新しいGistを作成");
            Console.WriteLine("3. ファイル名（例: packages.yaml）を入力");
            Console.WriteLine("4. 初期内容として空のYAMLを入力: packages: []");
            Console.WriteLine("5. 'Create public gist' または 'Create secret gist' をクリック");
            Console.WriteLine("6. 作成されたGistのURLまたはIDを以下に入力してください");
        }

        private static string? CollectGistId(string? providedGistId)
        {
            if (!string.IsNullOrWhiteSpace(providedGistId))
            {
                return ExtractGistIdFromUrl(providedGistId);
            }

            try
            {
                var userInput = Prompt.Input<string>(
                    "Gist IDまたはURLを入力してください",
                    validators: new[] { Validators.Required() }
                );

                return ExtractGistIdFromUrl(userInput);
            }
            catch (Exception)
            {
                Console.WriteLine("Error: Gist IDの入力に失敗しました。");
                return null;
            }
        }

        private static string CollectFileName(string? providedFileName)
        {
            if (!string.IsNullOrWhiteSpace(providedFileName))
            {
                return providedFileName;
            }

            const string defaultFileName = "packages.yaml";

            try
            {
                var userInput = Prompt.Input<string>(
                    "ファイル名を入力してください",
                    defaultValue: defaultFileName
                );

                return string.IsNullOrWhiteSpace(userInput) ? defaultFileName : userInput;
            }
            catch (Exception)
            {
                Console.WriteLine($"ファイル名の入力に失敗しました。デフォルト値 '{defaultFileName}' を使用します。");
                return defaultFileName;
            }
        }

        private static string ExtractGistIdFromUrl(string gistIdOrUrl)
        {
            if (gistIdOrUrl.Contains("gist.github.com"))
            {
                var segments = gistIdOrUrl.Split('/');
                return segments.LastOrDefault() ?? gistIdOrUrl;
            }

            return gistIdOrUrl;
        }

        private static void DisplaySuccessMessage(string gistId, string fileName)
        {
            Console.WriteLine("Gist設定を保存しました。");
            Console.WriteLine($"  Gist ID: {gistId}");
            Console.WriteLine($"  ファイル名: {fileName}");
            Console.WriteLine($"  Gist URL: https://gist.github.com/{gistId}");
        }

        private static void DisplayErrorMessage(string errorMessage)
        {
            Console.WriteLine($"Error: {errorMessage}");
        }
    }
}