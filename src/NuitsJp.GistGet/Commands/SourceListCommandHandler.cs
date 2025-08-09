namespace NuitsJp.GistGet.Commands;

public class SourceListCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Source list command executed");
        await Task.Delay(100);
        return 0;
    }
}

