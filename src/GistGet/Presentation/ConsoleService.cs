// Console I/O implementation for the CLI layer.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using GistGet.Infrastructure;

namespace GistGet.Presentation;

/// <summary>
/// Writes messages to the console and provides clipboard support.
/// </summary>
[ExcludeFromCodeCoverage]
public class ConsoleService : IConsoleService
{
    private readonly IProcessRunner _processRunner;

    public ConsoleService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    /// <summary>
    /// Writes an informational message.
    /// </summary>
    public void WriteInfo(string message) =>
        Console.WriteLine(message ?? throw new ArgumentNullException(nameof(message)));

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    public void WriteWarning(string message) =>
        Console.WriteLine($"! {message ?? throw new ArgumentNullException(nameof(message))}");

    /// <summary>
    /// Reads a single line from standard input.
    /// </summary>
    public string? ReadLine() => Console.ReadLine();

    [SupportedOSPlatform("windows")]
    public void SetClipboard(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        SetClipboardWindows(text);
    }

    [SupportedOSPlatform("windows")]
    private void SetClipboardWindows(string text)
    {
        var escaped = text.Replace("'", "''");
        var arguments = $"-Command \"Set-Clipboard -Value '{escaped}'\"";
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _processRunner.RunAsync(startInfo).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Starts a spinner progress display.
    /// </summary>
    public IDisposable WriteProgress(string message)
    {
        return new SpinnerProgress(message);
    }

    /// <summary>
    /// Writes a step progress message (simple one-line output).
    /// </summary>
    public void WriteStep(int current, int total, string message) =>
        Console.WriteLine($"[{current}/{total}] {message}");

    /// <summary>
    /// Writes a success message.
    /// </summary>
    public void WriteSuccess(string message) =>
        Console.WriteLine($"✓ {message}");

    /// <summary>
    /// Writes an error message.
    /// </summary>
    public void WriteError(string message) =>
        Console.Error.WriteLine($"✗ {message}");

    [ExcludeFromCodeCoverage]
    private sealed class SpinnerProgress : IDisposable
    {
        private static readonly string[] s_spinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _spinnerTask;
        private readonly string _message;
        private int _frameIndex;

        public SpinnerProgress(string message)
        {
            _message = message;
            Console.CursorVisible = false;
            _spinnerTask = RunSpinnerAsync(_cts.Token);
        }

        private async Task RunSpinnerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Console.Write($"\r{s_spinnerFrames[_frameIndex]} {_message}");
                _frameIndex = (_frameIndex + 1) % s_spinnerFrames.Length;
                try
                {
                    await Task.Delay(100, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _spinnerTask.Wait(); } catch (AggregateException) { /* Expected on cancellation */ }
            // Clear the entire line using console buffer width
            var clearLength = Math.Max(Console.BufferWidth - 1, _message.Length + 2);
            Console.Write($"\r{new string(' ', clearLength)}\r");
            Console.CursorVisible = true;
            _cts.Dispose();
        }
    }
}
