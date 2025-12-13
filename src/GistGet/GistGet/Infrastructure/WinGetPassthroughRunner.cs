using System.Diagnostics;

namespace GistGet.Infrastructure.Diagnostics;

public class WinGetPassthroughRunner : IWinGetPassthroughRunner
{
    private readonly IProcessRunner _processRunner;

    public WinGetPassthroughRunner(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

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

        // ArgumentListを使用してスペースを含む引数を正しくエスケープ
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        return await _processRunner.RunAsync(startInfo);
    }

    private string ResolveWinGetPath()
    {
        // 1. PATH 上の winget を探索
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

        // 2. フォールバック: LocalAppData
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        return Path.Combine(localAppData!, "Microsoft", "WindowsApps", "winget.exe");
    }

}
