namespace NuitsJp.GistGet.Commands;

public class SourceUpdateCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Source update command executed");
        await Task.Delay(100);
        return 0;
    }
}

