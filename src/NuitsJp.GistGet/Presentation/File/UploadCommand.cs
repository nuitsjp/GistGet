using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Infrastructure.GitHub;

namespace NuitsJp.GistGet.Presentation.File;

/// <summary>
/// uploadコマンドの実装
/// ローカルファイルをGistにアップロードする
/// </summary>
public class UploadCommand(
    IGistManager gistManager,
    IGitHubGistClient gistClient,
    IFileConsole console,
    ILogger<UploadCommand> logger)
{

    /// <summary>
    /// uploadコマンドを実行
    /// </summary>
    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            logger.LogInformation("uploadコマンドを開始します");

            // オプション解析
            var options = ParseOptions(args);

            // ファイルパスが指定されていない場合はエラー
            if (string.IsNullOrEmpty(options.FilePath))
            {
                console.ShowError(new ArgumentException("ファイルパスが指定されていません"),
                    "アップロードするファイルパスを指定してください。例: gistget upload packages.yaml");
                return 1;
            }

            // ファイル存在確認
            if (!System.IO.File.Exists(options.FilePath))
            {
                console.ShowError(new FileNotFoundException($"ファイルが見つかりません: {options.FilePath}"),
                    $"指定されたファイルが見つかりません: {options.FilePath}");
                return 1;
            }

            // Gist設定確認
            if (!await gistManager.IsConfiguredAsync())
            {
                console.ShowError(new InvalidOperationException("Gist設定がありません"),
                    "Gist設定がありません。先に 'gistget gist set' を実行してください。");
                return 1;
            }

            var config = await gistManager.GetConfigurationAsync();
            console.NotifyUploadStarting(options.FilePath);

            // ファイル内容を読み込み
            var content = await System.IO.File.ReadAllTextAsync(options.FilePath);

            // Gistに更新
            await gistClient.UpdateFileContentAsync(config.GistId, config.FileName, content);

            console.NotifyUploadSuccess(config.FileName);
            logger.LogInformation("アップロードが完了しました: {FilePath} -> {GistId}/{FileName}",
                options.FilePath, config.GistId, config.FileName);

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "uploadコマンドの実行中にエラーが発生しました");
            console.ShowError(ex, "アップロード処理でエラーが発生しました");
            return 1;
        }
    }

    private UploadOptions ParseOptions(string[] args)
    {
        var options = new UploadOptions();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            // オプションフラグをスキップして、最初の引数をファイルパスとして扱う
            if (arg.StartsWith("--") || arg.StartsWith("-"))
            {
                // 将来的なオプション拡張用
                continue;
            }

            // コマンド名以外の最初の引数をファイルパスとする
            if (arg != "upload" && string.IsNullOrEmpty(options.FilePath))
            {
                options.FilePath = arg;
            }
        }

        return options;
    }

    private class UploadOptions
    {
        public string? FilePath { get; set; }
    }
}