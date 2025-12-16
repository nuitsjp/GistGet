// Abstraction for user interaction and messaging.

namespace GistGet;

/// <summary>
/// Defines the user interaction surface used by application workflows.
/// </summary>
public interface IUserInterface
{
    /// <summary>
    /// Writes a message followed by a newline.
    /// </summary>
    void WriteLine(string message);
}
