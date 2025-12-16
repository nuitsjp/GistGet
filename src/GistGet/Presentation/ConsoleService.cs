// Console I/O implementation for the CLI layer.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using GistGet.Infrastructure;

namespace GistGet.Presentation;

/// <summary>
/// Writes messages to the console and provides clipboard support.
/// </summary>
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
    public void WriteInfo(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    public void WriteWarning(string message)
    {
        Console.WriteLine($"! {message}");
    }

    /// <summary>
    /// Reads a single line from standard input.
    /// </summary>
    public string? ReadLine()
    {
        return Console.ReadLine();
    }

    [SupportedOSPlatform("windows")]
    public void SetClipboard(string text)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SetClipboardWindows(text);
        }
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
}
