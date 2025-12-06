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