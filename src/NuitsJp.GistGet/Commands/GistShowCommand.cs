using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Services;

namespace NuitsJp.GistGet.Commands;

public class GistShowCommand
{
    private readonly GitHubAuthService _authService;
    private readonly GistInputService _inputService;
    private readonly GistManager _gistManager;
    private readonly PackageYamlConverter _yamlConverter;
    private readonly ILogger<GistShowCommand> _logger;

    public GistShowCommand(
        GitHubAuthService authService,
        GistInputService inputService,
        GistManager gistManager,
        PackageYamlConverter yamlConverter,
        ILogger<GistShowCommand> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _gistManager = gistManager ?? throw new ArgumentNullException(nameof(gistManager));
        _yamlConverter = yamlConverter ?? throw new ArgumentNullException(nameof(yamlConverter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ExecuteAsync(bool raw = false)
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

            // Gist設定確認
            var isConfigured = await _gistManager.IsConfiguredAsync();
            if (!isConfigured)
            {
                Console.WriteLine("Error: Gist設定が見つかりません。");
                Console.WriteLine("'gistget gist set' コマンドを実行してGist設定を行ってください。");
                return 1;
            }

            // 設定情報取得
            var config = await _gistManager.GetConfigurationAsync();

            if (!raw)
            {
                Console.WriteLine($"=== Gist内容表示: {config.GistId} ===");
                Console.WriteLine($"ファイル名: {config.FileName}");
                Console.WriteLine($"Gist URL: {_inputService.FormatGistUrl(config.GistId)}");
                Console.WriteLine();
            }

            try
            {
                // パッケージ取得
                var packages = await _gistManager.GetGistPackagesAsync();

                if (raw)
                {
                    // Raw YAMLとして出力
                    var yamlContent = _yamlConverter.ToYaml(packages);
                    Console.WriteLine(yamlContent);
                }
                else
                {
                    // 整形された表形式で出力
                    if (packages.Count == 0)
                    {
                        Console.WriteLine("登録されているパッケージはありません。");
                    }
                    else
                    {
                        Console.WriteLine($"登録パッケージ数: {packages.Count}");
                        Console.WriteLine();
                        Console.WriteLine("Package ID                          | Version      | Architecture | Scope   | Source  | Custom");
                        Console.WriteLine("----------------------------------- | ------------ | ------------ | ------- | ------- | ---------------");

                        var sortedPackages = packages.ToSortedList();
                        foreach (var package in sortedPackages)
                        {
                            var id = TruncateString(package.Id, 35);
                            var version = TruncateString(package.Version ?? "", 12);
                            var arch = TruncateString(package.Architecture ?? "", 12);
                            var scope = TruncateString(package.Scope ?? "", 7);
                            var source = TruncateString(package.Source ?? "", 7);
                            var custom = TruncateString(package.Custom ?? "", 15);

                            Console.WriteLine($"{id,-35} | {version,-12} | {arch,-12} | {scope,-7} | {source,-7} | {custom,-15}");
                        }
                    }
                }

                _logger.LogInformation("Displayed Gist content for {GistId}, {PackageCount} packages",
                    config.GistId, packages.Count);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Gist内容の取得に失敗: {ex.Message}");
                _logger.LogError(ex, "Failed to retrieve Gist content for {GistId}", config.GistId);
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            _logger.LogError(ex, "Unexpected error while showing Gist content");
            return 1;
        }
    }

    private static string TruncateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        if (input.Length <= maxLength)
            return input;

        return input.Substring(0, maxLength - 3) + "...";
    }
}