namespace NuitsJp.GistGet.Commands;

public class ExportCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Export command executed");
        await Task.Delay(100);
        return 0;
    }
}

