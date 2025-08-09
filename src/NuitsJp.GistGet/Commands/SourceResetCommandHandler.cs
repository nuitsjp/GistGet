namespace NuitsJp.GistGet.Commands;

public class SourceResetCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Source reset command executed");
        await Task.Delay(100);
        return 0;
    }
}

