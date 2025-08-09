namespace NuitsJp.GistGet.Commands;

public class ImportCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Import command executed");
        await Task.Delay(100);
        return 0;
    }
}

