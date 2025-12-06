using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Business;

namespace NuitsJp.GistGet.Presentation.File;

/// <summary>
/// downloadコマンドの実装
/// Gist定義ファイルをローカルにダウンロードする
/// </summary>
public class DownloadCommand(
    IGistManager gistManager,
    IFileConsole console,
    ILogger<DownloadCommand> logger)
{

    /// <summary>
    /// downloadコマンドを実行
    /// </summary>
    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            logger.LogInformation("downloadコマンドを開始します");

            // オプション解析
            var options = ParseOptions(args);

            // Gist設定確認
            if (!await gistManager.IsConfiguredAsync())
            {
                console.ShowError(new InvalidOperationException("Gist設定がありません"),
                    "Gist設定がありません。先に 'gistget gist set' を実行してください。");
                return 1;
            }

            var config = await gistManager.GetConfigurationAsync();
            var fileName = config.FileName;

            console.NotifyDownloadStarting(fileName);

            // Gistからコンテンツを取得
            var content = await gistManager.GetGistContentAsync();

            // 出力ファイルパスを決定
            var outputPath = options.OutputPath ?? (options.Interactive ?
                console.GetOutputFilePath(fileName) : fileName);

            // ファイル上書き確認
            if (System.IO.File.Exists(outputPath) && !options.Force)
            {
                if (!options.Interactive || !console.ConfirmFileOverwrite(outputPath))
                {
                    logger.LogInformation("ユーザーがファイル上書きをキャンセルしました");
                    return 0; // キャンセルは正常終了扱い
                }
            }

            // ファイルに保存
            await System.IO.File.WriteAllTextAsync(outputPath, content);

            console.NotifyDownloadSuccess(fileName, outputPath);
            logger.LogInformation("ダウンロードが完了しました: {OutputPath}", outputPath);

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "downloadコマンドの実行中にエラーが発生しました");
            console.ShowError(ex, "ダウンロード処理でエラーが発生しました");
            return 1;
        }
    }

    private DownloadOptions ParseOptions(string[] args)
    {
        var options = new DownloadOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OutputPath = args[i + 1];
                        i++; // 次の引数をスキップ
                    }
                    break;
                case "--force":
                case "-f":
                    options.Force = true;
                    break;
                case "--interactive":
                case "-i":
                    options.Interactive = true;
                    break;
            }
        }

        return options;
    }

    private class DownloadOptions
    {
        public string? OutputPath { get; set; }
        public bool Force { get; set; }
        public bool Interactive { get; set; }
    }
}