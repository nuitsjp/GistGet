// Default implementation that delegates to System.Console.

namespace GistGet.Presentation;

public class SystemConsoleProxy : IConsoleProxy
{
    public bool CursorVisible
    {
        get => Console.CursorVisible;
        set => Console.CursorVisible = value;
    }

    public int BufferWidth => Console.BufferWidth;

    public bool IsOutputRedirected => Console.IsOutputRedirected;

    public bool IsErrorRedirected => Console.IsErrorRedirected;

    public string? ReadLine() => Console.ReadLine();

    public void Write(string value) => Console.Write(value);

    public void WriteLine(string value) => Console.WriteLine(value);

    public void WriteErrorLine(string value) => Console.Error.WriteLine(value);
}
