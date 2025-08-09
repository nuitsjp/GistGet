namespace NuitsJp.GistGet.Commands;

public class ConfigureCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Configure command executed");
        await Task.Delay(100);
        return 0;
    }
}

