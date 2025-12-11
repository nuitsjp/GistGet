namespace GistGet;

public interface ICommand
{
    string Name { get; }
    Task RunAsync(string[] args);
}