// Default implementation for running external processes.

using System.Diagnostics;
using GistGet.GistGet.Infrastructure;

namespace GistGet.Infrastructure.Diagnostics;

/// <summary>
/// Runs a process and returns its exit code.
/// </summary>
public class ProcessRunner : IProcessRunner
{
    /// <summary>
    /// Runs the specified process and returns its exit code.
    /// </summary>
    public async Task<int> RunAsync(ProcessStartInfo startInfo)
    {
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
