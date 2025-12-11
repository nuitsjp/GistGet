namespace GistGet.Model;

public interface IWinGetPassthroughRunner
{
    Task<int> RunAsync(string[] args);
}