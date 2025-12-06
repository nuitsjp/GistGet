using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.File;

/// <summary>
/// ファイル操作コマンド固有のコンソール入出力実装
/// download/uploadコマンド用の高レベル操作を提供
/// </summary>
public class FileConsole : ConsoleBase, IFileConsole
{
    /// <summary>
    /// ダウンロード開始を通知
    /// </summary>
    public void NotifyDownloadStarting(string fileName)
    {
        System.Console.WriteLine($"📥 Gistから {fileName} をダウンロードしています...");
    }

    /// <summary>
    /// ダウンロード成功を通知
    /// </summary>
    public void NotifyDownloadSuccess(string fileName, string filePath)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"✅ {fileName} を {filePath} にダウンロードしました");
    }

    /// <summary>
    /// アップロード開始を通知
    /// </summary>
    public void NotifyUploadStarting(string filePath)
    {
        System.Console.WriteLine($"📤 {filePath} をGistにアップロードしています...");
    }

    /// <summary>
    /// アップロード成功を通知
    /// </summary>
    public void NotifyUploadSuccess(string fileName)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"✅ {fileName} をGistにアップロードしました");
    }

    /// <summary>
    /// ファイル上書き確認を取る
    /// </summary>
    public bool ConfirmFileOverwrite(string filePath)
    {
        System.Console.WriteLine();
        System.Console.Write($"{filePath} は既に存在します。上書きしますか？ (y/N): ");
        var input = System.Console.ReadLine()?.Trim().ToLower();
        return input == "y" || input == "yes";
    }

    /// <summary>
    /// 出力ファイルパスを取得
    /// </summary>
    public string GetOutputFilePath(string defaultFileName)
    {
        System.Console.Write($"出力ファイルパス (デフォルト: {defaultFileName}): ");
        var input = System.Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? defaultFileName : input;
    }

    /// <summary>
    /// エラーメッセージを出力
    /// </summary>
    protected override void WriteErrorLine(string message)
    {
        System.Console.WriteLine($"❌ {message}");
    }

    /// <summary>
    /// 警告メッセージを出力
    /// </summary>
    protected override void WriteWarningLine(string message)
    {
        System.Console.WriteLine($"⚠️  {message}");
    }

    /// <summary>
    /// 情報メッセージを出力
    /// </summary>
    protected override void WriteInfoLine(string message)
    {
        System.Console.WriteLine($"ℹ️  {message}");
    }
}