namespace GistGet;

public interface IWinGetPassthroughRunner
{
    Task<int> RunAsync(string[] args);
}