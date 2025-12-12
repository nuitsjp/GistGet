namespace GistGet;

public interface IConsoleService
{
    void WriteInfo(string message);
    void WriteWarning(string message);
    string? ReadLine();
    void SetClipboard(string text);
}