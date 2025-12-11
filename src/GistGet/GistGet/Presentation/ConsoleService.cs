namespace GistGet.Presentation;

public class ConsoleService : IConsoleService
{
    public void WriteInfo(string message)
    {
        Console.WriteLine(message);
    }
}