namespace NuitsJp.GistGet.Commands;

public class SearchCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Search command executed");
        await Task.Delay(100);
        return 0;
    }
}

