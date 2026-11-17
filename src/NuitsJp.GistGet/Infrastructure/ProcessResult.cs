namespace NuitsJp.GistGet.Infrastructure;

/// <summary>
/// Captures a process exit code and redirected output streams.
/// </summary>
public record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
