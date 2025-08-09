using System.Diagnostics;

namespace NuitsJp.GistGet.WinGetClient.Abstractions;

public sealed class DefaultProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        IDictionary<string, string>? environment = null,
        CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow;
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? string.Empty
        };
        if (environment is not null)
        {
            foreach (var kv in environment)
            {
                psi.Environment[kv.Key] = kv.Value;
            }
        }

        using var proc = Process.Start(psi);
        if (proc is null)
        {
            return new ProcessResult(1, string.Empty, "Failed to start process", TimeSpan.Zero);
        }

        var stdOutTask = proc.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = proc.StandardError.ReadToEndAsync(cancellationToken);
        await proc.WaitForExitAsync(cancellationToken);
        var elapsed = DateTime.UtcNow - start;
        var stdout = await stdOutTask;
        var stderr = await stdErrTask;
        return new ProcessResult(proc.ExitCode, stdout, stderr, elapsed);
    }
}
