namespace NuitsJp.GistGet.Commands;

public class SourceRemoveCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Source remove command executed");
        await Task.Delay(100);
        return 0;
    }
}

