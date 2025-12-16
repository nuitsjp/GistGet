// Abstraction for executing WinGet commands as a passthrough.

namespace GistGet;

/// <summary>
/// Defines a runner that forwards commands to WinGet and returns exit codes.
/// </summary>
public interface IWinGetPassthroughRunner
{
    /// <summary>
    /// Runs WinGet with the provided command-line arguments.
    /// </summary>
    Task<int> RunAsync(string[] args);
}
