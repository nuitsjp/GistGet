namespace NuitsJp.GistGet.Commands;

public class ShowCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Show command executed");
        await Task.Delay(100);
        return 0;
    }
}

