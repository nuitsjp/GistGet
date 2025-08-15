using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Presentation.GistConfig;

/// <summary>
/// Gist内容表示コマンドの実装（Command-Console分離版）
/// </summary>
public class GistShowCommand
{
    private readonly IGitHubAuthService _authService;
    private readonly IGistConfigConsole _console;
    private readonly IGistManager _gistManager;
    private readonly ILogger<GistShowCommand> _logger;
    private readonly IPackageYamlConverter _yamlConverter;

    public GistShowCommand(
        IGitHubAuthService authService,
        IGistManager gistManager,
        IPackageYamlConverter yamlConverter,
        IGistConfigConsole console,
        ILogger<GistShowCommand> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _gistManager = gistManager ?? throw new ArgumentNullException(nameof(gistManager));
        _yamlConverter = yamlConverter ?? throw new ArgumentNullException(nameof(yamlConverter));
        _console = console ?? throw new ArgumentNullException(nameof(console));
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
                _console.ShowError(new InvalidOperationException("GitHub認証が必要です"),
                    "まず 'gistget auth' コマンドを実行して認証を完了してください");
                return 1;
            }

            // Gist設定確認
            var isConfigured = await _gistManager.IsConfiguredAsync();
            if (!isConfigured)
            {
                _console.ShowError(new InvalidOperationException("Gist設定が見つかりません"),
                    "'gistget gist-set' コマンドを実行してGist設定を行ってください");
                return 1;
            }

            // 設定情報取得
            var config = await _gistManager.GetConfigurationAsync();

            try
            {
                using var progress = _console.BeginProgress("Gistコンテンツ取得");

                // パッケージ取得
                var packages = await _gistManager.GetGistPackagesAsync();

                if (raw)
                {
                    // Raw YAMLとして出力
                    var yamlContent = _yamlConverter.ToYaml(packages);
                    _console.ShowGistContent(config.GistId, config.FileName, yamlContent);
                }
                else
                {
                    // 整形された表形式で出力
                    var formattedContent = FormatPackagesAsTable(packages);
                    _console.ShowGistContent(config.GistId, config.FileName, formattedContent);
                }

                _logger.LogInformation("Displayed Gist content for {GistId}, {PackageCount} packages",
                    config.GistId, packages.Count);

                return 0;
            }
            catch (Exception ex)
            {
                _console.ShowError(ex, "Gist内容の取得に失敗しました");
                _logger.LogError(ex, "Failed to retrieve Gist content for {GistId}", config.GistId);
                return 1;
            }
        }
        catch (Exception ex)
        {
            _console.ShowError(ex, "Gist内容表示中に予期しないエラーが発生しました");
            _logger.LogError(ex, "Unexpected error while showing Gist content");
            return 1;
        }
    }

    /// <summary>
    /// パッケージを表形式でフォーマット
    /// </summary>
    private static string FormatPackagesAsTable(PackageCollection packages)
    {
        var result = new List<string>();

        if (packages.Count == 0)
        {
            result.Add("登録されているパッケージはありません。");
        }
        else
        {
            result.Add($"登録パッケージ数: {packages.Count}");
            result.Add("");
            result.Add(
                "Package ID                          | Version      | Architecture | Scope   | Source  | Custom");
            result.Add(
                "----------------------------------- | ------------ | ------------ | ------- | ------- | ---------------");

            var sortedPackages = packages.ToSortedList();
            foreach (var package in sortedPackages)
            {
                var id = TruncateString(package.Id, 35);
                var version = TruncateString(package.Version ?? "", 12);
                var arch = TruncateString(package.Architecture ?? "", 12);
                var scope = TruncateString(package.Scope ?? "", 7);
                var source = TruncateString(package.Source ?? "", 7);
                var custom = TruncateString(package.Custom ?? "", 15);

                result.Add($"{id,-35} | {version,-12} | {arch,-12} | {scope,-7} | {source,-7} | {custom,-15}");
            }
        }

        return string.Join(Environment.NewLine, result);
    }

    /// <summary>
    /// 文字列を指定の長さで切り詰め
    /// </summary>
    private static string TruncateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        if (input.Length <= maxLength)
            return input;

        return input.Substring(0, maxLength - 3) + "...";
    }
}