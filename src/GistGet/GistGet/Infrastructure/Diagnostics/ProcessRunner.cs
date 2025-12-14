using System.Diagnostics;

namespace GistGet.Infrastructure.Diagnostics;

public class ProcessRunner : IProcessRunner
{
    public async Task<int> RunAsync(ProcessStartInfo startInfo)
    {
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
