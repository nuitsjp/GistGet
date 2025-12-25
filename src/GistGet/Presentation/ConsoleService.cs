// Console I/O implementation for the CLI layer.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using GistGet.Infrastructure;
using Sharprompt;

namespace GistGet.Presentation;

/// <summary>
/// Writes messages to the console and provides clipboard support.
/// </summary>
[ExcludeFromCodeCoverage]
public class ConsoleService : IConsoleService
{
    private readonly IProcessRunner _processRunner;
    private readonly IConsoleProxy _console;

    public ConsoleService(IProcessRunner processRunner, IConsoleProxy console)
    {
        _processRunner = processRunner;
        _console = console;
    }

    /// <summary>
    /// Writes an informational message.
    /// </summary>
    public void WriteInfo(string message) =>
        _console.WriteLine(message ?? throw new ArgumentNullException(nameof(message)));

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    public void WriteWarning(string message) =>
        _console.WriteLine($"! {message ?? throw new ArgumentNullException(nameof(message))}");

    /// <summary>
    /// Reads a single line from standard input.
    /// </summary>
    public string? ReadLine() => _console.ReadLine();

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
    /// Falls back to one-shot output when spinner is not available.
    /// </summary>
    public IDisposable WriteProgress(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_console.IsOutputRedirected || _console.IsErrorRedirected)
        {
            _console.WriteLine($"... {message}");
            return new SimpleProgress();
        }

        try
        {
            return new SpinnerProgress(_console, message);
        }
        catch (IOException)
        {
            _console.WriteLine($"... {message}");
            return new SimpleProgress();
        }
        catch (PlatformNotSupportedException)
        {
            _console.WriteLine($"... {message}");
            return new SimpleProgress();
        }
        catch (UnauthorizedAccessException)
        {
            _console.WriteLine($"... {message}");
            return new SimpleProgress();
        }
    }

    /// <summary>
    /// Writes a step progress message (simple one-line output).
    /// </summary>
    public void WriteStep(int current, int total, string message) =>
        _console.WriteLine($"[{current}/{total}] {message}");

    /// <summary>
    /// Writes a success message.
    /// </summary>
    public void WriteSuccess(string message) =>
        _console.WriteLine($"✓ {message}");

    /// <summary>
    /// Writes an error message.
    /// </summary>
    public void WriteError(string message) =>
        _console.WriteErrorLine($"✗ {message}");

    /// <summary>
    /// Prompts the user for a yes/no confirmation.
    /// </summary>
    public bool Confirm(string message, bool defaultValue = false) =>
        Prompt.Confirm(message, defaultValue);

    [ExcludeFromCodeCoverage]
    private sealed class SpinnerProgress : IDisposable
    {
        private static readonly string[] s_spinnerFrames = ["-", "\\", "|", "/", "-", "\\", "|", "/", "-", "\\"];
        private readonly IConsoleProxy _console;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _spinnerTask;
        private readonly string _message;
        private readonly bool _originalCursorVisible;
        private int _frameIndex;

        public SpinnerProgress(IConsoleProxy console, string message)
        {
            _console = console;
            _message = message;
            _originalCursorVisible = console.CursorVisible;
            _console.CursorVisible = false;
            _spinnerTask = RunSpinnerAsync(_cts.Token);
        }

        private async Task RunSpinnerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                _console.Write($"\r{s_spinnerFrames[_frameIndex]} {_message}");
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
            var clearLength = Math.Max(_console.BufferWidth - 1, _message.Length + 2);
            _console.Write($"\r{new string(' ', clearLength)}\r");
            _console.CursorVisible = _originalCursorVisible;
            _cts.Dispose();
        }
    }

    private sealed class SimpleProgress : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
