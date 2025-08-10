using System.Diagnostics;

namespace NuitsJp.GistGet;

/// <summary>
/// MVP Phase 1: winget.exeへの最小限のパススルー実装
/// </summary>
public class WinGetPassthrough
{
    public async Task<int> ExecuteAsync(string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "winget.exe",
            UseShellExecute = false,
        };

        // 引数をそのまま渡す
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            Console.WriteLine("Error: Failed to start winget.exe");
            return 1;
        }

        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}