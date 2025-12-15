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
}