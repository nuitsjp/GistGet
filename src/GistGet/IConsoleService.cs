// Abstraction for writing user-facing output.

namespace GistGet;

/// <summary>
/// Defines console and clipboard operations used by the application.
/// </summary>
/// <remarks>
/// <para><strong>Logging Policy</strong></para>
/// <para>
/// This interface provides the logging and console output abstraction for GistGet.
/// When implementing or using this interface, follow these guidelines:
/// </para>
/// 
/// <para><strong>1. Log Levels</strong></para>
/// <list type="table">
///   <listheader>
///     <term>Level</term>
///     <description>Usage</description>
///   </listheader>
///   <item>
///     <term>Info</term>
///     <description>Normal processing flow, operation start/completion. Use <see cref="WriteInfo"/>.</description>
///   </item>
///   <item>
///     <term>Warning</term>
///     <description>Situations requiring attention, skipped operations. Use <see cref="WriteWarning"/>.</description>
///   </item>
///   <item>
///     <term>Progress</term>
///     <description>Spinner display for long-running operations. Use <see cref="WriteProgress"/>.</description>
///   </item>
///   <item>
///     <term>Step</term>
///     <description>Multi-step progress display. Use <see cref="WriteStep"/>.</description>
///   </item>
///   <item>
///     <term>Success</term>
///     <description>Successful completion of operations. Use <see cref="WriteSuccess"/>.</description>
///   </item>
///   <item>
///     <term>Error</term>
///     <description>Error information. Use <see cref="WriteError"/>.</description>
///   </item>
/// </list>
/// 
/// <para><strong>2. Winget Passthrough Principle</strong></para>
/// <para>
/// For winget command passthrough operations, let winget handle its own output:
/// </para>
/// <list type="bullet">
///   <item>Do not add redundant logging before/after <c>passthroughRunner.RunAsync()</c> calls.</item>
///   <item>Winget displays its own progress and errors; avoid duplicate output.</item>
///   <item>Only log GistGet-specific operations (e.g., Gist operations).</item>
/// </list>
/// 
/// <para><strong>3. Progress Display Guidelines</strong></para>
/// <para>Use progress display for the following operations:</para>
/// <list type="bullet">
///   <item>Gist retrieval (<c>GetPackagesAsync()</c>)</item>
///   <item>Gist saving (<c>SavePackagesAsync()</c>)</item>
///   <item>Sync processing (source-specific retrieval)</item>
/// </list>
/// 
/// <para><strong>Important Notes:</strong></para>
/// <list type="bullet">
///   <item>
///     <see cref="WriteProgress"/> returns <see cref="IDisposable"/>. Use with a <c>using</c> block
///     to ensure the progress indicator is properly terminated, even when exceptions occur.
///   </item>
///   <item>
///     When using <see cref="WriteStep"/>, determine the step count before starting.
///     If packages are dynamically added/skipped, recalculate the total.
///   </item>
/// </list>
/// </remarks>
public interface IConsoleService
{
    /// <summary>
    /// Writes an informational message.
    /// </summary>
    /// <remarks>
    /// Use for normal processing flow and operation start/completion notifications.
    /// </remarks>
    void WriteInfo(string message);

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    /// <remarks>
    /// Use for situations requiring attention or when operations are skipped.
    /// </remarks>
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

    /// <summary>
    /// Prompts the user for a yes/no confirmation.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="defaultValue">The default value if the user just presses enter.</param>
    /// <returns>True if the user confirmed, false otherwise.</returns>
    bool Confirm(string message, bool defaultValue = false);
}
