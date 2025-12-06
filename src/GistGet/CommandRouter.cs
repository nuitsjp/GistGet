namespace GistGet;

public class CommandRouter(
    IWinGetPassthroughRunner winGetPassthroughRunner)
{
    public async Task RunAsync(string[] args)
    {
        var mainCommand = args.FirstOrDefault();
        if (mainCommand is null)
        {
            await winGetPassthroughRunner.RunAsync(args);
        }
    }
}

public interface ICommand
{
    string Name { get; }
    Task RunAsync(string[] args);
}

public abstract class CommandBase : ICommand
{
    public abstract string Name { get; }
    protected abstract string Help { get; }
    public Task RunAsync(string[] args)
    {
        if (args.Contains("-?")
            || args.Contains("-h")
            || args.Contains("--help"))
        {

        }

        return RunInnerAsync(args);
    }

    protected abstract Task RunInnerAsync(string[] args);
}