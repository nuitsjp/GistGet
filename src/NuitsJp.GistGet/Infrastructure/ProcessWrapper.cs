using System.Diagnostics;
using NuitsJp.GistGet.Abstractions;

namespace NuitsJp.GistGet.Infrastructure;

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

    public void Dispose()
    {
        _process?.Dispose();
    }
}