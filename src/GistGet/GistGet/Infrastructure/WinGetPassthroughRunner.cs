using System.Diagnostics;

namespace GistGet.Infrastructure.Diagnostics;

public class WinGetPassthroughRunner : IWinGetPassthroughRunner
{
    public async Task<int> RunAsync(string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveWinGetPath(),
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private string ResolveWinGetPath()
    {
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        return Path.Combine(localAppData!, "Microsoft", "WindowsApps", "winget.exe");
    }

}