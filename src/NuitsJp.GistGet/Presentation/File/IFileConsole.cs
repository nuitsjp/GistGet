using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.File;

/// <summary>
/// ファイル操作コンソール表示のインターフェース
/// download/uploadコマンド用の高レベル抽象化を提供
/// </summary>
public interface IFileConsole : IConsoleBase
{
    /// <summary>
    /// ダウンロード開始を通知
    /// </summary>
    void NotifyDownloadStarting(string fileName);

    /// <summary>
    /// ダウンロード成功を通知
    /// </summary>
    void NotifyDownloadSuccess(string fileName, string filePath);

    /// <summary>
    /// アップロード開始を通知
    /// </summary>
    void NotifyUploadStarting(string filePath);

    /// <summary>
    /// アップロード成功を通知
    /// </summary>
    void NotifyUploadSuccess(string fileName);

    /// <summary>
    /// ファイル上書き確認を取る
    /// </summary>
    bool ConfirmFileOverwrite(string filePath);

    /// <summary>
    /// 出力ファイルパスを取得
    /// </summary>
    string GetOutputFilePath(string defaultFileName);
}