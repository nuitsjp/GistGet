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
    public virtual IDisposable BeginProgress(string operation)
    {
        return new SimpleProgressIndicator(operation, this);
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

    /// <summary>
    /// シンプルな進捗表示実装
    /// </summary>
    private class SimpleProgressIndicator : IProgressIndicator
    {
        private readonly ConsoleBase _console;
        private readonly string _operation;
        private bool _disposed;

        public SimpleProgressIndicator(string operation, ConsoleBase console)
        {
            _operation = operation;
            _console = console;
            _console.WriteInfoLine($"{_operation}を開始しています...");
        }

        public void UpdateMessage(string message)
        {
            if (!_disposed) _console.WriteInfoLine($"{_operation}: {message}");
        }

        public void Complete()
        {
            if (!_disposed) _console.WriteInfoLine($"{_operation}が完了しました。");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Complete();
                _disposed = true;
            }
        }
    }
}