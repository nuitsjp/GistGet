using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using GistGet.Infrastructure.Diagnostics;

namespace GistGet.Presentation;

public class ConsoleService : IConsoleService
{
    private readonly IProcessRunner _processRunner;

    public ConsoleService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public void WriteInfo(string message)
    {
        Console.WriteLine(message);
    }

    public void WriteWarning(string message)
    {
        Console.WriteLine($"! {message}");
    }

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
        // Use PowerShell to set clipboard on Windows
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
