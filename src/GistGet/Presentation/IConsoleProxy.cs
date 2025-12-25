// Abstraction over System.Console for easier testing and capability detection.

namespace GistGet.Presentation;

public interface IConsoleProxy
{
    bool CursorVisible { get; set; }

    int BufferWidth { get; }

    bool IsOutputRedirected { get; }

    bool IsErrorRedirected { get; }

    string? ReadLine();

    void Write(string value);

    void WriteLine(string value);

    void WriteErrorLine(string value);
}
