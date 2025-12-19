// Abstraction for writing user-facing output.

namespace GistGet;

/// <summary>
/// Defines console and clipboard operations used by the application.
/// </summary>
public interface IConsoleService
{
    /// <summary>
    /// Writes an informational message.
    /// </summary>
    void WriteInfo(string message);

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    void WriteWarning(string message);

    /// <summary>
    /// Reads a single line of input.
    /// </summary>
    string? ReadLine();

    /// <summary>
    /// Copies text to the clipboard.
    /// </summary>
    void SetClipboard(string text);

    /// <summary>
    /// Starts a spinner progress display.
    /// The spinner animates in the background and stops on Dispose.
    /// </summary>
    /// <param name="message">Message to display.</param>
    /// <returns>IDisposable that stops the spinner on Dispose.</returns>
    IDisposable WriteProgress(string message);

    /// <summary>
    /// Writes a step progress message (simple one-line output).
    /// </summary>
    /// <param name="current">Current step number.</param>
    /// <param name="total">Total number of steps.</param>
    /// <param name="message">Message to display.</param>
    void WriteStep(int current, int total, string message);

    /// <summary>
    /// Writes a success message.
    /// </summary>
    /// <param name="message">Message to display.</param>
    void WriteSuccess(string message);

    /// <summary>
    /// Writes an error message.
    /// </summary>
    /// <param name="message">Message to display.</param>
    void WriteError(string message);
}
