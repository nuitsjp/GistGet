using System.Diagnostics;

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
public class ProcessResult(Process process) : IProcessResult
{

    public async Task WaitForExitAsync()
    {
        await process.WaitForExitAsync();
    }

    public int ExitCode => process.ExitCode;

    public async Task<string> ReadStandardOutputAsync()
    {
        return await process.StandardOutput.ReadToEndAsync();
    }

    public async Task<string> ReadStandardErrorAsync()
    {
        return await process.StandardError.ReadToEndAsync();
    }

    public void Dispose()
    {
        process?.Dispose();
    }
}