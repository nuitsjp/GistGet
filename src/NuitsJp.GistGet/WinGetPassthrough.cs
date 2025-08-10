using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Abstractions;

namespace NuitsJp.GistGet;

/// <summary>
/// winget.exeへのパススルー実装（アーキテクチャ改善版）
/// </summary>
public class WinGetPassthrough : IWinGetPassthroughClient
{
    private readonly IProcessWrapper _processWrapper;
    private readonly ILogger<WinGetPassthrough> _logger;

    public WinGetPassthrough(IProcessWrapper processWrapper, ILogger<WinGetPassthrough> logger)
    {
        _processWrapper = processWrapper;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        _logger.LogDebug("Executing winget passthrough with args: {Args}", string.Join(" ", args));

        var startInfo = new ProcessStartInfo
        {
            FileName = "winget.exe",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };

        // 引数をそのまま渡す
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        try
        {
            using var process = _processWrapper.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start winget.exe");
                return 1;
            }

            await process.WaitForExitAsync();
            _logger.LogDebug("winget.exe completed with exit code: {ExitCode}", process.ExitCode);
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing winget.exe");
            return 1;
        }
    }
}