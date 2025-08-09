namespace NuitsJp.GistGet.Commands;

public class DownloadCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Download command executed");
        await Task.Delay(100);
        return 0;
    }
}

