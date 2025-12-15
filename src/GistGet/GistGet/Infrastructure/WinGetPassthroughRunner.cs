// Runs WinGet as an external process and returns its exit code.

using System.Diagnostics;

namespace GistGet.Infrastructure.Diagnostics;

/// <summary>
/// Executes WinGet as a child process.
/// </summary>
public class WinGetPassthroughRunner : IWinGetPassthroughRunner
{
    private readonly IProcessRunner _processRunner;

    public WinGetPassthroughRunner(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    /// <summary>
    /// Runs WinGet with the given arguments.
    /// </summary>
    public async Task<int> RunAsync(string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveWinGetPath(),
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        return await _processRunner.RunAsync(startInfo);
    }

    private string ResolveWinGetPath()
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv != null)
        {
            var paths = pathEnv.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, "winget.exe");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        return Path.Combine(localAppData!, "Microsoft", "WindowsApps", "winget.exe");
    }
}
