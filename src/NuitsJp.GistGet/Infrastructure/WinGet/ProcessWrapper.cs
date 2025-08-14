using System.Diagnostics;
using NuitsJp.GistGet.Infrastructure.WinGet;

namespace NuitsJp.GistGet.Infrastructure.WinGet;

/// <summary>
/// プロセス実行のラッパー実装
/// </summary>
public class ProcessWrapper : IProcessWrapper
{
    public IProcessResult? Start(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo);
        return process != null ? new ProcessResult(process) : null;
    }
}

/// <summary>
/// プロセス実行結果の実装
/// </summary>
public class ProcessResult : IProcessResult
{
    private readonly Process _process;

    public ProcessResult(Process process)
    {
        _process = process;
    }

    public async Task WaitForExitAsync()
    {
        await _process.WaitForExitAsync();
    }

    public int ExitCode => _process.ExitCode;

    public async Task<string> ReadStandardOutputAsync()
    {
        return await _process.StandardOutput.ReadToEndAsync();
    }

    public async Task<string> ReadStandardErrorAsync()
    {
        return await _process.StandardError.ReadToEndAsync();
    }

    public void Dispose()
    {
        _process?.Dispose();
    }
}