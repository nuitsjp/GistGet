// Runs WinGet as an external process and returns its exit code.

using System.Diagnostics;

namespace NuitsJp.GistGet.Infrastructure;

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
        ArgumentNullException.ThrowIfNull(args);

        var wingetPath = ResolveWinGetPath();
        var wingetArgs = string.Join(" ", args.Select(EscapeArgument));

        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"\"{wingetPath}\" {wingetArgs}\"",
            UseShellExecute = false,
            CreateNoWindow = false
        };

        return await _processRunner.RunAsync(startInfo);
    }

    private static string EscapeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
        {
            return "\"\"";
        }

        // If the argument contains spaces, quotes, or special characters, wrap it in quotes
        if (arg.Contains(' ') || arg.Contains('"') || arg.Contains('&') || arg.Contains('|') || arg.Contains('<') || arg.Contains('>') || arg.Contains('^'))
        {
            // Escape internal quotes by doubling them
            return $"\"{arg.Replace("\"", "\"\"")}\"";
        }

        return arg;
    }

    private static string ResolveWinGetPath()
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




