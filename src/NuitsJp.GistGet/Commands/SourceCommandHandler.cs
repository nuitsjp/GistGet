namespace NuitsJp.GistGet.Commands;

public class SourceCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Source command executed");
        await Task.Delay(100);
        return 0;
    }
}

