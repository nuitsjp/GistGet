namespace NuitsJp.GistGet.Commands;

public class FeaturesCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Features command executed");
        await Task.Delay(100);
        return 0;
    }
}

