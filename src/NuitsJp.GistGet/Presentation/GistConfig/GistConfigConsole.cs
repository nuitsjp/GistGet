using NuitsJp.GistGet.Presentation.Console;
using Sharprompt;

namespace NuitsJp.GistGet.Presentation.GistConfig;

/// <summary>
/// Gist設定コマンド固有のコンソール入出力実装
/// Gist設定管理用の高レベル操作を提供
/// </summary>
public class GistConfigConsole : ConsoleBase, IGistConfigConsole
{
    /// <summary>
    /// Gist作成手順を表示
    /// </summary>
    public void ShowGistCreationInstructions()
    {
        System.Console.WriteLine("GitHub Gist の作成手順:");
        System.Console.WriteLine("1. https://gist.github.com にアクセス");
        System.Console.WriteLine("2. 新しいGistを作成");
        System.Console.WriteLine("3. ファイル名（例: packages.yaml）を入力");
        System.Console.WriteLine("4. 初期内容として空のYAMLを入力: packages: []");
        System.Console.WriteLine("5. 'Create public gist' または 'Create secret gist' をクリック");
        System.Console.WriteLine("6. 作成されたGistのURLまたはIDを以下に入力してください");
    }

    /// <summary>
    /// Gist設定（IDとファイル名）を取得
    /// </summary>
    public (string gistId, string fileName)? RequestGistConfiguration(string? providedGistId, string? providedFileName)
    {
        // Gist IDの取得
        var gistId = CollectGistId(providedGistId);
        if (gistId == null) return null;

        // ファイル名の取得
        var fileName = CollectFileName(providedFileName);

        return (gistId, fileName);
    }

    /// <summary>
    /// Gist設定保存成功を通知
    /// </summary>
    public void NotifyConfigurationSaved(string gistId, string fileName)
    {
        System.Console.WriteLine("✅ Gist設定を保存しました。");
        System.Console.WriteLine($"  Gist ID: {gistId}");
        System.Console.WriteLine($"  ファイル名: {fileName}");
        System.Console.WriteLine($"  Gist URL: https://gist.github.com/{gistId}");
    }

    /// <summary>
    /// 現在のGist設定を表示
    /// </summary>
    public void ShowCurrentConfiguration(string gistId, string fileName)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("=== 現在のGist設定 ===");
        System.Console.WriteLine($"Gist ID: {gistId}");
        System.Console.WriteLine($"ファイル名: {fileName}");
        System.Console.WriteLine($"Gist URL: https://gist.github.com/{gistId}");
    }

    /// <summary>
    /// Gist設定状態を表示
    /// </summary>
    public void ShowGistStatus(bool isAuthenticated, bool isConfigured, string? gistId = null, string? fileName = null)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("=== Gist設定状態 ===");
        System.Console.WriteLine();

        // 認証状態確認
        System.Console.WriteLine($"GitHub認証状態: {(isAuthenticated ? "✅ 認証済み" : "❌ 未認証")}");

        if (!isAuthenticated)
        {
            System.Console.WriteLine("警告: 認証が必要です。'gistget auth' コマンドを実行してください。");
            System.Console.WriteLine();
            return;
        }

        // Gist設定状態確認
        System.Console.WriteLine($"Gist設定状態: {(isConfigured ? "✅ 設定済み" : "❌ 未設定")}");

        if (isConfigured && !string.IsNullOrEmpty(gistId) && !string.IsNullOrEmpty(fileName))
        {
            System.Console.WriteLine($"  Gist ID: {gistId}");
            System.Console.WriteLine($"  ファイル名: {fileName}");
            System.Console.WriteLine($"  Gist URL: https://gist.github.com/{gistId}");
        }
        else if (!isConfigured)
        {
            System.Console.WriteLine("Gist設定が必要です。'gistget gist-set' コマンドで設定してください。");
        }

        System.Console.WriteLine();
    }

    /// <summary>
    /// Gistコンテンツを表示
    /// </summary>
    public void ShowGistContent(string gistId, string fileName, string content)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"=== Gist コンテンツ: {gistId}/{fileName} ===");
        System.Console.WriteLine(content);
    }

    /// <summary>
    /// Gist設定の設定ミスを通知
    /// </summary>
    public void NotifyConfigurationError(string reason)
    {
        System.Console.WriteLine($"❌ Gist設定エラー: {reason}");
    }

    /// <summary>
    /// エラーメッセージを出力
    /// </summary>
    protected override void WriteErrorLine(string message)
    {
        System.Console.WriteLine(message);
    }

    /// <summary>
    /// 警告メッセージを出力
    /// </summary>
    protected override void WriteWarningLine(string message)
    {
        System.Console.WriteLine(message);
    }

    /// <summary>
    /// 情報メッセージを出力
    /// </summary>
    protected override void WriteInfoLine(string message)
    {
        System.Console.WriteLine(message);
    }

    /// <summary>
    /// Gist IDを収集（内部ヘルパー）
    /// </summary>
    private string? CollectGistId(string? providedGistId)
    {
        if (!string.IsNullOrWhiteSpace(providedGistId)) return ExtractGistIdFromUrl(providedGistId);

        try
        {
            var userInput = Prompt.Input<string>(
                "Gist IDまたはURLを入力してください",
                validators: [Validators.Required()]
            );

            return ExtractGistIdFromUrl(userInput);
        }
        catch (Exception)
        {
            System.Console.WriteLine("❌ Gist IDの入力に失敗しました。");
            return null;
        }
    }

    /// <summary>
    /// ファイル名を収集（内部ヘルパー）
    /// </summary>
    private static string CollectFileName(string? providedFileName)
    {
        if (!string.IsNullOrWhiteSpace(providedFileName)) return providedFileName;

        const string defaultFileName = "packages.yaml";

        try
        {
            var userInput = Prompt.Input<string>(
                "ファイル名を入力してください",
                defaultFileName
            );

            return string.IsNullOrWhiteSpace(userInput) ? defaultFileName : userInput;
        }
        catch (Exception)
        {
            System.Console.WriteLine($"ファイル名の入力に失敗しました。デフォルト値 '{defaultFileName}' を使用します。");
            return defaultFileName;
        }
    }

    /// <summary>
    /// GistのURLからIDを抽出（内部ヘルパー）
    /// </summary>
    private static string ExtractGistIdFromUrl(string gistIdOrUrl)
    {
        if (gistIdOrUrl.Contains("gist.github.com"))
        {
            var segments = gistIdOrUrl.Split('/');
            return segments.LastOrDefault() ?? gistIdOrUrl;
        }

        return gistIdOrUrl;
    }

    /// <summary>
    /// Gist設定クリアの確認を取る
    /// </summary>
    public bool ConfirmClearConfiguration()
    {
        System.Console.WriteLine();
        System.Console.Write("現在のGist設定をクリアしますか？ (y/N): ");
        var input = System.Console.ReadLine()?.Trim().ToLower();
        return input == "y" || input == "yes";
    }

    /// <summary>
    /// Gist設定クリア成功を通知
    /// </summary>
    public void NotifyConfigurationCleared()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("✅ Gist設定をクリアしました");
    }

    /// <summary>
    /// Gist設定が既にクリア済みであることを通知
    /// </summary>
    public void NotifyNotConfigured()
    {
        System.Console.WriteLine("Gist設定は既にクリア済みです");
    }

    /// <summary>
    /// 操作キャンセルを通知
    /// </summary>
    public void NotifyOperationCanceled()
    {
        System.Console.WriteLine("操作をキャンセルしました");
    }
}