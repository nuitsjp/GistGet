namespace NuitsJp.GistGet.Commands;

public class SourceAddCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Source add command executed");
        await Task.Delay(100);
        return 0;
    }
}

