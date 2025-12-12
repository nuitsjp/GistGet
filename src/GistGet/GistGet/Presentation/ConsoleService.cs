using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GistGet.Presentation;

public class ConsoleService : IConsoleService
{
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
    private static void SetClipboardWindows(string text)
    {
        // Use PowerShell to set clipboard on Windows
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-Command \"Set-Clipboard -Value '{text.Replace("'", "''")}'\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.Start();
        process.WaitForExit();
    }
}