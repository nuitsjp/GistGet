using System.Diagnostics;

namespace NuitsJp.GistGet.WinGetClient.Abstractions;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        IDictionary<string, string>? environment = null,
        CancellationToken cancellationToken = default);
}

public readonly record struct ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Elapsed)
{
    public bool Succeeded => ExitCode == 0;
}
