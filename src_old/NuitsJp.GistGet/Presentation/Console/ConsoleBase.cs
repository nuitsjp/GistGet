using Microsoft.Extensions.Logging;

namespace NuitsJp.GistGet.Presentation.Console;

/// <summary>
/// コンソール入出力サービスの共通基盤実装
/// </summary>
public abstract class ConsoleBase(ILogger? logger = null) : IConsoleBase
{

    /// <summary>
    /// エラーを表示（詳細度は実装に委ねる）
    /// </summary>
    public virtual void ShowError(Exception exception, string? userFriendlyMessage = null)
    {
        logger?.LogError(exception, "Error occurred: {Message}", exception.Message);

        var displayMessage = userFriendlyMessage ?? exception.Message;
        WriteErrorLine($"エラー: {displayMessage}");
    }

    /// <summary>
    /// 警告を表示
    /// </summary>
    public virtual void ShowWarning(string message)
    {
        WriteWarningLine($"警告: {message}");
    }

    /// <summary>
    /// 進捗状況を表示（長時間処理用）
    /// </summary>
    public virtual void BeginProgress(string operation)
    {
        WriteInfoLine($"{operation}を開始しています...");
    }

    /// <summary>
    /// エラーメッセージを出力（派生クラスで実装）
    /// </summary>
    protected abstract void WriteErrorLine(string message);

    /// <summary>
    /// 警告メッセージを出力（派生クラスで実装）
    /// </summary>
    protected abstract void WriteWarningLine(string message);

    /// <summary>
    /// 情報メッセージを出力（派生クラスで実装）
    /// </summary>
    protected abstract void WriteInfoLine(string message);
}