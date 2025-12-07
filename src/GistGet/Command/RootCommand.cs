namespace GistGet.Command;

public class RootCommand(
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