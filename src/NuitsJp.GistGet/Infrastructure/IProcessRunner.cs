// Abstraction for running external processes.

using System.Diagnostics;

namespace NuitsJp.GistGet.Infrastructure;

/// <summary>
/// Defines an API for executing processes and returning their exit code.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a process and returns its exit code.
    /// </summary>
    Task<int> RunAsync(ProcessStartInfo startInfo);

    /// <summary>
    /// Runs a process and captures its exit code, standard output, and standard error.
    /// </summary>
    ProcessResult RunAndCapture(ProcessStartInfo startInfo);
}




