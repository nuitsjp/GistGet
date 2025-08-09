namespace NuitsJp.GistGet.Commands;

public class SourceExportCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Source export command executed");
        await Task.Delay(100);
        return 0;
    }
}

